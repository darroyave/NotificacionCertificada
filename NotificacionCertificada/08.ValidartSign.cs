using Azure.Data.Tables;
using Azure.Storage.Blobs;
using Azure.Storage.Queues;
using iText.Bouncycastle.Crypto;
using iText.Bouncycastle.X509;
using iText.Commons.Bouncycastle.Cert;
using iText.Kernel.Pdf;
using iText.Signatures;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Pkcs;
using Org.BouncyCastle.X509;
using NotificacionCertificada.Shared.Messages;
using NotificacionCertificada.Shared.Tables;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection.Metadata;
using System.Threading.Tasks;
using Azure.Storage.Blobs.Models;
using NotificacionCertificada.Shared.Utils;

namespace NotificacionCertificada
{
    public class ValidartSign
    {
        [FunctionName("ValidartSign")]
        public async Task Run(
            [QueueTrigger("notificacioncertificada-sign")] string filename,
            [Table("ncclientes")] TableClient tableCliente,
            [Table("nctransacciones")] TableClient tableTransaccion,
            [Blob("certificates/cert_test.p12", FileAccess.Read)] Stream blobStreamTemplate,
            [Blob("temp/{queueTrigger}", FileAccess.Read)] Stream myBlob,
            [Blob("sign/{rand-guid}.pdf", FileAccess.Write)] BlobClient blobStream,
            [Queue("notificacioncertificada-notify")] QueueClient queueNotify,
            [Queue("notificacioncertificada-db")] QueueClient queueDB,
            [Queue("notificacioncertificada-notify-email")] QueueClient queueNotifyEmail,
            [Blob("temp/{queueTrigger}")] BlobClient blobDelete,
            [Table("ncconfig", "notificacioncertificado", "config")] ConfigEntity tableConfig,
            [Queue("notificacioncertificada-fill")] QueueClient queueFill,
            ILogger log)
        {
            log.LogInformation($"C# Queue trigger function: ValidartSign");

            string codeFile = Path.GetFileNameWithoutExtension(filename);

            ClientEntity cliente = await tableCliente.GetEntityAsync<ClientEntity>(
                "file", codeFile);

            if(cliente != null)
            {
                TransaccionEntity tran = await tableTransaccion.GetEntityAsync<TransaccionEntity>(
                    cliente.OperacionId.ToString(), cliente.TransaccionId.ToString());

                if (tran != null)
                {
                    byte[] fileData = PDFSigner(myBlob, blobStreamTemplate, tableConfig.CerPassword,
                        tableConfig.TSAUrl, tableConfig.TSAUser, tableConfig.TSAPassword,
                        tableConfig.TSAEstimatedSize);

                    using var ms = new MemoryStream(fileData, false);

                    await blobStream.UploadAsync(ms, new BlobHttpHeaders { ContentType = "application/pdf" });

                    string outputBlobUrl = blobStream.Uri.ToString();

                    if (tran!.Flujo! == Flujos.Callback)
                    {
                        tran.UrlPdfRecibido = outputBlobUrl;
                        tran.Flujo = "Recibido";
                    }
                    else if (tran!.Flujo! == Flujos.Visualizado)
                    {
                        tran.UrlPdfVisualizado = outputBlobUrl;
                    }
                    else // Error
                    {
                        tran.UrlPdfError = outputBlobUrl;
                    }
                    
                    await tableTransaccion.UpdateEntityAsync(tran, tran.ETag);

                    // Notify Client
                    if (!string.IsNullOrEmpty(tran.CallbackClient))
                    {
                        var messageNotify = new MessageNotifyCola()
                        {
                            OperacionId = Guid.Parse(tran.PartitionKey),
                            TransaccionId = Guid.Parse(tran.RowKey),
                            Flujo = tran.Flujo,
                            UrlPdf = outputBlobUrl,
                            CallbackCliente = tran.CallbackClient
                        };

                        var jsonNotify = JsonConvert.SerializeObject(messageNotify);
                        await queueNotify.SendMessageAsync(jsonNotify);
                    }

                    // Notify DB
                    Guid tranId = Guid.Empty;

                    if (tran.Flujo == Flujos.Recibido || tran.Flujo == Flujos.Error)
                    {
                        tranId = tran.TransaccionRecibidoId;
                    }
                    else // Visuzalizado
                    {
                        tranId = tran.TransaccionVisualizadoId;
                    }

                    var messageDatabase = new MessageDatabaseCola()
                    {
                        OperacionId = Guid.Parse(tran.PartitionKey),
                        TransaccionId = Guid.Parse(tran.RowKey),
                        TransaccionEventoId = tranId,
                        UrlPDF = outputBlobUrl,
                    };

                    var jsonDB = JsonConvert.SerializeObject(messageDatabase);
                    await queueDB.SendMessageAsync(jsonDB);

                    if (tran.NotifyEmail)
                    {
                        // Notify Email
                        var messageNotifyEmailCola = new MessageNotifyEmailCola()
                        {
                            OperacionId = Guid.Parse(tran.PartitionKey),
                            TransaccionId = Guid.Parse(tran.RowKey),
                            Url = outputBlobUrl
                        };

                        var jsonEmail = JsonConvert.SerializeObject(messageNotifyEmailCola);
                        await queueNotifyEmail.SendMessageAsync(jsonEmail);
                    }

                    // Borrar el temp
                    await blobDelete.DeleteIfExistsAsync();

                    if(!tran.ProductCode.Equals(ProductCertificado.SMSSimple) && tran.Flujo != Flujos.Error)
                    {
                        if(tran.Flujo == Flujos.Recibido && 
                            tran.ProductCode.Equals(ProductCertificado.SMSUrl) &&
                            tran.Visualizado)
                        {
                            TransaccionEntity tranVisualizado = await tableTransaccion.GetEntityAsync<TransaccionEntity>(
                                cliente.OperacionId.ToString(), cliente.TransaccionId.ToString());

                            if(tranVisualizado != null)
                            {
                                tranVisualizado.Flujo = Flujos.Visualizado;

                                await tableTransaccion.UpdateEntityAsync(tranVisualizado, tranVisualizado.ETag);

                                // Generar PDF de Visualizado

                                var messageFill = new MessageFillCola()
                                {
                                    OperacionId = Guid.Parse(tranVisualizado.PartitionKey),
                                    TransaccionId = Guid.Parse(tranVisualizado.RowKey),
                                    EntidadId = tranVisualizado.EntidadId,
                                    ProductCode = tranVisualizado.ProductCode
                                };

                                var json = JsonConvert.SerializeObject(messageFill);
                                await queueFill.SendMessageAsync(json);

                            }
                            

                        }
                    }

                }

            }

        }

        public static byte[] PDFSigner(
            Stream pdfToSign, 
            Stream cert, string password,
            string TSAUrl, string TSAUserName, string TSAPassword,
            int estimatedSize)
        {
            using MemoryStream fout = new();

            char[] pass = password.ToCharArray();

            Pkcs12Store pk12 = new Pkcs12StoreBuilder().Build();
            pk12.Load(cert, pass);

            string? alias = null;
            foreach (var a in pk12.Aliases)
            {
                alias = a;
                if (pk12.IsKeyEntry(alias))
                    break;
            }

            ICipherParameters pk = pk12.GetKey(alias).Key;
            X509CertificateEntry[] ce = pk12.GetCertificateChain(alias);
            X509Certificate[] chain = new X509Certificate[ce.Length];
            for (int k = 0; k < ce.Length; ++k)
            {
                chain[k] = ce[k].Certificate;
            }

            PdfReader reader = new(pdfToSign);
            PdfSigner signer = new(reader, fout, new StampingProperties());

            IExternalSignature pks = new PrivateKeySignature(new PrivateKeyBC(pk), DigestAlgorithms.SHA256);

            IX509Certificate[] certificateWrappers = new IX509Certificate[chain.Length];
            for (int i = 0; i < certificateWrappers.Length; ++i)
            {
                certificateWrappers[i] = new X509CertificateBC(chain[i]);
            }

            ITSAClient tsaClient = new TSAClientBouncyCastle(TSAUrl, TSAUserName, TSAPassword);
            IOcspClient ocspClient = new OcspClientBouncyCastle(null);
            ICrlClient crlClient = new CrlClientOnline(certificateWrappers);
            List<ICrlClient> lstCrlClients = new List<ICrlClient> { crlClient };

            signer.SignDetached(pks, certificateWrappers, lstCrlClients, ocspClient, tsaClient, estimatedSize, PdfSigner.CryptoStandard.CMS);

            return AddLTV(fout.ToArray(), ocspClient, crlClient);
        }

        private static byte[] AddLTV(byte[] pdfbytesOrigen, IOcspClient ocsp, ICrlClient crl)
        {
            var pdfMemoryStream = new MemoryStream(pdfbytesOrigen);

            using var memoryStream = new MemoryStream();
            using (var pdfDocument = new PdfDocument(new PdfReader(pdfMemoryStream), new PdfWriter(memoryStream),
                new StampingProperties().UseAppendMode()))
            {
                LtvVerification v = new(pdfDocument);
                SignatureUtil signatureUtil = new(pdfDocument);
                IList<string> names = signatureUtil.GetSignatureNames();
                string sigName = names[names.Count - 1];
                PdfPKCS7 pkcs7 = signatureUtil.ReadSignatureData(sigName);
                if (pkcs7.IsTsp())
                {
                    v.AddVerification(sigName, ocsp, crl, LtvVerification.CertificateOption.WHOLE_CHAIN,
                        LtvVerification.Level.OCSP_CRL, LtvVerification.CertificateInclusion.NO);
                }
                else
                {
                    foreach (var name in names)
                    {
                        v.AddVerification(name, ocsp, crl, LtvVerification.CertificateOption.WHOLE_CHAIN,
                            LtvVerification.Level.OCSP_CRL, LtvVerification.CertificateInclusion.YES);
                        v.Merge();
                    }
                }
            }

            return memoryStream.ToArray();
        }
    }
}

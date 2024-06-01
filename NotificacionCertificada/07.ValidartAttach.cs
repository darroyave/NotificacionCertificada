using iText.Kernel.Pdf.Filespec;
using iText.Kernel.Pdf;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Linq;
using System;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Azure.Data.Tables;
using System.Net.Http;
using Azure.Storage.Blobs.Models;
using NotificacionCertificada.Shared.Tables;
using NotificacionCertificada.Shared.Models;

namespace NotificacionCertificada
{
    public class ValidartAttach
    {
        [FunctionName("ValidartAttach")]
        public async Task Run(
            [QueueTrigger("notificacioncertificada-attach")] string filename,
            [Blob("temp/{queueTrigger}", FileAccess.Read)] Stream myBlob,
            [Table("ncclientes")] TableClient tableCliente,
            [Table("nctransacciones")] TableClient tableTransaccion,
            [Blob("temp/{rand-guid}.pdf", FileAccess.Write)] BlobClient blobStream,
            [Queue("notificacioncertificada-sign")] ICollector<string> queueSign,
            [Blob("temp/{queueTrigger}")] BlobClient blobDelete,
            ILogger log)
        {
            log.LogInformation("C# Queue trigger function processed: ValidartAttach");

            string codeFile = Path.GetFileNameWithoutExtension(filename);

            ClientEntity cliente = await tableCliente.GetEntityAsync<ClientEntity>(
                "file", codeFile);

            if (cliente != null)
            {
                TransaccionEntity tran = await tableTransaccion.GetEntityAsync<TransaccionEntity>(
                    cliente.OperacionId.ToString(), cliente.TransaccionId.ToString());

                if (tran != null)
                {
                    var archivo = new TipoAttachViewModel[] {
                            new() {
                                Archivo = new ArchivoViewModel() {
                                    Url = tran.UrlPdfRecibido
                                },
                                Ext = ".pdf",
                                Tipo = "PDF"
                            }
                        };

                    byte[] fileData = await AddAttachments(myBlob, archivo);

                    using var ms = new MemoryStream(fileData, false);

                    await blobStream.UploadAsync(ms, new BlobHttpHeaders { ContentType = "application/pdf" });

                    string outputBlobUrl = blobStream.Uri.ToString();

                    string fileName = Path.GetFileName(outputBlobUrl);
                    string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(outputBlobUrl);

                    var clientEntity = new ClientEntity()
                    {
                        PartitionKey = "file",
                        RowKey = fileNameWithoutExtension,
                        OperacionId = Guid.Parse(tran.PartitionKey),
                        TransaccionId = Guid.Parse(tran.RowKey)
                    };

                    await tableCliente.AddEntityAsync(clientEntity);

                    queueSign.Add(fileName);

                    // Borrar el temp
                    await blobDelete.DeleteIfExistsAsync();
                }
                    
            }
 
        }

        public static async Task<byte[]> AddAttachments(Stream pdfMemoryStream, TipoAttachViewModel[] attachments)
        {

            using var memoryStream = new MemoryStream();

            using (var pdfDocument = new PdfDocument(new PdfReader(pdfMemoryStream), new PdfWriter(memoryStream)))
            {
                foreach (var attachment in attachments)
                {
                    byte[] embeddedFileContentBytes;

                    if (attachment.BytesTxt == null)
                    {
                        embeddedFileContentBytes = await DownloadFile(attachment.Archivo.Url);

                        string fname = attachment.Archivo.Url.Split('/').Last();

                        string tipo = attachment.Tipo;

                        if (!string.IsNullOrEmpty(attachment.Archivo.Name))
                        {
                            tipo = attachment.Archivo.Name;
                        }

                        PdfFileSpec spec = PdfFileSpec.CreateEmbeddedFileSpec(pdfDocument, embeddedFileContentBytes,
                            tipo, fname, null, null, null);

                        pdfDocument.AddFileAttachment(System.IO.Path.GetFileNameWithoutExtension(fname), spec);
                    }
                    else
                    {
                        embeddedFileContentBytes = attachment.BytesTxt;

                        PdfFileSpec spec = PdfFileSpec.CreateEmbeddedFileSpec(pdfDocument, embeddedFileContentBytes,
                            attachment.Tipo, $"{Guid.NewGuid()}{attachment.Ext}", null, null, null);

                        pdfDocument.AddFileAttachment($"{Guid.NewGuid()}", spec);
                    }


                }
            }

            return memoryStream.ToArray();
        }

        public static async Task<byte[]> DownloadFile(string url)
        {
            HttpClient _httpClient = new();
            byte[] fileBytes = await _httpClient.GetByteArrayAsync(url);
            return fileBytes;
        }
    }
}

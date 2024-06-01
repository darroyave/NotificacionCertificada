using Azure.Data.Tables;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using iText.Kernel.Pdf;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using NotificacionCertificada.Shared.Messages;
using NotificacionCertificada.Shared.Tables;
using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace NotificacionCertificada
{
    

    public class ValidarEncriptar
    {
        private static readonly string? AdminPasswordPDF = Environment.GetEnvironmentVariable("AdminPasswordPDF");

        [FunctionName("ValidarEncriptar")]
        public async Task Run(
            [QueueTrigger("notificacioncertificada-password")] MessagePasswordCola messagePasswordCola,
            [Table("nctransacciones")] TableClient tableTransaccion,
            [Blob("encripcion/{rand-guid}.pdf", FileAccess.Write)] BlobClient blobStream,
            ILogger log)
        {
            log.LogInformation("C# Queue trigger function processed: notificacioncertificada");

            TransaccionEntity tran = await tableTransaccion.GetEntityAsync<TransaccionEntity>(
                    messagePasswordCola.OperacionId.ToString(), messagePasswordCola.TransaccionId.ToString());

            if (tran != null)
            {
                byte[] fileData = await DownloadFile(tran.UrlDoc);

                byte[] fileDataResult = EncryptPDFwithPassword(fileData, tran.PassDoc, AdminPasswordPDF);

                using var ms = new MemoryStream(fileDataResult, false);

                await blobStream.UploadAsync(ms, new BlobHttpHeaders { ContentType = "application/pdf" });

                string outputBlobUrl = blobStream.Uri.ToString();

                tran.UrlDocEncriptada = outputBlobUrl;

                await tableTransaccion.UpdateEntityAsync(tran, tran.ETag);
            }

        }

        public static byte[] EncryptPDFwithPassword(byte[] srcBytes, string passwordUser, string passwordOwner)
        {
            var userPassword = Encoding.ASCII.GetBytes(passwordUser);
            var ownerPassword = Encoding.ASCII.GetBytes(passwordOwner);

            PdfReader reader = new(new MemoryStream(srcBytes));
            WriterProperties props = new WriterProperties()
                    .SetStandardEncryption(userPassword, ownerPassword, EncryptionConstants.ALLOW_PRINTING,
                            EncryptionConstants.ENCRYPTION_AES_128 | EncryptionConstants.DO_NOT_ENCRYPT_METADATA);

            using var memoryStream = new MemoryStream();
            PdfWriter writer = new(memoryStream, props);
            PdfDocument pdfDoc = new(reader, writer);
            pdfDoc.Close();
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

using Azure.Data.Tables;
using Azure.Storage.Blobs;
using System.IO;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using iText.Kernel.Pdf;
using iText.Kernel.Utils;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using System;
using Azure.Storage.Blobs.Models;
using NotificacionCertificada.Shared.Tables;
using NotificacionCertificada.Shared.Utils;

namespace NotificacionCertificada
{
    public class ValidarAppend
    {
        [FunctionName("ValidarAppend")]
        public async Task Run(
            [QueueTrigger("notificacioncertificada-append")] string filename,
            [Blob("temp/{queueTrigger}", FileAccess.Read)] Stream myBlob,
            [Table("ncclientes")] TableClient tableCliente,
            [Table("nctransacciones")] TableClient tableTransaccion,
            [Blob("temp/{rand-guid}.pdf", FileAccess.Write)] BlobClient blobStream,
            [Queue("notificacioncertificada-attach")] ICollector<string> queueAttach,
            [Queue("notificacioncertificada-sign")] ICollector<string> queueSign,
            [Blob("temp/{queueTrigger}")] BlobClient blobDelete,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            string codeFile = Path.GetFileNameWithoutExtension(filename);

            ClientEntity cliente = await tableCliente.GetEntityAsync<ClientEntity>(
                "file", codeFile);

            if (cliente != null)
            {
                TransaccionEntity tran = await tableTransaccion.GetEntityAsync<TransaccionEntity>(
                    cliente.OperacionId.ToString(), cliente.TransaccionId.ToString());

                if (tran != null)
                {
                    List<string> archivos = new()
                    {
                        tran.UrlDoc
                    };

                    byte[] fileData = await AppendFiles(myBlob, archivos.ToArray());

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

                    if (tran!.Flujo! == Flujos.Visualizado)
                    {
                        queueAttach.Add(fileName);
                    } 
                    else
                    {
                        queueSign.Add(fileName);
                    }

                    // Borrar el temp
                    await blobDelete.DeleteIfExistsAsync();
                }
            }
        }

        public static async Task<byte[]> AppendFiles(Stream pdf1MemoryStream, string[] groupFiles)
        {
            using var memoryStream = new MemoryStream();

            using (var pdfDocument = new PdfDocument(new PdfWriter(memoryStream)))
            {
                var pdf1 = new PdfDocument(new PdfReader(pdf1MemoryStream));

                var merger = new PdfMerger(pdfDocument);

                merger.Merge(pdf1, 1, pdf1.GetNumberOfPages());

                foreach (var filePdf in groupFiles)
                {
                    byte[] pdfbytes = await DownloadFile(filePdf);

                    var pdfMemoryStream = new MemoryStream(pdfbytes);

                    var pdfFile = new PdfDocument(new PdfReader(pdfMemoryStream));

                    merger.Merge(pdfFile, 1, pdfFile.GetNumberOfPages());
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

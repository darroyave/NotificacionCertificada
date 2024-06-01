using iText.Forms.Fields;
using iText.Forms;
using iText.Kernel.Pdf;
using iText.Layout.Element;
using Microsoft.Extensions.Logging;
using NotificacionCertificada.Shared.Messages;
using NotificacionCertificada.Shared.Tables;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Azure.Data.Tables;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using NotificacionCertificada.Shared.Utils;
using System;

namespace NotificacionCertificada
{
    public class ValidartFill
    {
        [FunctionName("ValidartFill")]
        public async Task Run(
            [QueueTrigger("notificacioncertificada-fill")] MessageFillCola messageColaFill,
            [Table("nctransacciones")] TableClient tableTransaccion,
            [Blob("templates/template_sms_recibido.pdf", FileAccess.Read)] Stream blobStreamTemplateRecibido,
            [Blob("templates/template_sms_error.pdf", FileAccess.Read)] Stream blobStreamTemplateError,
            [Blob("templates/template_sms_visualizado.pdf", FileAccess.Read)] Stream blobStreamTemplateVisualizado,
            [Blob("temp/{rand-guid}.pdf", FileAccess.Write)] BlobClient blobStream,
            [Table("ncclientes")] TableClient tableCliente,
            [Queue("notificacioncertificada-sign")] ICollector<string> queueSign,
            [Queue("notificacioncertificada-append")] ICollector<string> queueAppend,
            ILogger log)
        {
            log.LogInformation($"C# Queue trigger function processed: ValidartFill");

            TransaccionEntity tran = await tableTransaccion.GetEntityAsync<TransaccionEntity>(
                messageColaFill.OperacionId.ToString(), messageColaFill.TransaccionId.ToString());

            if (tran != null)
            {
                Dictionary<string, string> variables = new Dictionary<string, string>();

                if (tran!.Flujo! == Flujos.Callback)
                {
                    variables.Add("OperacionId", messageColaFill.OperacionId.ToString());
                    variables.Add("TransaccionId", tran.TransaccionRecibidoId.ToString());
                    variables.Add("SMSId", tran.MessageId!);
                    variables.Add("FechaCreacion", tran.FechaCreacion.ToString());
                    variables.Add("NombreIniciador", tran.NameFrom ?? "");
                    variables.Add("NombreDestinatario", tran.NameTo ?? "");
                    variables.Add("CelularDestinatario", $"+{tran.Indicative}{tran.PhoneNumber!}");
                    variables.Add("Asunto", tran.Content!);
                    variables.Add("FechaEnviado", tran.SentAt!);
                    variables.Add("FechaEntregado", tran.DoneAt!);
                    variables.Add("TransaccionIdEntregado", tran.TransaccionRecibidoId.ToString());
                    variables.Add("TransaccionMensajeEntregado", tran.MessageStatus!);
                }
                else if (tran!.Flujo! == Flujos.Visualizado)
                {
                    variables.Add("OperacionId", messageColaFill.OperacionId.ToString());
                    variables.Add("TransaccionId", tran.TransaccionRecibidoId.ToString());
                    variables.Add("SMSId", tran.MessageId!);
                    variables.Add("FechaCreacion", tran.FechaCreacion.ToString());
                    variables.Add("NombreIniciador", tran.NameFrom ?? "");
                    variables.Add("NombreDestinatario", tran.NameTo ?? "");
                    variables.Add("CelularDestinatario", $"+{tran.Indicative}{tran.PhoneNumber!}");
                    variables.Add("Asunto", tran.Content!);
                    variables.Add("FechaEnviado", tran.SentAt!);
                    variables.Add("FechaEntregado", tran.DoneAt!);
                    variables.Add("TransaccionIdEntregado", tran.TransaccionRecibidoId.ToString());
                    variables.Add("TransaccionMensajeEntregado", tran.MessageStatus!);
                    variables.Add("FechaNotificado", tran.FechaVisualizado.ToString());
                    variables.Add("TransaccionIdNotificado", tran.TransaccionVisualizadoId.ToString());
                    variables.Add("DireccionIP", tran.IpVisualizado);
                    variables.Add("Navegador", tran.NavegadorVisualizado);
                }
                else { // Error

                    variables.Add("OperacionId", messageColaFill.OperacionId.ToString());
                    variables.Add("TransaccionId", tran.TransaccionRecibidoId.ToString());
                    variables.Add("SMSId", tran.MessageId!);
                    variables.Add("FechaCreacion", tran.FechaCreacion.ToString());
                    variables.Add("NombreIniciador", tran.NameFrom ?? "");
                    variables.Add("NombreDestinatario", tran.NameTo ?? "");
                    variables.Add("CelularDestinatario", $"+{tran.Indicative}{tran.PhoneNumber!}");
                    variables.Add("Asunto", tran.Content!);
                    variables.Add("FechaEnviado", tran.SentAt);
                    variables.Add("FechaNoEntregado", tran.DoneAt);
                    variables.Add("TransaccionIdNoEntregado", tran.TransaccionRecibidoId.ToString());

                    string error = "No entregado por vencimiento de tiempo luego de 48 Horas.";

                    if (tran.ErrorCola!.Equals("ValidarSMS"))
                        error = tran.MessageName!;
                    else if(tran.ErrorCola.Equals("ValidarCallbackOperador"))
                        error = tran.MessageStatus!;

                    variables.Add("TransaccionMensajeNoEntregado", error);
                }
                
                byte[] fileData;

                if (tran!.Flujo! == Flujos.Callback)
                {
                    fileData = FillForm(blobStreamTemplateRecibido, variables);
                }
                else if (tran!.Flujo! == Flujos.Visualizado)
                {
                    fileData = FillForm(blobStreamTemplateVisualizado, variables);
                }
                else
                {
                    fileData = FillForm(blobStreamTemplateError, variables);
                }

                using var ms = new MemoryStream(fileData, false);

                await blobStream.UploadAsync(ms, new BlobHttpHeaders { ContentType = "application/pdf" });

                string outputBlobUrl = blobStream.Uri.ToString();

                string fileName = Path.GetFileName(outputBlobUrl);
                string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(outputBlobUrl);

                var clientEntity = new ClientEntity()
                {
                    PartitionKey = "file",
                    RowKey = fileNameWithoutExtension,
                    OperacionId = messageColaFill.OperacionId,
                    TransaccionId = messageColaFill.TransaccionId
                };

                await tableCliente.AddEntityAsync(clientEntity);

                if (tran.ProductCode.Equals(ProductCertificado.SMSUrl) && 
                    (tran!.Flujo! == Flujos.Visualizado || tran!.Flujo! == Flujos.Callback))
                {
                    queueAppend.Add(fileName);
                }
                else
                {
                    queueSign.Add(fileName);
                }

            } 
        }

        public static byte[] FillForm(Stream pdfMemoryStream, Dictionary<string, string> variables)
        {
            using var memoryStream = new MemoryStream();

            using (var pdfDocument = new PdfDocument(new PdfReader(pdfMemoryStream), new PdfWriter(memoryStream)))
            {
                PdfAcroForm form = PdfAcroForm.GetAcroForm(pdfDocument, true);
                var fields = form.GetAllFormFields();

                foreach (var dict in variables)
                {
                    fields.TryGetValue(dict.Key, out PdfFormField? toSet);

                    if (toSet != null && !string.IsNullOrEmpty(dict.Value))
                    {

                        string richText = dict.Value;

                        toSet.SetValue(richText);

                    }

                }

                form.FlattenFields();
            }

            return memoryStream.ToArray();
        }
    }
}

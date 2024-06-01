using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using NotificacionCertificada.Shared.Tables;
using Azure.Data.Tables;
using Newtonsoft.Json;
using System.IO;
using NotificacionCertificada.Shared.Models;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using NotificacionCertificada.Shared.Messages;
using Azure.Storage.Queues;
using NotificacionCertificada.Shared.Utils;
using iText.Kernel.Pdf;
using iText.Kernel.Utils;
using System.Net.Http;
using NotificacionCertificada.Seguridad;

namespace NotificacionCertificada
{
    public static class ValidartNotificacionCertificada
    {

        [FunctionName("ValidartCreate")]
        public static async Task<ActionResult<ResultViewModel>> Create(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "create")] HttpRequest req,
            [Table("ncoperaciones")] TableClient tableOperacion,
            [Table("nctransacciones")] TableClient tableTransaccion,
            [Table("ncpendiente")] TableClient tablePendiente,
            [Blob("templates/pdf_test.pdf", FileAccess.Read)] Stream blobStreamTest,
            [Table("entidades")] TableClient tableEntidad,
            [Queue("notificacioncertificada-sms")] QueueClient queueSMS,
            [Queue("notificacioncertificada-password")] QueueClient queuePassword,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger ValidartCreate processed a request: ValidartCreate");

            ValidateJWT auth = new ValidateJWT(req);
            if (!auth.IsValid)
            {
                return new UnauthorizedResult(); // No authentication info.
            }

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            OperacionCreateViewModel? model = JsonConvert.DeserializeObject<OperacionCreateViewModel>(requestBody);

            if(model == null || model.Messages == null || model.Messages.Length == 0) {

                return new BadRequestObjectResult("No hay mensajes para enviar");
            }

            var messages = new List<MessageCreateModel>();

            var errors = new List<string>();

            foreach (var m in model.Messages)
            {
                if (string.IsNullOrEmpty(m.Content))
                {
                    errors.Add($"{m.PhoneNumber} content: Vacío");
                }
                else if (string.IsNullOrEmpty(m.ProductCode))
                {
                    errors.Add($"{m.PhoneNumber} product - code: Vacío");
                }
                else if (!(m.ProductCode.Equals(ProductCertificado.SMSSimple) ||
                     m.ProductCode.Equals(ProductCertificado.SMSUrl)))
                {
                    errors.Add($"{m.PhoneNumber} product - code: Error");
                }
                else if ((m.ProductCode.Equals(ProductCertificado.SMSSimple) || m.ProductCode.Equals(ProductCertificado.SMSUrl)) &&
                    (string.IsNullOrEmpty(m.PhoneNumber) || !IsNumberValid(m.PhoneNumber)))
                {
                    errors.Add("PhoneNumber NULL");
                }
                else if ((m.ProductCode.Equals(ProductCertificado.SMSSimple) ||
                    m.ProductCode.Equals(ProductCertificado.SMSUrl)) && !(m.Indicative > 0 && m.Indicative < 1000))
                {
                    errors.Add($"{m.PhoneNumber} indicative: No válido");
                }
                else if (!(m.ProductCode.Equals(ProductCertificado.SMSSimple) ||
                     m.ProductCode.Equals(ProductCertificado.SMSUrl)))
                {
                    errors.Add($"{m.PhoneNumber} product - code: Error");
                }
                else if (m.ProductCode.Equals(ProductCertificado.SMSUrl) && 
                    (m.UrlDoc == null || !IsUrlValid(m.UrlDoc)))
                {
                    errors.Add($"{m.PhoneNumber} urlDoc: No válida");
                }
                else if (!string.IsNullOrEmpty(m.EmailFrom) && !IsEmailValid(m.EmailFrom))
                {
                    errors.Add($"{m.EmailFrom} E-Mail From - No válido");
                }
                else if ((m.ProductCode.Equals(ProductCertificado.SMSSimple) || m.ProductCode.Equals(ProductCertificado.SMSUrl)) &&
                   (m.Indicative == 57 && m.PhoneNumber.Length != 10 && m.PhoneNumber[0] != '3'))
                {
                    errors.Add($"{m.PhoneNumber}: Longitud no válida");
                }

                // Validar si el pdf Adjunto se puede concatenar
                if (m.ProductCode.Equals(ProductCertificado.SMSUrl))
                {
                    try
                    {
                        byte[] pdfResult = await AppendFiles(m.UrlDoc!, new Stream[] { blobStreamTest });

                        if (pdfResult == null)
                        {
                            errors.Add($"{m.PhoneNumber} urlDoc: No válido");
                        }
                    }
                    catch (Exception)
                    {
                    }
                }

                messages.Add(new MessageCreateModel()
                {
                    TransaccionId = Guid.NewGuid(),
                    Code = m.Code,
                    Content = m.Content,
                    Indicative = m.Indicative,
                    PhoneNumber = m.PhoneNumber,
                    ProductCode = m.ProductCode,
                    UrlDoc = m.UrlDoc,
                    Email = m.Email,
                    Subject = m.Subject,
                    PassDoc = m.PassDoc,
                    NameTo = m.NameTo,
                    NameFrom = m.NameFrom,
                    EmailFrom = m.EmailFrom,
                    TransaccionRecibidoId = Guid.NewGuid(),
                    TransaccionVisualizadoId = Guid.NewGuid(),
                });
            }

            if (errors.Count > 0)
            {
                return new BadRequestObjectResult(new ResultViewModel()
                {
                    Total = 0,
                    Message = "Hay errores en el mensaje",
                    Estado = "Rechazada",
                    Errors = errors.ToArray()
                });
            }

            var entidadId = auth.EntidadId;

            var fechaTrans = DateTime.UtcNow.AddHours(Constantes.HoraColombia);
            // =====================================

            Guid operationId = Guid.NewGuid();

            var operacionEntity = new OperacionEntity()
            {
                PartitionKey = entidadId.ToString(),
                RowKey = operationId.ToString(),
                Fecha = fechaTrans,
                Total = messages.Count,
                Callback = model.CallBack
            };

            await tableOperacion.AddEntityAsync(operacionEntity);

            var entidadData = await tableEntidad.GetEntityIfExistsAsync<EntidadEntity>(
               "entidad", entidadId.ToString().ToUpper());

            bool notifyEmail = false;

            if (entidadData.HasValue)
            {
                notifyEmail = entidadData.Value.NotifyEmail;
            }

            foreach (var msg in messages)
            {
                var transaccionSMS = new TransaccionEntity()
                {
                    PartitionKey = operationId.ToString(),
                    RowKey = msg.TransaccionId.ToString(),
                    Code = msg.Code,
                    Content = msg.Content,
                    Email = msg.Email,
                    Subject = msg.Subject,
                    ProductCode = msg.ProductCode,
                    PhoneNumber = msg.PhoneNumber,
                    Indicative = msg.Indicative,
                    UrlDoc = msg.UrlDoc,
                    PassDoc = msg.PassDoc,
                    NameTo = msg.NameTo,
                    NameFrom = msg.NameFrom,
                    EmailFrom = msg.EmailFrom,
                    EntidadId = entidadId,
                    CallbackClient = model.CallBack,
                    IpVisualizado = "",
                    NavegadorVisualizado = "",
                    TransaccionRecibidoId = msg.TransaccionRecibidoId,
                    TransaccionVisualizadoId = msg.TransaccionVisualizadoId,
                    CodeCertificate = model.CodeCertificate,
                    Flujo = Flujos.Init,
                    FechaCreacion = DateTime.UtcNow.AddHours(Constantes.HoraColombia),
                    SentAt = DateTime.UtcNow.AddHours(Constantes.HoraColombia).ToString(),
                    Visualizado = false,
                    NotifyEmail = notifyEmail
                };

                await tableTransaccion.AddEntityAsync(transaccionSMS);

                int horas = 48;
                if(msg.ProductCode.Equals(ProductCertificado.EmailSimple) ||
                    msg.ProductCode.Equals(ProductCertificado.EmailUrl))
                {
                    horas = 24;
                }

                var sondaEntity = new PendienteEntity()
                {
                    PartitionKey = operationId.ToString(),
                    RowKey = msg.TransaccionId.ToString(),
                    Fecha = DateTime.UtcNow.AddHours(horas), 
                };

                await tablePendiente.AddEntityAsync(sondaEntity);
            }

            var resultMgs = new List<ResultMessageViewModel>();

            foreach (var msg in messages)
            {
                if (msg.ProductCode.Equals(ProductCertificado.SMSSimple) ||
                    msg.ProductCode.Equals(ProductCertificado.SMSUrl))
                {
                    var messageSMS = new MessageSMSCola()
                    {
                        OperacionId = operationId,
                        TransaccionId = msg.TransaccionId,
                        EntidadId = entidadId,
                        ProductCode = msg.ProductCode
                    };

                    var json = JsonConvert.SerializeObject(messageSMS);
                    await queueSMS.SendMessageAsync(json);
                }

                if (!string.IsNullOrEmpty(msg.UrlDoc) && !string.IsNullOrEmpty(msg.PassDoc))
                {
                    var messagePassword = new MessagePasswordCola()
                    {
                        OperacionId = operationId,
                        TransaccionId = msg.TransaccionId
                    };

                    var jsonPassword = JsonConvert.SerializeObject(messagePassword);
                    await queuePassword.SendMessageAsync(jsonPassword);
                }

                resultMgs.Add(new ResultMessageViewModel()
                {
                    Code = msg.Code,
                    TransaccionId = msg.TransaccionId,
                    ProductCode = msg.ProductCode,
                    PhoneNumber = $"{msg.Indicative}{msg.PhoneNumber}",
                    TransaccionRecibidoId = msg.TransaccionRecibidoId,
                    TransaccionVisualizadoId = msg.TransaccionVisualizadoId
                }); 
            }

            var result = new ResultViewModel
            {
                OperationId = operationId,
                Total = messages.Count,
                Estado = "Pendiente",
                Transacciones = resultMgs.ToArray()
            };

            return new OkObjectResult(result);
        }

        [FunctionName("ValidartInfo")]
        public static async Task<ActionResult<OperacionInfoViewModel>> Info(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "info/{operacionId}")] HttpRequest req,
            [Table("ncoperaciones")] TableClient tableOperacion,
            [Table("nctransacciones")] TableClient tableTransaccion,
            Guid operacionId,
            ILogger log)
        {
            ValidateJWT auth = new ValidateJWT(req);
            if (!auth.IsValid)
            {
                return new UnauthorizedResult(); // No authentication info.
            }

            var entidadId = auth.EntidadId;
            //var entidadId = Guid.Parse("2edd94b0-ea1f-40ff-965f-08d87b99bdf7");

            var result = new OperacionInfoViewModel();

            OperacionEntity operacion = await tableOperacion.GetEntityAsync<OperacionEntity>(
               entidadId.ToString(), operacionId.ToString());

            if (operacion != null)
            {
                var trans = tableTransaccion.QueryAsync<TransaccionEntity>
                    (filter: $"PartitionKey eq '{operacionId}'");

                result.Fecha = operacion.Fecha;
                result.Total = operacion.Total;

                var items = new List<OperacionInfoTranViewModel>();

                await foreach (var entity in trans)
                {
                    var item = new OperacionInfoTranViewModel()
                    {
                        TransaccionId = Guid.Parse(entity.RowKey),
                        ProductCode = entity.ProductCode,
                        Code = entity.Code,
                        Content = entity.Content,
                        Flujo = entity.Flujo,
                        Indicative = entity.Indicative,
                        PhoneNumber = entity.PhoneNumber,
                        Email = entity.Email,
                        Subject = entity.Subject,
                        TransaccionRecibidoId = entity.TransaccionRecibidoId,
                        TransaccionVisualizadoId = entity.TransaccionVisualizadoId,
                        UrlPdfRecibido = entity.UrlPdfRecibido,
                        UrlPdfVisualizado = entity.UrlPdfVisualizado,
                        UrlPdfError = entity.UrlPdfError,
                        MessageName = entity.MessageName,
                        ErrorCola = entity.ErrorCola,
                        Visualizado = entity.Visualizado
                    };

                    items.Add(item);
                }

                result.Transacciones = items;
            }

            return new OkObjectResult(result);
        }

        public static async Task<byte[]> AppendFiles(string pdfOrigen, Stream[] groupFiles)
        {
            byte[] pdf1bytes = await DownloadBytesAsync(pdfOrigen);

            using var memoryStream = new MemoryStream();

            var pdf1MemoryStream = new MemoryStream(pdf1bytes);

            using (var pdfDocument = new PdfDocument(new PdfWriter(memoryStream)))
            {
                var pdf1 = new PdfDocument(new PdfReader(pdf1MemoryStream));

                var merger = new PdfMerger(pdfDocument);

                merger.Merge(pdf1, 1, pdf1.GetNumberOfPages());

                foreach (var file in groupFiles)
                {
                    var pdfFile = new PdfDocument(new PdfReader(file));

                    merger.Merge(pdfFile, 1, pdfFile.GetNumberOfPages());
                }
            }

            return memoryStream.ToArray();
        }

        private static async Task<byte[]> DownloadBytesAsync(string url)
        {
            using HttpClient client = new();
            
            byte[] result = await client.GetByteArrayAsync(url);

            return result;
        }

        public static bool IsNumberValid(string text)
        {
            string pattern = @"^\d+$";
            Regex reg = new Regex(pattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);
            return reg.IsMatch(text);
        }

        public static bool IsUrlValid(string url)
        {
            if (string.IsNullOrEmpty(url))
                return false;

            string pattern = @"^(ht|f)tp(s?)\:\/\/[0-9a-zA-Z]([-.\w]*[0-9a-zA-Z])*(:(0-9)*)*(\/?)([a-zA-Z0-9\-\.\?\,\'\/\\\+&%\$#_]*)?$";
            Regex reg = new(pattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);
            return reg.IsMatch(url);
        }

        public static bool IsEmailValid(string email)
        {
            string pattern = @"^\s*[\w\-\+_']+(\.[\w\-\+_']+)*\@[A-Za-z0-9]([\w\.-]*[A-Za-z0-9])?\.[A-Za-z][A-Za-z\.]*[A-Za-z]$";
            Regex reg = new(pattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);
            return reg.IsMatch(email);
        }
    }
}

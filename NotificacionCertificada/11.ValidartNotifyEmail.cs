using Azure.Data.Tables;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using NotificacionCertificada.Shared.Messages;
using NotificacionCertificada.Shared.Models;
using NotificacionCertificada.Shared.Tables;
using NotificacionCertificada.Shared.Utils;
using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace NotificacionCertificada
{
    public class ValidartNotifyEmail
    {
        private static string? MailUrl => Environment.GetEnvironmentVariable("MailJetUrl");
        private static string? MailToken => Environment.GetEnvironmentVariable("MailJetToken");
        private static string? MailFrom => Environment.GetEnvironmentVariable("MailJetFrom");

        [FunctionName("ValidartNotifyEmail")]
        public async Task Run(
            [QueueTrigger("notificacioncertificada-notify-email")] MessageNotifyEmailCola messageNotifyEmailCola,
            [Blob("templates/sms_enviado.html")] string SMSTemplateEnviado,
            [Blob("templates/sms_visualizado.html")] string SMSTemplateVisualizado,
            [Blob("templates/sms_error.html")] string SMSTemplateError,
            [Table("nctransacciones")] TableClient tableTransaccion,
            ILogger log)
        {
            log.LogInformation("C# Queue trigger function processed: ValidartNotifyEmail");

            TransaccionEntity tran = await tableTransaccion.GetEntityAsync<TransaccionEntity>(
                messageNotifyEmailCola.OperacionId.ToString(), messageNotifyEmailCola.TransaccionId.ToString());

            if (tran != null)
            {
                string contentHtml;

                string templateMail = SMSTemplateEnviado;

                if (tran.Flujo == Flujos.Visualizado)
                {
                    templateMail = SMSTemplateVisualizado;
                }
                else if (tran.Flujo == Flujos.Error)
                {
                    templateMail = SMSTemplateError;
                }

                contentHtml = templateMail.Replace("%Asunto%", tran.Subject);

                contentHtml = contentHtml.Replace("%NumeroCelular%", $"+{tran.Indicative}{tran.PhoneNumber}");
                
                contentHtml = contentHtml.Replace("%FechaRecibido%", tran.DoneAt);

                contentHtml = contentHtml.Replace("%IDMensaje%", messageNotifyEmailCola.OperacionId.ToString());
                
                string flujo = "Entregado";

                if(tran.Flujo == Flujos.Visualizado)
                {
                    flujo = "Notificado";
                } 
                else if(tran.Flujo == Flujos.Error)
                {
                    flujo = "No entregado";
                }

                try
                {
                    await SendMailMailJetAttach(MailUrl, MailToken, MailFrom,
                            tran.EmailFrom, tran.Subject, flujo,
                            contentHtml, contentHtml, messageNotifyEmailCola.Url);
                } 
                catch
                {
                }

            }
        }

        public async Task<MailJetResponseViewModel> SendMailMailJetAttach(
            string url, string token, string from, string to, 
            string subject, string flujo, string content, string contentHtml, string urlAdjunto)
        {
            byte[] bytes = await DownloadFile(urlAdjunto);

            string filename = urlAdjunto.Split('/').Last();

            var emailViewModel = new MailJetViewModel()
            {
                Messages = new MailJetMessageViewModel[]
               {
                    new MailJetMessageViewModel()
                    {
                        From = new MailJetInfoViewModel()
                        {
                            Email = from,
                            Name = $"Alerta {flujo}"
                        },
                        To = new MailJetInfoViewModel[]
                        {
                           new MailJetInfoViewModel()
                           {
                                Email = to,
                                Name = "ValidarT"
                           }
                        },
                        Subject = subject,
                        TextPart = content,
                        HTMLPart = contentHtml,
                        Attachments = new MailJetAttachmentViewModel[]
                        {
                            new MailJetAttachmentViewModel()
                            {
                                ContentType = "application/pdf",
                                Filename = filename,
                                Base64Content = Convert.ToBase64String(bytes)
                            }
                        }
                    }
               }
            };

            var json = JsonConvert.SerializeObject(emailViewModel);

            var response = await SendPost(token, url, json);

            var mailjetResponse = JsonConvert.DeserializeObject<MailJetResultViewModel>(response);

            var mailJetResponseViewModel = new MailJetResponseViewModel()
            {
                Status = "init"
            };

            if (mailjetResponse.Messages != null && mailjetResponse.Messages.Length > 0)
            {
                var message = mailjetResponse.Messages[0];

                var status = message.Status;

                mailJetResponseViewModel.Status = status;

                if (status == "success")
                {

                    if (message.To != null && message.To.Length > 0)
                    {
                        mailJetResponseViewModel.MessageID = message.To[0].MessageID.ToString();
                        mailJetResponseViewModel.MessageHref = message.To[0].MessageHref;
                    }

                }
                else if (status == "error")
                {
                    mailJetResponseViewModel.Errors = mailjetResponse.Messages[0].Errors;
                }

            }

            return mailJetResponseViewModel;
        }

        public static async Task<byte[]> DownloadFile(string url)
        {
            HttpClient _httpClient = new();
            byte[] fileBytes = await _httpClient.GetByteArrayAsync(url);
            return fileBytes;
        }

        public static async Task<string> SendPost(string token, string url, string json)
        {
            var data = new StringContent(json, Encoding.UTF8, "application/json");

            using var client = new HttpClient();

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", token);

            var response = await client.PostAsync(url, data);

            var result = await response.Content.ReadAsStringAsync();

            return result;
        }
    }
}

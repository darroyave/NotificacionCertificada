using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Azure.Data.Tables;
using NotificacionCertificada.Shared.Messages;
using Microsoft.Extensions.Logging;
using NotificacionCertificada.Shared.Tables;
using System;
using NotificacionCertificada.Shared.Utils;
using NotificacionCertificada.Shared.Options;
using Newtonsoft.Json;
using NotificacionCertificada.Shared.Models;
using System.Net.Http;
using System.Text;
using System.Threading;
using Azure.Storage.Queues;
using System.Collections.Generic;
using NotificacionCertificada.Models;
using System.Net.Http.Headers;

namespace NotificacionCertificada
{
    public static class ValidartSMS
    {
        private static readonly string? COLOMBIARED_URL = Environment.GetEnvironmentVariable("ColombiaRedUrl");
        private static readonly string? COLOMBIARED_TOKEN = Environment.GetEnvironmentVariable("ColombiaRedToken");
        private static readonly string? COLOMBIARED_CALLBACK = Environment.GetEnvironmentVariable("ColombiaRedCallback");

        private static readonly string? UrlDownloadFile = Environment.GetEnvironmentVariable("UrlDownloadFile");
        private static readonly string? ShortUrlService = Environment.GetEnvironmentVariable("ShortUrlService");

        private static readonly string? ValidartURL = Environment.GetEnvironmentVariable("ValidartURL");
        private static readonly string? ValidartClientId = Environment.GetEnvironmentVariable("ValidartClientId");
        private static readonly string? ValidartClientSecret = Environment.GetEnvironmentVariable("ValidartClientSecret");

        [FunctionName("ValidartSMS")]
        public static async Task Run(
            [QueueTrigger("notificacioncertificada-sms")] MessageSMSCola messageColaSMS,
            [Table("nctransacciones")] TableClient tableTransaccion,
            [Table("ncclientes")] TableClient tableCliente,
            [Table("ncpendiente")] TableClient tablePendiente,
            [Queue("notificacioncertificada-fill")] QueueClient queueFill,
            ILogger log)
        {
            log.LogInformation($"C# Queue trigger function processed: ValidartSMS");

            TransaccionEntity tran = await tableTransaccion.GetEntityAsync<TransaccionEntity>(
                messageColaSMS.OperacionId.ToString(), messageColaSMS.TransaccionId.ToString());

            if (tran != null)
            {
                string messageContent = tran.Content!;

                if (messageColaSMS!.ProductCode!.Equals(ProductCertificado.SMSUrl))
                {
                    Guid codeClient = Guid.NewGuid();

                    var clientEntity = new ClientEntity()
                    {
                        PartitionKey = "client",
                        RowKey = codeClient.ToString(),
                        OperacionId = messageColaSMS.OperacionId,
                        TransaccionId = messageColaSMS.TransaccionId
                    };

                    await tableCliente.AddEntityAsync(clientEntity);

                    log.LogInformation($"C# Queue trigger function processed: {UrlDownloadFile}/{codeClient}");

                    var token = await ValidartToken(ValidartURL, ValidartClientId, ValidartClientSecret);

                    var shorterURLResponse = await ShorterURL(ShortUrlService!, token, $"{UrlDownloadFile}?codeclient={codeClient}");

                    messageContent = $"{messageContent}. Para ver y guardar dar clic al link {shorterURLResponse.ShortUrl}";
                }

                // Api Colombia Red
                var colombiaRedResponse = await SendMesageEventAsync(new SMSOptions()
                {
                    Url = COLOMBIARED_URL!,
                    Token = COLOMBIARED_TOKEN!,
                    PhoneNumber = $"{tran.Indicative}{tran.PhoneNumber}",
                    Text = messageContent!,
                    From = "ValidartT",
                    CallBack = COLOMBIARED_CALLBACK!
                });

                if (colombiaRedResponse != null && colombiaRedResponse.Messages != null && 
                    colombiaRedResponse.Messages.Length > 0)
                {
                    string messageName = colombiaRedResponse.Messages[0]!.Status!.Name!;
                    string messageId = colombiaRedResponse.Messages[0]!.MessageId!;
                    string groupName = colombiaRedResponse.Messages[0]!.Status!.GroupName!;

                    string message;
                    if (messageName.Equals("PENDING_ENROUTE"))
                    {
                        message = groupName;

                        var clientMessageEntity = new ClientEntity()
                        {
                            PartitionKey = "message",
                            RowKey = messageId,
                            OperacionId = messageColaSMS.OperacionId,
                            TransaccionId = messageColaSMS.TransaccionId
                        };

                        await tableCliente.AddEntityAsync(clientMessageEntity);
                    }
                    else
                    {
                        await tablePendiente.DeleteEntityAsync(tran.PartitionKey, tran.RowKey);

                        tran.Flujo = Flujos.Error;

                        tran.SentAt = DateTime.UtcNow.AddHours(Constantes.HoraColombia).ToString();
                        tran.DoneAt = DateTime.UtcNow.AddHours(Constantes.HoraColombia).ToString();
                         
                        tran.ErrorCola = "ValidarSMS";

                        message = groupName + " " + messageName;
                    }

                    // Update transacción
                    tran.MessageId = messageId;
                    tran.MessageName = message;
                    tran.MessageJson = JsonConvert.SerializeObject(colombiaRedResponse);

                    await tableTransaccion.UpdateEntityAsync(tran, tran.ETag);

                    if(tran.Flujo == Flujos.Error)
                    {
                        // Generar PDF de error

                        var messageFill = new MessageFillCola()
                        {
                            OperacionId = Guid.Parse(tran.PartitionKey),
                            TransaccionId = Guid.Parse(tran.RowKey),
                            EntidadId = tran.EntidadId,
                            ProductCode = tran.ProductCode
                        };

                        var json = JsonConvert.SerializeObject(messageFill);
                        await queueFill.SendMessageAsync(json);

                    }
                }
            }
            
        }

        private static async Task<ColombiaRedResponse?> SendMesageEventAsync(SMSOptions smsOptions, CancellationToken ct = default)
        {
            var request = new ColombiaRedAvanzadoRequest()
            {
                Messages = new ColombiaRedAvanzadoMessageRequest[]
                    {
                        new()
                        {
                            Text = smsOptions.Text,
                            From = smsOptions.From,
                            NotifyUrl = smsOptions.CallBack,
                            NotifyContentType = "application/json",
                            Destinations = new[]
                            {
                                new ColombiaRedAvanzadoDestinationRequest()
                                {
                                    To = smsOptions.PhoneNumber
                                }
                            }
                        }
                    }
            };

            var json = JsonConvert.SerializeObject(request);
            var data = new StringContent(json, Encoding.UTF8, "application/json");

            using var httpClient = new HttpClient();

            httpClient.DefaultRequestHeaders.Add("Authorization", $"Basic {smsOptions.Token}");

            var httpResponseMessage = await httpClient.PostAsync($"{smsOptions.Url}/sms/1/text/advanced", data, ct);

            if (httpResponseMessage.IsSuccessStatusCode)
            {
                var responseContentStr = await httpResponseMessage.Content.ReadAsStringAsync(ct);

                var colombiaRedResponse = JsonConvert.DeserializeObject<ColombiaRedResponse>(responseContentStr);

                return colombiaRedResponse;
            }

            return null;
        }

        public static async Task<ShorterURLResponse?> ShorterURL(string urlService, string token, string url)
        {
            using var httpClient = new HttpClient();

            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var data = new UrlModel()
            {
                Url = url
            };

            var json = JsonConvert.SerializeObject(data);

            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await httpClient.PostAsync(urlService, content);

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();

                var responseObject = JsonConvert.DeserializeObject<ShorterURLResponse>(responseContent);

                return responseObject;
            }

            return null;
        }

        public static async Task<string?> ValidartToken(string url, string clientId, string clientSecret)
        {
            using var httpClient = new HttpClient();

            var data = new Credentials()
            {
                Client_Id = clientId,
                Client_Secret = clientSecret
            };

            var json = JsonConvert.SerializeObject(data);

            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await httpClient.PostAsync(url, content);

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();

                var responseObject = JsonConvert.DeserializeObject<ValidartResponse>(responseContent);

                return responseObject.access_token;
            }

            return null;
        }
    }
}

using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using NotificacionCertificada.Shared.Messages;
using NotificacionCertificada.Shared.Models;
using Newtonsoft.Json;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text;

namespace NotificacionCertificada
{
    public class ValidartNotifyClient
    {
        [FunctionName("ValidartNotifyClient")]
        public async Task Run(
            [QueueTrigger("notificacioncertificada-notify")] MessageNotifyCola messageNotifyCola,
            ILogger log)
        {
            log.LogInformation($"C# Queue trigger function processed: ValidartNotifyClient");

            string json = JsonConvert.SerializeObject(new CallBackClientViewModel()
            {
                OperacionId = messageNotifyCola.OperacionId,
                TransaccionId = messageNotifyCola.TransaccionId,
                Url = messageNotifyCola.UrlPdf,
                Flujo = messageNotifyCola.Flujo
            });

            var data = new StringContent(json, Encoding.UTF8, "application/json");

            using var httpClient = new HttpClient();

            var httpResponseMessage = await httpClient.PostAsync(messageNotifyCola.CallbackCliente, data);

            if (httpResponseMessage.IsSuccessStatusCode)
            {

            }
        }
    }
}

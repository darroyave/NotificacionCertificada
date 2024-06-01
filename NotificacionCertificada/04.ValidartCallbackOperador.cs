using Azure.Data.Tables;
using Azure.Storage.Queues;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using NotificacionCertificada.Shared.Messages;
using NotificacionCertificada.Shared.Models;
using NotificacionCertificada.Shared.Tables;
using NotificacionCertificada.Shared.Utils;
using System;
using System.IO;
using System.Threading.Tasks;

namespace NotificacionCertificada
{
    public static class ValidartCallbackOperador
    {
        [FunctionName("ValidartCallbackOperador")]
        public static async Task<ActionResult<string>> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequest req,
            [Table("nccallback")] TableClient tableCallback,
            [Table("ncclientes")] TableClient tableCliente,
            [Table("nctransacciones")] TableClient tableTransaccion,
            [Table("ncpendiente")] TableClient tablePendiente,
            [Queue("notificacioncertificada-fill")] QueueClient queueFill,
            ILogger log
            )
        {
            log.LogInformation("C# HTTP trigger ValidartCreate processed a request: ValidartCallbackOperador");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            CallbackOperadorViewModel? model = JsonConvert.DeserializeObject<CallbackOperadorViewModel>(requestBody);

            await tableCallback.AddEntityAsync(new SMSCallbackEntity()
            {
                PartitionKey = "colombiared",
                RowKey = Guid.NewGuid().ToString(),
                Json = requestBody
            });

            if(model != null && model.Results != null && model.Results.Length > 0 && 
                model.Results[0].Status != null)
            {
                string messageId = model.Results[0].MessageId ?? "";
                string status = model.Results[0]!.Status!.Name ?? "";
                string groupName = model.Results[0]!.Status!.GroupName!;

                ClientEntity cliente = await tableCliente.GetEntityAsync<ClientEntity>(
                        "message", messageId);

                if (cliente != null)
                {
                    TransaccionEntity tran = await tableTransaccion.GetEntityAsync<TransaccionEntity>(
                        cliente.OperacionId.ToString(), cliente.TransaccionId.ToString());

                    if (tran != null && tran.Flujo == Flujos.Init)
                    {
                        await tablePendiente.DeleteEntityAsync(tran.PartitionKey, tran.RowKey);

                        string message;
                        if (status.Equals("DELIVERED_TO_HANDSET")) // GroupName
                        {
                            // Create PDF recibido
                            message = groupName;

                            tran.Flujo = Flujos.Callback;
                        }
                        else
                        {
                            // Create PDF error
                            tran.Flujo = Flujos.Error;

                            tran.ErrorCola = "ValidarCallbackOperador";

                            message = groupName + " " + status;
                        }

                        tran.SentAt = model.Results[0]!.SentAt;
                        tran.DoneAt = model.Results[0]!.DoneAt;
                        tran.MessageStatus = message;
                        tran.MessageJsonCallback = requestBody;

                        await tableTransaccion.UpdateEntityAsync(tran, tran.ETag);

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

            return new OkObjectResult("OK");
        }
    }
}

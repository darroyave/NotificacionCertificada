using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Azure.Data.Tables;
using Newtonsoft.Json;
using NotificacionCertificada.Shared.Models;
using NotificacionCertificada.Shared.Tables;
using NotificacionCertificada.Shared.Utils;
using NotificacionCertificada.Shared.Messages;
using Azure.Storage.Queues;

namespace NotificacionCertificada
{
    public static class ValidartApiRestWeb
    {
        [FunctionName("RestPostClient")]
        public static async Task<IActionResult> PostClient(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "web/{codeClient}")] WebCreateViewModel model,
            [Table("ncclientes")] TableClient tableCliente,
            [Table("nctransacciones")] TableClient tableTransaccion,
            [Queue("notificacioncertificada-fill")] QueueClient queueFill,
            Guid codeClient)
        {
            ClientEntity cliente = await tableCliente.GetEntityAsync<ClientEntity>(
                   "client", codeClient.ToString());

            if(model != null && cliente != null)
            {
                TransaccionEntity tran = await tableTransaccion.GetEntityAsync<TransaccionEntity>(
                        cliente.OperacionId.ToString(), cliente.TransaccionId.ToString());

                if(tran != null)
                {
                    if (tran.Flujo == Flujos.Recibido)
                    {
                        tran.Flujo = Flujos.Visualizado;
                        tran.Visualizado = true;
                        tran.FechaVisualizado = DateTime.UtcNow.AddHours(Constantes.HoraColombia);
                        tran.IpVisualizado = model.IpRecibido;
                        tran.NavegadorVisualizado = model.NavegadorRecibido;

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
                    else if (!tran.Visualizado)
                    {
                        tran.FechaVisualizado = DateTime.UtcNow.AddHours(Constantes.HoraColombia);
                        tran.Visualizado = true;
                        tran.IpVisualizado = model.IpRecibido;
                        tran.NavegadorVisualizado = model.NavegadorRecibido;

                        await tableTransaccion.UpdateEntityAsync(tran, tran.ETag);
                    }

                    return new OkObjectResult("OK");
                }

            }

            return new BadRequestObjectResult("OK");
        }

        [FunctionName("RestGetClient")]
        public static async Task<IActionResult> GetClient(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "web/{codeClient}")] HttpRequest req,
            [Table("ncclientes")] TableClient tableCliente,
            [Table("nctransacciones")] TableClient tableTransaccion,
            Guid codeClient)
        {
            ClientEntity cliente = await tableCliente.GetEntityAsync<ClientEntity>(
                   "client", codeClient.ToString());

            if (cliente != null)
            {
                TransaccionEntity tran = await tableTransaccion.GetEntityAsync<TransaccionEntity>(
                        cliente.OperacionId.ToString(), cliente.TransaccionId.ToString());

                if (tran != null)
                {
                    string? urlDoc = null;
                    if (!string.IsNullOrEmpty(tran.UrlDoc))
                    {
                        urlDoc = string.IsNullOrEmpty(tran.PassDoc) ? tran.UrlDoc : tran.UrlDocEncriptada;
                    }

                    var webModel = new WebModel
                    {
                        UrlDoc = urlDoc,
                        Content = tran.Content!,
                        ProductCode = tran.ProductCode!,
                        Visualizado = tran.Visualizado
                    };

                    return new OkObjectResult(webModel);
                }

            }

            return new BadRequestObjectResult("Error");
        }
    }
}

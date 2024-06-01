using System;
using System.Linq;
using System.Threading.Tasks;
using Azure.Data.Tables;
using Azure.Storage.Queues;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using NotificacionCertificada.Shared.Messages;
using NotificacionCertificada.Shared.Tables;
using NotificacionCertificada.Shared.Utils;

namespace NotificacionCertificada
{
    public class ValidartSonda
    {
        [FunctionName("ValidartSonda")]
        public async Task Run(
            [TimerTrigger("0 0 */1 * * *")] TimerInfo myTimer, // Cada hora
            [Table("ncpendiente")] TableClient tablePendiente,
            [Table("nctransacciones")] TableClient tableTransaccion,
            [Queue("notificacioncertificada-fill")] QueueClient queueFill,
            ILogger log)
        {
            log.LogInformation("C# Timer trigger function executed at: ValidartSMSSonda");

            var currentDate = DateTime.UtcNow;

            try
            {
                var listdatos = tablePendiente.Query<PendienteEntity>().ToList();

                var list = listdatos.Where(l => l.Fecha <= currentDate).ToList();

                foreach (var entity in list)
                {
                    TransaccionEntity tran = await tableTransaccion.GetEntityAsync<TransaccionEntity>(
                        entity.PartitionKey, entity.RowKey);

                    if (tran != null)
                    {
                        tran.Flujo = Flujos.Error;

                        tran.DoneAt = DateTime.UtcNow.AddHours(Constantes.HoraColombia).ToString();

                        tran.ErrorCola = "ValidarSonda";

                        await tableTransaccion.UpdateEntityAsync(tran, tran.ETag);

                        // Borra el registro
                        await tablePendiente.DeleteEntityAsync(entity.PartitionKey, entity.RowKey);

                        // Genera el PDF de error
                        var messageFill = new MessageFillCola()
                        {
                            OperacionId = Guid.Parse(entity.PartitionKey),
                            TransaccionId = Guid.Parse(entity.RowKey),
                            EntidadId = tran.EntidadId,
                            ProductCode = tran.ProductCode
                        };

                        var json = JsonConvert.SerializeObject(messageFill);
                        await queueFill.SendMessageAsync(json);

                    }

                }
            } catch {

            }
        }
    }
}

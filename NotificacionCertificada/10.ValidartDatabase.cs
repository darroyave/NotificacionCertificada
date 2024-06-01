using Azure.Data.Tables;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using NotificacionCertificada.Shared.Messages;
using NotificacionCertificada.Shared.Models;
using NotificacionCertificada.Shared.Tables;
using NotificacionCertificada.Shared.Utils;
using System;
using System.Threading.Tasks;

namespace NotificacionCertificada
{
    public class ValidartDatabase
    {
        [FunctionName("ValidartDatabase")]
        public async Task Run(
            [QueueTrigger("notificacioncertificada-db")] MessageDatabaseCola messageColaDatabase,
            [Table("nctransacciones")] TableClient tableTransaccion,
            [Sql(commandText: "dbo.Transacciones",
                connectionStringSetting: "SqlConnectionString")] IAsyncCollector<TransaccionTable> transacciones,
            ILogger log)
        {
            log.LogInformation($"C# Queue trigger function processed: ValidartDatabase");

            TransaccionEntity tran = await tableTransaccion.GetEntityAsync<TransaccionEntity>(
                messageColaDatabase.OperacionId.ToString(), messageColaDatabase.TransaccionId.ToString());

            if (tran != null)
            {
                var newTran = new TransaccionTable()
                {
                    Id = messageColaDatabase.TransaccionEventoId,
                    CreatedOnUtc = DateTime.UtcNow,
                    DateEvidence = DateTime.UtcNow,
                    StateTransactionId = Estados.OK,
                    EntidadId = tran.EntidadId,
                    ProductId = Products.FirmaElectronicaSMSCertificado,
                    NoAudios = 1,
                    Url = messageColaDatabase.UrlPDF,
                    CallBackClient = tran.CallbackClient
                };

                log.LogInformation("inserting to dbo.Transacciones", newTran);

                await transacciones.AddAsync(newTran);
            }
                
        }
    }
}

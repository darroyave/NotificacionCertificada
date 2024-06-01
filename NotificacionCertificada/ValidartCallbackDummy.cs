using System;
using System.IO;
using System.Threading.Tasks;
using Azure.Data.Tables;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using NotificacionCertificada.Shared.Tables;

namespace NotificacionCertificada
{
    public static class ValidartCallbackClientDummy
    {
        [FunctionName("ValidartCallbackClientDummy")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequest req,
            [Table("ncclientcallback")] TableClient tableClienteCallback,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request: ValidartCallbackClientDummy");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();

            var clientCallback = new ClientCallbackEntity()
            {
                PartitionKey = "callback",
                RowKey = Guid.NewGuid().ToString(),
                Json = requestBody
            };

            await tableClienteCallback.AddEntityAsync(clientCallback);

            return new OkObjectResult("OK");
        }
    }
}

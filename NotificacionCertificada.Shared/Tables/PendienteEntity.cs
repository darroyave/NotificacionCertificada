using Azure;
using Azure.Data.Tables;

namespace NotificacionCertificada.Shared.Tables
{
    public class PendienteEntity : ITableEntity
    {
        //PartitionKey:  operacionId
        //RowKey: transaccionId

        public DateTime Fecha { get; set; }

        public string PartitionKey { get; set; } = "";

        public string RowKey { get; set; } = "";

        public DateTimeOffset? Timestamp { get; set; }

        public ETag ETag { get; set; }
        
    }
}

using Azure;
using Azure.Data.Tables;

namespace NotificacionCertificada.Shared.Tables
{
    public class ClientEntity : ITableEntity
    {
        //PartitionKey:  'client', 'message'
        //RowKey: code, messageId

        public Guid OperacionId { get; set; }

        public Guid TransaccionId { get; set; }

        public string PartitionKey { get; set; } = "";

        public string RowKey { get; set; } = "";

        public DateTimeOffset? Timestamp { get; set; }

        public ETag ETag { get; set; }
    }
}

using Azure;
using Azure.Data.Tables;
using System;

namespace NotificacionCertificada.Shared.Tables
{
    public class OperacionEntity : ITableEntity
    {
        // PartitionKey = entidadId
        // RowKey = operationId

        public DateTime Fecha { get; set; }

        public int Total { get; set; }

        public string? Callback { get; set; }

        public string PartitionKey { get; set; } = "";

        public string RowKey { get; set; } = "";

        public DateTimeOffset? Timestamp { get; set; }

        public ETag ETag { get; set; }
    }
}

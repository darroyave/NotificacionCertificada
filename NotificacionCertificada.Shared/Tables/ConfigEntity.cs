using Azure;
using Azure.Data.Tables;
using System;

namespace NotificacionCertificada.Shared.Tables
{
    public class ConfigEntity : ITableEntity
    {
        //PartitionKey:  'notificacioncertificado'
        //RowKey: config

        public string CerPassword { get; set; } = "";

        public string TSAUrl { get; set; } = "";

        public string TSAUser { get; set; } = "";

        public string TSAPassword { get; set; } = "";

        public int TSAEstimatedSize { get; set; } = 0;

        public string PartitionKey { get; set; } = "";

        public string RowKey { get; set; } = "";

        public DateTimeOffset? Timestamp { get; set; }

        public ETag ETag { get; set; }
    }
}

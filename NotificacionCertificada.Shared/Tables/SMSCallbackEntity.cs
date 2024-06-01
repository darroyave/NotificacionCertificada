﻿using Azure;
using Azure.Data.Tables;

namespace NotificacionCertificada.Shared.Tables
{
    public class SMSCallbackEntity : ITableEntity
    {

        //PartitionKey: 'colombiared'
        //RowKey: Guid

        public string Json { get; set; } = "";

        public string PartitionKey { get; set; } = "";

        public string RowKey { get; set; } = "";

        public DateTimeOffset? Timestamp { get; set; }

        public ETag ETag { get; set; }
    }
}

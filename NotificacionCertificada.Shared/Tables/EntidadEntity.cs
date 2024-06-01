using Azure.Data.Tables;
using Azure;

namespace NotificacionCertificada.Shared.Tables
{
    public class EntidadEntity : ITableEntity
    {
        //PartitionKey: 'entidad'
        //RowKey: Guid
        public string? Name { get; set; }
        public string? EmailAdmin { get; set; }
        public string? Token { get; set; }
        public bool ValidateIP { get; set; }
        public bool Active { get; set; }
        public bool SaldoInfinito { get; set; }
        public int? SelloTiempo { get; set; }
        public bool IsNotDisplayQR { get; set; }
        public string? UrlLogo { get; set; }
        public string? UrlEmailCertificado { get; set; }
        public bool? AllowQueryStorage2 { get; set; }
        public bool? IsNotSendEmail { get; set; }
        public string? HeaderBasic { get; set; }
        public bool? IsNotDisplayWatermark { get; set; }
        public bool NotifyEmail { get; set; } = false;

        public string PartitionKey { get; set; } = "";
        public string RowKey { get; set; } = "";
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }
        
    }
}

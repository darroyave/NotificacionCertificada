using Azure;
using Azure.Data.Tables;

namespace NotificacionCertificada.Shared.Tables
{
    public class TransaccionEntity : ITableEntity
    {
        // PartitionKey = operationId
        // RowKey = transaccionId
        public string? Code { get; set; }
        public string? Content { get; set; }
        public string? ProductCode { get; set; }
        public string? UrlDoc { get; set; }
        public string? UrlDocEncriptada { get; set; }
        public string? PassDoc { get; set; }
        public Guid EntidadId { get; set; }
        public string? CallbackClient { get; set; }
        public int Indicative { get; set; }
        public string? PhoneNumber { get; set; }
        public Guid TransaccionRecibidoId { get; set; }
        public Guid TransaccionVisualizadoId { get; set; }
        public string? CodeCertificate { get; set; }
        public string? MessageId { get; set; }
        public string? MessageJson { get; set; }
        public string? MessageJsonCallback { get; set; }
        public string? MessageName { get; set; }
        public string? MessageStatus { get; set; }
        public string? SentAt { get; set; }
        public string? DoneAt { get; set; }
        public DateTime? FechaVisualizado { get; set; }
        public string? Flujo { get; set; }
        public string? UrlPdfRecibido { get; set; }
        public string? UrlPdfVisualizado { get; set; }
        public string? UrlPdfError { get; set; }
        public string? ErrorCola { get; set; }
        public string? Email { get; set; }
        public string? Subject { get; set; }
        public string? NameFrom { get; set; }
        public string? NameTo { get; set; }
        public string? MessageHref { get; set; }
        public string? EmailFrom { get; set; }
        public string? IpVisualizado { get; set; }
        public string? NavegadorVisualizado { get; set; }
        public DateTime FechaCreacion { get; set; }
        public bool Visualizado { get; set; }

        public string PartitionKey { get; set; } = "";
        public string RowKey { get; set; } = "";
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }
        public bool NotifyEmail { get; set; }
        
    }
}

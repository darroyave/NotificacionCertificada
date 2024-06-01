namespace NotificacionCertificada.Shared.Models
{
    public class OperacionInfoTranViewModel
    {
        public Guid TransaccionId { get; set; }
        public string? Code { get; set; }
        public string? ProductCode { get; set; }
        public string? Content { get; set; }
        public string? Flujo { get; set; }
        public string? Email { get; set; }
        public int Indicative { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Subject { get; set; }
        public Guid TransaccionRecibidoId { get; set; }
        public Guid TransaccionVisualizadoId { get; set; }
        public string? UrlPdfRecibido { get; set; }
        public string? UrlPdfVisualizado { get; set; }
        public string? UrlPdfError { get; set; }
        public string? MessageName { get; set; }
        public string? ErrorCola { get; set; }
        public bool Visualizado { get; set; }
        
    }

    public class OperacionInfoViewModel
    {
        public DateTime Fecha { get; set; }
        public int Total { get; set; }
        public List<OperacionInfoTranViewModel>? Transacciones { get; set; }
    }
}

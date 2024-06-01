using System;

namespace NotificacionCertificada.Shared.Models
{
    public class MessageCreateModel
    {
        public Guid TransaccionId { get; set; }

        public string? Code { get; set; }

        public string? ProductCode { get; set; }

        public string? Content { get; set; }

        public string? UrlDoc { get; set; }

        public int Indicative { get; set; }

        public string? Email { get; set; }

        public string? Subject { get; set; }

        public string? PassDoc { get; set; }

        public string? NameTo { get; set; }

        public string? NameFrom { get; set; }

        public string? EmailFrom { get; set; }

        public string? PhoneNumber { get; set; }

        public Guid TransaccionRecibidoId { get; set; }

        public Guid TransaccionVisualizadoId { get; set; }
        
    }
}

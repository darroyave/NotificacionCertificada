using System;

namespace NotificacionCertificada.Shared.Models
{
    public class ResultMessageViewModel
    {
        public Guid TransaccionId { get; set; }

        public string? Code { get; set; }

        public string? ProductCode { get; set; }

        public string? PhoneNumber { get; set; }
    
        public Guid TransaccionRecibidoId { get; set; }

        public Guid TransaccionVisualizadoId { get; set; }
        
    }
}

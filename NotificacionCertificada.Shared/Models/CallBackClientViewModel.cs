using System;

namespace NotificacionCertificada.Shared.Models
{
    public class CallBackClientViewModel
    {
        public Guid OperacionId { get; set; }
        public Guid TransaccionId { get; set; }
        public string? Flujo { get; set; }
        public string? Url { get; set; }
    }
}

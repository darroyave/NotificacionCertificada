using System;

namespace NotificacionCertificada.Shared.Messages
{
    public class MessageNotifyCola
    {
        public Guid OperacionId { get; set; }
        public Guid TransaccionId { get; set; }
        public string? Flujo { get; set; }
        public string? UrlPdf { get; set; }
        public string? CallbackCliente { get; set; }
    }
}

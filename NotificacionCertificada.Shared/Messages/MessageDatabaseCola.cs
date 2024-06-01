using System;

namespace NotificacionCertificada.Shared.Messages
{
    public class MessageDatabaseCola
    {
        public Guid OperacionId { get; set; }

        public Guid TransaccionId { get; set; }

        public Guid TransaccionEventoId { get; set; }

        public string? UrlPDF { get; set; }
        
    }
}

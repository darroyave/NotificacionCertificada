using System;

namespace NotificacionCertificada.Shared.Messages
{
    public class MessageFillCola
    {
        public Guid OperacionId { get; set; }
        public Guid TransaccionId { get; set; }
        public Guid EntidadId { get; set; }
        public string? ProductCode { get; set; }
    }
}

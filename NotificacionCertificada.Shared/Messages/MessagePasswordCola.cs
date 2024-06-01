using System;

namespace NotificacionCertificada.Shared.Messages
{
    public class MessagePasswordCola
    {
        public Guid OperacionId { get; set; }
        public Guid TransaccionId { get; set; }
    }
}

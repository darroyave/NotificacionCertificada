using System;

namespace NotificacionCertificada.Shared.Messages
{
    public class MessageVisualizadoCola
    {
        public Guid OperacionId { get; set; }
        public Guid TransaccionId { get; set; }
        public Guid EntidadId { get; set; }
        public string? ProductCode { get; set; }
        public string? ColaOrigen { get; set; }
    }
}

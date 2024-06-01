using System;

namespace NotificacionCertificada.Shared.Models
{
    public class TransaccionTable
    {
        public Guid Id { get; set; }

        public DateTime CreatedOnUtc { get; set; }

        public DateTime? DateEvidence { get; set; }

        public Guid StateTransactionId { get; set; }

        public Guid EntidadId { get; set; }

        public Guid ProductId { get; set; }

        public string? CallBackClient { get; set; }

        public int NoAudios { get; set; }

        public string? Url { get; set; }
    }
}

using System;

namespace NotificacionCertificada.Shared.Models
{
    public class ResultViewModel
    {
        public Guid OperationId { get; set; }

        public int Total { get; set; }

        public string? Estado { get; set; }

        public string? Message { get; set; }

        public ResultMessageViewModel[]? Transacciones { get; set; }

        public string[]? Errors { get; set; }
    }
}

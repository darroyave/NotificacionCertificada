namespace NotificacionCertificada.Shared.Models
{
    public class StatusOperadorViewModel
    {
        public string? GroupName { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
    }

    public class ResultOperadorViewModel
    {
        public StatusOperadorViewModel? Status { get; set; }
        public string? MessageId { get; set; }
        public string? To { get; set; }
        public string? SentAt { get; set; }
        public string? DoneAt { get; set; }
    }

    public class CallbackOperadorViewModel
    {
        public ResultOperadorViewModel[]? Results { get; set; }
    }
}

namespace NotificacionCertificada.Shared.Models
{
    public class CallbackMailViewModel
    {
        public string? Event { get; set; }
        public long Time { get; set; }
        public string? MessageId { get; set; }
        public string? Message_GUID { get; set; }
    }
}


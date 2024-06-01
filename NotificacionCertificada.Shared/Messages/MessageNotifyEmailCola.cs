namespace NotificacionCertificada.Shared.Messages
{
    public class MessageNotifyEmailCola
    {
        public Guid OperacionId { get; set; }

        public Guid TransaccionId { get; set; }

        public string? Url { get; set; }
    }
}

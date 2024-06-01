namespace NotificacionCertificada.Shared.Models
{
    public class OperacionCreateViewModel
    {
        public string? CallBack { get; set; }

        public string? CodeCertificate { get; set; }

        public MessageViewModel[]? Messages { get; set; }
    }
}

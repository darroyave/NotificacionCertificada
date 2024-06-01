namespace NotificacionCertificada.Shared.Models
{
    public class WebModel
    {
        public string? UrlDoc { get; set; }
        public string Content { get; set; } = "";
        public string ProductCode { get; set; } = "";
        public bool Visualizado { get; set; } = false;
    }
}

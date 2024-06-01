namespace NotificacionCertificada.Shared.Options
{
    public class SMSOptions
    {
        public string Url { get; set; } = "";
        public string Token { get; set; } = "";
        public string PhoneNumber { get; set; } = "";
        public string Text { get; set; } = "";
        public string From { get; set; } = "";
        public string CallBack { get; set; } = "";
    }
}

using Newtonsoft.Json;

namespace NotificacionCertificada.Shared.Models
{
    public class ColombiaRedAvanzadoDestinationRequest
    {
        [JsonProperty("to")]
        public string? To { get; set; }
    }

    public class ColombiaRedAvanzadoMessageRequest
    {
        [JsonProperty("from")]
        public string? From { get; set; }

        [JsonProperty("text")]
        public string? Text { get; set; }

        [JsonProperty("notifyUrl")]
        public string? NotifyUrl { get; set; }

        [JsonProperty("notifyContentType")]
        public string? NotifyContentType { get; set; }

        [JsonProperty("destinations")]
        public ColombiaRedAvanzadoDestinationRequest[]? Destinations { get; set; }

    }

    public class ColombiaRedAvanzadoRequest
    {
        [JsonProperty("messages")]
        public ColombiaRedAvanzadoMessageRequest[]? Messages { get; set; }

    }
}

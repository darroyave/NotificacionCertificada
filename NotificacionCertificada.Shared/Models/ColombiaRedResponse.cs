using Newtonsoft.Json;

namespace NotificacionCertificada.Shared.Models
{
    public class ColombiaRedStatus
    {
        [JsonProperty("name")]
        public string? Name { get; set; }

        [JsonProperty("groupName")]
        public string? GroupName { get; set; }

        [JsonProperty("description")]
        public string? Description { get; set; }
    }

    public class ColombiaRedMessage
    {
        [JsonProperty("to")]
        public string? To { get; set; }

        [JsonProperty("messageId")]
        public string? MessageId { get; set; }

        [JsonProperty("status")]
        public ColombiaRedStatus? Status { get; set; }

        [JsonProperty("smsCount")]
        public int SMSCount { get; set; }
    }

    public class ColombiaRedResponse
    {
        [JsonProperty("messages")]
        public ColombiaRedMessage[]? Messages { get; set; }
    }
}

/*
 
{
    "results":[{"price":{"pricePerMessage":5.000000,"currency":"COP"},
    "status":{"id":5,"groupId":3,"groupName":"DELIVERED","name":"DELIVERED_TO_HANDSET",
        "description":"Message delivered to handset"},
    "error":{"id":0,"name":"NO_ERROR","description":"No Error","groupId":0,"groupName":"OK","permanent":false},
    "messageId":"40792368928903536328",
    "doneAt":"2024-02-14T10:14:59.330-0500",
    "smsCount":2,"sentAt":"2024-02-14T10:14:49.291-0500",
    "to":"573116395612"}]}
*/
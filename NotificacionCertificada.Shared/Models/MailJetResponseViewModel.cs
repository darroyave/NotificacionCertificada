namespace NotificacionCertificada.Shared.Models
{
    public class MailJetAttachmentViewModel
    {
        public string? ContentType { get; set; }
        public string? Filename { get; set; }
        public string? Base64Content { get; set; }
    }

    public class MailJetInfoViewModel
    {
        public string? Email { get; set; }
        public string? Name { get; set; }
    }

    public class MailJetMessageViewModel
    {
        public MailJetInfoViewModel? From { get; set; }
        public MailJetInfoViewModel[]? To { get; set; }
        public string? Subject { get; set; }
        public string? TextPart { get; set; }
        public string? HTMLPart { get; set; }
        public MailJetAttachmentViewModel[]? Attachments { get; set; }
    }

    public class MailJetViewModel
    {
        public MailJetMessageViewModel[]? Messages { get; set; }
    }

    public class MailJetToResultViewModel
    {
        public string? Email { get; set; }
        public long MessageID { get; set; }
        public string? MessageHref { get; set; }
    }

    public class MailJetToResultErrorViewModel
    {
        public string? ErrorCode { get; set; }
    }

    public class MailJetMessageResultViewModel
    {
        public string? Status { get; set; }

        public MailJetToResultViewModel[]? To { get; set; }

        public MailJetToResultErrorViewModel[]? Errors { get; set; }
    }

    public class MailJetResultViewModel
    {
        public MailJetMessageResultViewModel[]? Messages { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public class MailJetResponseViewModel
    {
        public string? Status { get; set; }

        public string? MessageID { get; set; }

        public string? MessageHref { get; set; }

        public MailJetToResultErrorViewModel[]? Errors { get; set; }

        public string? ErrorMessage { get; set; }
    }
}

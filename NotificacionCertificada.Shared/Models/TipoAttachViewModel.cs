namespace NotificacionCertificada.Shared.Models
{
    public class TipoAttachViewModel
    {
        public byte[]? BytesTxt { get; set; }

        public ArchivoViewModel? Archivo { get; set; }

        public string? Tipo { get; set; }

        public string? Ext { get; set; }
    }
}

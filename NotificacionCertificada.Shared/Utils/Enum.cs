using System;

namespace NotificacionCertificada.Shared.Utils
{
    public static class ProductCertificado
    {
        public static readonly string SMSSimple = "SMSCertificadoSimple";
        public static readonly string SMSUrl = "SMSCertificadoURL";

        public static readonly string EmailSimple = "EmailCertificadoSimple";
        public static readonly string EmailUrl = "EmailCertificadoURL";
    }

    public static class Flujos
    {
        public static readonly string Init = "Init";
        public static readonly string Callback = "Callback";
        public static readonly string Recibido = "Recibido";
        public static readonly string Visualizado = "Visualizado";
        public static readonly string Error = "Error";
    }

    public static class Estados
    {
        public static readonly Guid Pendiente = new Guid("234dc34f-3f6c-4e2c-bced-7f7a3bd8ef52");
        public static readonly Guid OK = new Guid("c8c95307-4ad0-4da2-b9fa-0acbc5e89ca6");
        public static readonly Guid Rechazada = new Guid("73e89a37-3af2-403c-9cd2-6fc3e55ee4b7");
    }

    public static class Products
    {
        public static readonly Guid FirmaElectronicaSMSCertificado = new Guid("7cb43f44-7bc5-401c-a87b-1ad4c7dddf0b");
    }
}

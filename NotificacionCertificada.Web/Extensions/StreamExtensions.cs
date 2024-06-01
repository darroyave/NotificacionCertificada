namespace NotificacionCertificada.Web.Extensions
{
    public static class StreamExtensions
    {
        public static async Task<byte[]> ReadFully(this Stream input)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                await input.CopyToAsync(ms);
                return ms.ToArray();
            }
        }
    }
}

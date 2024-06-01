namespace NotificacionCertificada.Testing
{
    public class UnitTestGeneral
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void TestTimeUnix()
        {
            long unixTimestampSeconds = 1433333949;

            DateTimeOffset dateTimeOffsetSeconds = 
                DateTimeOffset.FromUnixTimeSeconds(unixTimestampSeconds);

            var result = $"DateTime from Unix timestamp (seconds): {dateTimeOffsetSeconds}";

            Assert.IsNotEmpty(result);
        }
    }
}
using Microsoft.Extensions.Logging;

namespace Test.Extensions.Logging
{
    public abstract class BasisTestKlasse
    {
        public readonly TestLoggerFactory Lf;
        protected BasisTestKlasse(LogLevel logLevel = LogLevel.Information)
        {
            Lf = new TestLoggerFactory(logLevel);
        }
    }
}
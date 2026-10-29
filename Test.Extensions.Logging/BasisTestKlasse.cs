using System.IO;
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

        /// <summary>
        /// Erzeugt einen lesbaren Stream mit dem Inhalt <paramref name="s"/>
        /// </summary>
        protected static Stream GenerateStreamFromString(string s)
        {
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write(s);
            writer.Flush();
            stream.Position = 0;
            return stream;
        }
    }
}
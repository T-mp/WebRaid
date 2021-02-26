using System.IO;
using Microsoft.Extensions.Logging;

namespace Test.Extensions.Logging
{
    /// <summary>
    /// Basis Klasse für Tests zur vereinheitlichung der <see cref="TestLoggerFactory"/> verwendung
    /// </summary>
    public abstract class BasisTestKlasse
    {
        /// <summary>
        /// LoggerFactory für Tests
        /// </summary>
        public readonly TestLoggerFactory Lf;
        /// <summary>
        /// Initialisiert die <see cref="Lf"/>
        /// </summary>
        /// <param name="logLevel">Minimaler <see cref="LogLevel"/></param>
        protected BasisTestKlasse(LogLevel logLevel = LogLevel.Information)
        {
            Lf = new TestLoggerFactory(logLevel);
        }

        /// <summary>
        /// Generates the stream from string.
        /// </summary>
        /// <param name="s">Der Inhalt des Streams</param>
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
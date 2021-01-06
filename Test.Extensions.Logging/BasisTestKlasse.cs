using Microsoft.Extensions.Logging;

namespace Test.Extensions.Logging
{
    /// <summary>
    /// Basis Klasse f�r Tests zur vereinheitlichung der <see cref="TestLoggerFactory"/> verwendung
    /// </summary>
    public abstract class BasisTestKlasse
    {
        /// <summary>
        /// LoggerFactory f�r Tests
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
    }
}
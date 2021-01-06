using Microsoft.Extensions.Logging;

namespace Test.Extensions.Logging
{
    /// <summary>
    /// Speichert eine Log-Meldung, inc. Meta-Daten
    /// </summary>
    public class LogEintrag
    {
        /// <summary>
        /// Name des Loggers
        /// </summary>
        public string Name { get; }
        /// <summary>
        /// Schwere-Grad
        /// </summary>
        public LogLevel LogLevel { get; }
        /// <summary>
        /// Meta-Informationen
        /// </summary>
        public LogInfo Info { get; }
        /// <summary>
        /// Die eigentliche Meldung
        /// </summary>
        public string Nachricht { get; internal set; }

        /// <summary>
        /// Erstellt einen Eintrag
        /// </summary>
        /// <param name="name">Name des Loggers</param>
        /// <param name="logLevel">Schwere-Grad</param>
        /// <param name="info">Meta-Informationen</param>
        /// <param name="nachricht">Die eigentliche Meldung</param>
        public LogEintrag(string name, LogLevel logLevel, LogInfo info, string nachricht)
        {
            Name = name;
            LogLevel = logLevel;
            Info = info;
            Nachricht = nachricht;
        }
    }
}

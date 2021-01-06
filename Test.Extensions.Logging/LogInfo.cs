namespace Test.Extensions.Logging
{
    /// <summary>
    /// Kapselt allgemeine Meta-Information für einen Log-Eintrag
    /// </summary>
    public class LogInfo
    {
        private readonly int nr;
        private readonly long elapsedMilliseconds;
        private readonly int threadId;
        private readonly string threadName;

        /// <summary>
        /// Speichert die Meta-Daten
        /// </summary>
        /// <param name="nr">Laufende Nummer</param>
        /// <param name="elapsedMilliseconds">Zeitmessung</param>
        /// <param name="threadId">ID des loggenden Threads</param>
        /// <param name="threadName">Name des loggenden Threads, sofern vorhanden</param>
        public LogInfo(int nr, long elapsedMilliseconds, int threadId, string threadName)
        {
            this.nr = nr;
            this.elapsedMilliseconds = elapsedMilliseconds;
            this.threadId = threadId;
            this.threadName = threadName;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return string.IsNullOrWhiteSpace(threadName)
                ? $"{nr:000}:{elapsedMilliseconds:D5}({threadId:00})"
                : $"{nr:000}:{elapsedMilliseconds:D5}({threadId:00}:'{threadName}')";
        }
    }
}

namespace Test.Extensions.Logging
{
    public class LogInfo
    {
        private readonly int nr;
        private readonly long elapsedMilliseconds;
        private readonly int threadId;
        private readonly string threadName;

        public LogInfo(int nr, long elapsedMilliseconds, int threadId, string threadName)
        {
            this.nr = nr;
            this.elapsedMilliseconds = elapsedMilliseconds;
            this.threadId = threadId;
            this.threadName = threadName;
        }

        public override string ToString()
        {
            return string.IsNullOrWhiteSpace(threadName)
                ? $"{nr:000}:{elapsedMilliseconds:D5}({threadId:00})"
                : $"{nr:000}:{elapsedMilliseconds:D5}({threadId:00}:'{threadName}')";
        }
    }
}

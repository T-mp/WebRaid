using System.Diagnostics;
using System.Threading;

namespace Test.Extensions.Logging
{
    internal static class Log
    {
        static Log()
        {
            St.Start();
        }
        private static readonly Stopwatch St = new Stopwatch();
        private static int nr;

        public static LogInfo Info()
        {
            return new LogInfo(nr++, St.ElapsedMilliseconds, Thread.CurrentThread.ManagedThreadId, Thread.CurrentThread.Name);
        }
    }
}

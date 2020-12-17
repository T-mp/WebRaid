using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace Test.Extensions.Logging
{
    internal class TestLogger : ILogger, IDisposable
    {
        private readonly List<LogEintrag> list;
        private readonly string name;
        private readonly LogLevel minLogLevel;

        public TestLogger(List<LogEintrag> list,string name, LogLevel minLogLevel = LogLevel.Warning)
        {
            this.list = list;
            this.name = name.StartsWith("WebRaid.")
                ?name.Substring("WebRaid.".Length)
                :name;
            this.minLogLevel = minLogLevel;
        }
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (logLevel < minLogLevel){ return; }

            var info = Logging.Log.Info();
#pragma warning disable IDE0071 // Interpolation vereinfachen, ohne wird die Überladung nicht verwendet!
            // ReSharper disable once RedundantToStringCall, anscheinend hier doch nicht redundant!
            var nachricht = $"[{logLevel}]{info.ToString()}{name}:{state}";
#pragma warning restore IDE0071 // Interpolation vereinfachen
            Console.WriteLine(nachricht);
            var eintrag = new LogEintrag(name, logLevel, info, nachricht);
            if (state is IEnumerable<KeyValuePair<string, object>> properties)
            {
                var pairs = properties.ToList();
                if (pairs.Any())
                {
                    eintrag.Nachricht += $" [{string.Join(", ", pairs.Select(p => $"{p.Key}:'{p.Value}'"))}]";
                }
            }

            list.Add(eintrag);
        }

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
        public bool IsEnabled(LogLevel logLevel)
        {
            return logLevel >= minLogLevel;
        }

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
        public IDisposable BeginScope<TState>(TState state)
        {
            return this;
        }

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
        public void Dispose() { }
    }
    internal class TestLogger<T> : TestLogger, ILogger<T>
    {
        public TestLogger(List<LogEintrag> list, LogLevel minLogLevel = LogLevel.Warning) : base(list, typeof(T).FullName, minLogLevel) { }
    }
}

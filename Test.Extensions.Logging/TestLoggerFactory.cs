using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace Test.Extensions.Logging
{
    public sealed class TestLoggerFactory : ILoggerFactory
    {
        //public static readonly TestLoggerFactory Lf = new TestLoggerFactory();
        private readonly LogLevel minLogLevel;

        public readonly List<LogEintrag> Loggs = new List<LogEintrag>();

        public TestLoggerFactory(LogLevel minLogLevel = LogLevel.Warning)
        {
            this.minLogLevel = minLogLevel;
        }

        #region Dispose
        private bool isDisposed;
        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
        ~TestLoggerFactory()
        {
            Dispose(false);
        }
        // ReSharper disable once UnusedParameter.Local, soll so wegen CA1063: IDisposable korrekt implementieren
#pragma warning disable IDE0060 // Nicht verwendete Parameter entfernen
        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
        private void Dispose(bool disposing)
#pragma warning restore IDE0060 // Nicht verwendete Parameter entfernen
        {
            if (isDisposed) return;
            isDisposed = true;
        }
        #endregion

        public ILogger CreateLogger(string categoryName)
        {
            return new TestLogger(Loggs, categoryName, minLogLevel);
        }
        public ILogger<T> Logger<T>()
        {
            return new TestLogger<T>(Loggs, minLogLevel);
        }    
        public ILogger<T> Logger<T>(LogLevel minLevel)
        {
            return new TestLogger<T>(Loggs, minLevel);
        }

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
        public void AddProvider(ILoggerProvider provider)
        {
            throw new NotSupportedException();
        }
    }
}

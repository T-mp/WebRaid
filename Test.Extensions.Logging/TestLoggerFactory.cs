using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace Test.Extensions.Logging
{
    /// <summary>
    /// Factory für die TestLogger.
    /// Speichert auch die getätigten Logs
    /// </summary>
    public sealed class TestLoggerFactory : ILoggerFactory
    {
        //public static readonly TestLoggerFactory Lf = new TestLoggerFactory();
        private readonly LogLevel minLogLevel;

        /// <summary>
        /// Liste der getätigten Logs
        /// </summary>
        public readonly List<LogEintrag> Loggs = new List<LogEintrag>();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="minLogLevel"></param>
        public TestLoggerFactory(LogLevel minLogLevel = LogLevel.Warning)
        {
            this.minLogLevel = minLogLevel;
        }

        #region Dispose
        private bool isDisposed;

        /// <inheritdoc />
        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
#pragma warning disable CS1591 // Fehleder XML-Kommentar für öffentlich sichtbaren Typ oder Element
        ~TestLoggerFactory()
#pragma warning restore CS1591 // Fehleder XML-Kommentar für öffentlich sichtbaren Typ oder Element
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

        /// <summary>
        /// Erstellt einen <see cref="TestLogger"/> mit dem Namen in <paramref name="categoryName"/>
        /// </summary>
        /// <param name="categoryName">Name des Loggers</param>
        /// <returns>Ein <see cref="TestLogger"/></returns>
        public ILogger CreateLogger(string categoryName)
        {
            return new TestLogger(Loggs, categoryName, minLogLevel);
        }
        /// <summary>
        /// Erstellt einen <see cref="TestLogger"/> mit dem Namen des Typs <typeparamref name="T"/>
        /// </summary>
        /// <typeparam name="T">Der Type den den Namen bestimmt</typeparam>
        /// <returns>Ein <see cref="TestLogger"/></returns>
        public ILogger<T> Logger<T>()
        {
            return new TestLogger<T>(Loggs, minLogLevel);
        }    
        /// <summary>
        /// Erstellt einen <see cref="TestLogger"/> mit dem Namen des Typs <typeparamref name="T"/>
        /// </summary>
        /// <typeparam name="T">Der Type den den Namen bestimmt</typeparam>
        /// <param name="minLevel">Minimaler schwere Grad</param>
        /// <returns>Ein <see cref="TestLogger"/></returns>
        public ILogger<T> Logger<T>(LogLevel minLevel)
        {
            return new TestLogger<T>(Loggs, minLevel);
        }

        /// <summary>
        /// Wird nicht benötigt.
        /// </summary>
        /// <exception cref="NotSupportedException"></exception>
        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
        public void AddProvider(ILoggerProvider provider)
        {
            throw new NotSupportedException();
        }
    }
}

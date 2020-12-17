using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using ILogger = NWebDav.Server.Logging.ILogger;
using ILoggerFactory = NWebDav.Server.Logging.ILoggerFactory;
using LogLevel = NWebDav.Server.Logging.LogLevel;

namespace WebRaid.UI.Adapters
{
    public class NWebDavLogging : ILoggerFactory
    {
        private readonly Microsoft.Extensions.Logging.ILoggerFactory loggerFactory;

        public NWebDavLogging(Microsoft.Extensions.Logging.ILoggerFactory loggerFactory)
        {
            this.loggerFactory = loggerFactory;
        }

        public ICollection<LogLevel> LogLevels { get; } = new HashSet<LogLevel>() { LogLevel.Fatal, LogLevel.Error, LogLevel.Warning, LogLevel.Info, LogLevel.Debug };

        private class DebugOutputLoggerAdapter : ILogger
        {
            private readonly Microsoft.Extensions.Logging.ILogger logger;
            private NWebDavLogging Parent { get;}

            public DebugOutputLoggerAdapter(NWebDavLogging parent, Microsoft.Extensions.Logging.ILogger logger)
            {
                this.logger = logger;
                Parent = parent;
            }

            public bool IsLogEnabled(LogLevel logLevel)
            {
                 return Parent.LogLevels.Contains(logLevel);
            }

            public void Log(LogLevel logLevel, Func<string> messageFunc, Exception exception)
            {
                if (IsLogEnabled(logLevel))
                {
                    logger.Log((Microsoft.Extensions.Logging.LogLevel)((int)logLevel+1), messageFunc(), exception);
                    
                    if (exception != null) 
                    {
                        logger.LogError(exception, messageFunc());
                    }
                }                    
            }
        }

        public ILogger CreateLogger(Type type)
        {
            return new DebugOutputLoggerAdapter(this, loggerFactory.CreateLogger(type.FullName));
        }
    }
}


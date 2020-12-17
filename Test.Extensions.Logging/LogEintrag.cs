using Microsoft.Extensions.Logging;

namespace Test.Extensions.Logging
{
    public class LogEintrag
    {
        public string Name { get; }
        public LogLevel LogLevel { get; }
        public LogInfo Info { get; }
        public string Nachricht { get; internal set; }
        public int? AuftragsId { get; internal set; }
        public string JsonString { get; internal set; }


        public LogEintrag(string name, LogLevel logLevel, LogInfo info, string nachricht)
        {
            Name = name;
            LogLevel = logLevel;
            Info = info;
            Nachricht = nachricht;
        }
    }
}

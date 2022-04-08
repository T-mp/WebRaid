using System;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Threading.Tasks;
using System.Timers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace WinServiceTestHelper
{
    internal class Program
    {
        public static async Task Main(string[] args)
        {
            // create service collection
            var services = new ServiceCollection();

            // build config
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddCommandLine(args)
                .AddEnvironmentVariables()
                .AddJsonFile("appsettings.json", optional: false)
                .Build();

            services.AddSingleton(configuration);
            services.AddSingleton<IConfiguration>(configuration);

            services.AddLogging(builder =>
            {
                builder.AddConfiguration(configuration.GetSection("Logging"));
                builder.AddConsole();
                builder.AddDebug();
            });

            services.Configure<AppSettings>(configuration);

            // add app
            services.AddTransient<App>();

            // create service provider
            var serviceProvider = services.BuildServiceProvider();

            // entry to run app
            await serviceProvider.GetRequiredService<App>().Run(args);
        }
    }

    public class AppSettings
    {
        public string Quelle { get; set; }
        public string Ziel { get; set; }
        public string ServiceName { get; set; }
        public Modus Modus { get; set; }
    }

    public enum Modus
    {
        NurKopieren,
        StarteImmer,
        StarteNurWennLief
    }

    internal class App
    {
        private readonly ILogger<App> logger;
        private readonly AppSettings settings;
        private readonly Timer timer = new Timer(1000 * 10);

        public App(IOptions<AppSettings> appSettings, ILogger<App> logger)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            settings = appSettings?.Value ?? throw new ArgumentNullException(nameof(appSettings));
            timer.AutoReset = false;
            timer.Stop();
            timer.Elapsed += (_, _) => DoWork();
        }

        public async Task Run(string[] args)
        {
            await Task.Run(() =>
            {
                logger.LogInformation("Starting...");

                logger.LogTrace("settings:");
                logger.LogTrace($"  Quelle: '{settings.Quelle}'");
                logger.LogTrace($"  Ziel: '{settings.Ziel}'");
                logger.LogTrace($"  ServiceName: '{settings.ServiceName}'");
                logger.LogTrace($"  Modus: '{settings.Modus}'");

                using var watcher = new FileSystemWatcher(settings.Quelle,"*.exe");
                watcher.Changed += Trigger;
                watcher.Created += Trigger;
                watcher.Error += OnError;
                watcher.IncludeSubdirectories = true;
                watcher.EnableRaisingEvents = true;

                DoWork();

                Console.WriteLine("Warte auf Änderungen, Press enter to exit.");
                Console.ReadLine();
                logger.LogInformation("Finished!");
            }).ConfigureAwait(false);
        }

        private void Trigger(object sender, FileSystemEventArgs e)
        {
            logger.LogTrace("Changed: {Path}", e.FullPath);
            timer.Stop();
            timer.Enabled = true;
            timer.Start();
        }

        private void OnError(object sender, ErrorEventArgs e)
        {
            var exception = e.GetException();
            logger.LogWarning(exception, exception.Message);
        }

        private void DoWork()
        {
            logger.LogTrace("DoWork.Start ...");
            var service = ServiceController.GetServices()
                .FirstOrDefault(sc => sc.ServiceName == settings.ServiceName);

            var status = service?.Status ?? ServiceControllerStatus.Stopped;

            if (service != null)
            {
                logger.LogTrace("Service '{service}' gefunden, Status '{status}'", settings.ServiceName, status);
                if (status != ServiceControllerStatus.Stopped)
                {
                    var zweiterVersuch = false;
                    if (ServiceControllerStatus.StopPending == service.Status)
                    {
                        logger.LogWarning("Service wird schon gestoppt gestoppt");
                        zweiterVersuch = true;
                        Task.Delay(TimeSpan.FromSeconds(10)).Wait();
                    }
                    else
                    {
                        try
                        {
                            logger.LogTrace("Stoppe Service '{ServiceName}'", settings.ServiceName);
                            service.Stop();
                        }
                        catch (Exception e)
                        {
                            logger.LogWarning(e, "Fehler beim Stoppen des Service '{ServiceName}': {Err}", settings.ServiceName, e.Message);
                            return;
                        }
                    }
                    service.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(30));

                    if (service.Status != ServiceControllerStatus.Stopped)
                    {
                        logger.LogWarning("Service ist nicht gestoppt");
                        if (!zweiterVersuch)
                        {
                            Task.Delay(TimeSpan.FromSeconds(10)).Wait();
                            logger.LogInformation("=> Retry");
                            Trigger(null, null);
                        }
                        return;
                    }
                    logger.LogTrace("Service '{ServiceName}' gestoppt, ({status})", settings.ServiceName, service.Status);
                }

                MirrorDirectory(settings.Quelle, settings.Ziel);

                if (settings.Modus == Modus.StarteImmer
                    || (settings.Modus == Modus.StarteNurWennLief && status != ServiceControllerStatus.Stopped))
                {
                    StarteService(service);
                }
            }
            else
            {
                logger.LogWarning("Service '{service}' nicht gefunden!", settings.ServiceName);
            }
            logger.LogTrace("... DoWork.Ende");
        }

        private void StarteService(ServiceController service, int versuche = 3)
        {
            StarteService(service, versuche, TimeSpan.FromSeconds(30));
        }
        private void StarteService(ServiceController service, int versuche, TimeSpan timeout)
        {
            while (service.Status != ServiceControllerStatus.Running)
            {
                try
                {
                    logger.LogDebug("Starte Service '{ServiceName}'", settings.ServiceName);
                    service.Start();
                }
                catch (Exception e)
                {
                    logger.LogWarning(e, "Fehler beim Starten des Service '{ServiceName}': {Err}", settings.ServiceName, e.Message);
                    return;
                }
                versuche--;
                service.WaitForStatus(ServiceControllerStatus.Running, timeout);
                if (service.Status != ServiceControllerStatus.Running)
                {
                    logger.LogInformation("Service ist noch nicht gestartet! => retry {restVersuche}", versuche);
                }
            }
            if (service.Status != ServiceControllerStatus.Running)
            {
                logger.LogWarning("Service ist nicht gestartet!");
            }
            else
            {
                logger.LogDebug("Service '{ServiceName}' gestartet, ({status})", settings.ServiceName, service.Status);
            }
        }

        private void MirrorDirectory(string quelle, string ziel)
        {
            logger.BeginScope("{quelle}=>{ziel}", quelle, ziel);
            foreach (var zielPfad in Directory.GetDirectories(ziel))
            {
                var di = new DirectoryInfo(zielPfad);
                if (!Directory.Exists(Path.Combine(quelle, di.Name)))
                {
                    logger.LogInformation("Lösche Verzeichnis: '{Verzeichnis}'", di.FullName);
                    di.Delete(true);
                }
            }
            foreach (var quellPfad in Directory.GetDirectories(quelle))
            {
                var di = new DirectoryInfo(quellPfad);
                var neuerPfad = Path.Combine(ziel, di.Name);
                if (!Directory.Exists(neuerPfad))
                {
                    logger.LogInformation("Erzeuge Verzeichnis: '{Verzeichnis}'", neuerPfad);
                    Directory.CreateDirectory(neuerPfad);
                }
                MirrorDirectory(quellPfad, neuerPfad);
            }

            foreach (var zielDateiPfad in Directory.GetFiles(ziel))
            {
                var zielDatei = new FileInfo(zielDateiPfad);
                if (!File.Exists(Path.Combine(quelle, zielDatei.Name)))
                {
                    logger.LogInformation("Lösche Datei: '{Datei}'", zielDatei.FullName);
                    zielDatei.Delete();
                }
            }

            foreach (var quelldateiPfad in Directory.GetFiles(quelle))
            {
                var quellDatei = new FileInfo(quelldateiPfad);
                var zielName = Path.Combine(ziel, quellDatei.Name);
                if (!File.Exists(zielName))
                {
                    logger.LogInformation("Erzeuge Datei: '{Datei}'", zielName);
                    quellDatei.CopyTo(zielName);
                    var zielDatei = new FileInfo(zielName);
                    zielDatei.CreationTimeUtc = quellDatei.CreationTimeUtc;
                    zielDatei.LastWriteTimeUtc = quellDatei.LastWriteTimeUtc;
                }
                else
                {
                    var zielDatei = new FileInfo(zielName);
                    if (quellDatei.Length != zielDatei.Length
                        || quellDatei.CreationTimeUtc != zielDatei.CreationTimeUtc
                        || quellDatei.LastWriteTimeUtc != zielDatei.LastWriteTimeUtc
                        )
                    {
                        logger.LogInformation("Aktualisiere Datei: '{Datei}'", zielName);
                        quellDatei.CopyTo(zielName, true);
                        zielDatei = new FileInfo(zielName);
                        zielDatei.CreationTimeUtc = quellDatei.CreationTimeUtc;
                        zielDatei.LastWriteTimeUtc = quellDatei.LastWriteTimeUtc;
                    }
                }
            }
        }
    }
}

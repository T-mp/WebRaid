using System.Text.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using WebRaid.Abstraction.Speicher;
using WebRaid.Abstraction.VDS;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using WebRaid.VDS.JsonConverter;

namespace WebRaid.VDS
{
    /// <inheritdoc cref="IDirectoryInfo" />
    [JsonConverter(typeof(JsonConverterDirectoryInfo))]
    public class DirectoryInfo : FileSystemInfo, IDirectoryInfo
    {
        private readonly INode persistenz;
        private readonly IFileAdressenGenerator adressenGenerator;
        private readonly ILogger<DirectoryInfo> logger;
        [JsonInclude]
        public readonly Dictionary<string, IFileSystemInfo> Inhalt = new Dictionary<string, IFileSystemInfo>();

        private DirectoryInfo(INode persistenz, IFileAdressenGenerator adressenGenerator, ILogger<DirectoryInfo> logger)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.persistenz = persistenz ?? throw new ArgumentNullException(nameof(persistenz));
            this.adressenGenerator = adressenGenerator ?? throw new ArgumentNullException(nameof(adressenGenerator));
        }

        private DirectoryInfo(INode persistenz, IFileAdressenGenerator adressenGenerator, string fullName, ILogger<DirectoryInfo> logger)
            : this(persistenz, adressenGenerator, logger)
        {
            if (string.IsNullOrWhiteSpace(fullName)) throw new ArgumentNullException(nameof(fullName));
            if (!fullName.StartsWith(Pfad.DirectorySeparatorString)) throw new ArgumentOutOfRangeException(nameof(fullName), "Der Pfad muss bis zum Root gehen!");

            FullName = fullName;

            Adresse = this.adressenGenerator.GetNew();
            logger.LogTrace($"New DirectoryInfo({fullName})[{Adresse}]");

            Save();
        }

        private void Save()
        {
            logger.LogTrace($"Save()[{Adresse}:{FullName}]");
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream, Encoding.UTF8);

            var options = new JsonSerializerOptions();
            options.Converters.Add(JsonConverterFileSystemInfo.Instance);
            writer.Write(JsonSerializer.Serialize(this, options));
            writer.Flush();

            stream.Position = 0;
            persistenz.Write(Adresse, stream);
        }


        public static DirectoryInfo GetRoot(INode persistenz, string name, IFileAdressenGenerator adressenGenerator, ILogger<DirectoryInfo> logger)
        {
            logger.LogTrace($"GetRoot({name})");
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentNullException(nameof(name));
            if (!name.StartsWith(Pfad.DirectorySeparatorString)) throw new ArgumentOutOfRangeException(nameof(name), "Der Pfad muss bis zum Root gehen!");

            return new DirectoryInfo(persistenz, adressenGenerator, $"{name}", logger);
        }

        /// <summary>
        /// Läd ein <see cref="DirectoryInfo"/> von der <paramref name="adresse"/>
        /// </summary>
        /// <param name="persistenz">Zugrundeliegende Persistenzschicht</param>
        /// <param name="adresse">Adresse in der <paramref name="persistenz"/></param>
        /// <param name="adressenGenerator">Generator für eindeutige Adressen</param>
        public DirectoryInfo(INode persistenz, string adresse, IFileAdressenGenerator adressenGenerator, ILogger<DirectoryInfo> logger)
            : this(persistenz, adressenGenerator, logger)
        {
            if (string.IsNullOrWhiteSpace(adresse)) throw new ArgumentNullException(nameof(adresse));
            Adresse = adresse;

            var stream = persistenz.Get(adresse);

            var streamReader = new StreamReader(stream.Result);

            var json = streamReader.ReadToEnd();
            var options = new JsonSerializerOptions();
            options.Converters.Add(JsonConverterFileSystemInfoReadDto.Instance);
            var readDto = JsonSerializer.Deserialize<DirectoryInfoReadDto>(json, options);
            FullName = readDto.FullName;
            Inhalt = readDto.Inhalt.ToDictionary(
                pair => pair.Key, 
                pair =>
                        {
                            switch (pair.Value)
                            {
                                case DirectoryInfoReadDto dir:
                                    return (IFileSystemInfo)new DirectoryInfo(persistenz, dir.Adresse, adressenGenerator, logger);
                                case FileInfoReadDto file:
                                    return new FileInfo();
                                default:
                                    throw new NotSupportedException();
                            }
                        }
                );
            Properties = readDto.Properties;
            logger.LogTrace($"New DirectoryInfo({adresse})[{FullName}]");
        }

        /// <inheritdoc />
        [JsonIgnore]
        public override string Extension => string.Empty;
        /// <inheritdoc />
        [JsonIgnore]
        public override bool Exists => true;

        /// <inheritdoc />
        public void Delete(bool recursive)
        {
            logger.LogTrace($"Delete({recursive})[{Adresse}:{FullName}]");
            //TODO: Übergeordnetem Ordner bescheid sagen! ;-)

            if (!recursive && Inhalt.Any()) throw new InvalidOperationException("Der Ordner ist nicht leer!");

            if (!Inhalt.Any()) return;

            _ = Inhalt.Select(pair => pair.Value).Cast<IFileInfo>().Select(f =>
            {
                f.Delete();
                persistenz.Del(Adresse);
                return true;
            });

            _ = Inhalt.Select(pair => pair.Value).Cast<IDirectoryInfo>().Select(f =>
            {
                f.Delete(true);
                persistenz.Del(Adresse);
                return true;
            });
        }
        /// <inheritdoc />
        public IDirectoryInfo CreateSubdirectory(string path)
        {
            logger.LogTrace($"CreateSubdirectory({path})");
            if (path.Contains(Pfad.DirectorySeparatorChar))
            {
                var namenTeile = path.Split(Pfad.DirectorySeparatorChar);
                var name = namenTeile.First();

                if (Inhalt.ContainsKey(name)) throw new DuplicateNameException($"In '{FullName}' existiert '{path}' bereits!");
                var sub = new DirectoryInfo(persistenz, adressenGenerator, $"{FullName}{Pfad.DirectorySeparatorChar}{name}", logger);
                Inhalt[name] = sub;
                Save();

                return sub.CreateSubdirectory(string.Join(Pfad.DirectorySeparatorString, namenTeile.Skip(1)));
            }

            if (Inhalt.ContainsKey(path)) throw new DuplicateNameException($"In '{FullName}' existiert '{path}' bereits!");
            var newDir = new DirectoryInfo(persistenz, adressenGenerator, $"{FullName}{Pfad.DirectorySeparatorChar}{path}", logger);
            Inhalt[path] = newDir;
            Save();
            return newDir;
        }

        /// <inheritdoc />
        public IEnumerable<IFileInfo> GetFiles(string searchPattern = "*", SearchOption searchOption = SearchOption.TopDirectoryOnly)
        {
            logger.LogTrace($"GetFiles({searchPattern}, {searchOption})");
            var files = Inhalt.Select(pair => pair.Value)
                .Cast<IFileInfo>()
                .Where(d =>
                    d.FullName.EqualsWildcard(searchPattern)
                    || d.Name.EqualsWildcard(searchPattern)
                ).ToList();

            if (searchOption == SearchOption.TopDirectoryOnly)
            {
                return files;
            }
            var subs = Inhalt.Select(pair => pair.Value)
                .Cast<IDirectoryInfo>()
                .SelectMany(d => d.GetFiles(searchPattern, searchOption));
            files = files.Concat(subs).ToList();

            return files;
        }

        /// <inheritdoc />
        public IEnumerable<IFileSystemInfo> GetFileSystemInfos(string searchPattern = "*", SearchOption searchOption = SearchOption.TopDirectoryOnly)
        {
            logger.LogTrace($"GetFileSystemInfos({searchPattern}, {searchOption})");
            var infos = Inhalt.Select(pair => pair.Value)
                .Where(d =>
                    d.FullName.EqualsWildcard(searchPattern)
                    || d.Name.EqualsWildcard(searchPattern)
                ).ToList();

            if (searchOption == SearchOption.TopDirectoryOnly)
            {
                return infos;
            }
            var subs = Inhalt.Select(pair => pair.Value)
                .Cast<IDirectoryInfo>()
                .SelectMany(d => d.GetFileSystemInfos(searchPattern, searchOption));
            infos = infos.Concat(subs).ToList();

            return infos;
        }

        /// <inheritdoc />
        public IEnumerable<IDirectoryInfo> GetDirectories(string searchPattern = "*", SearchOption searchOption = SearchOption.TopDirectoryOnly)
        {
            logger.LogTrace($"GetDirectories({searchPattern}, {searchOption})");
            var dirs = Inhalt.Select(pair => pair.Value).Cast<IDirectoryInfo>()
                .Where(d =>
                    d.FullName.EqualsWildcard(searchPattern)
                    || d.Name.EqualsWildcard(searchPattern)
                );

            if (searchOption == SearchOption.TopDirectoryOnly)
            {
                return dirs;
            }

            var subs = Inhalt.Select(pair => pair.Value)
                .Cast<IDirectoryInfo>()
                .SelectMany(d => d.GetDirectories(searchPattern, searchOption));
            dirs = dirs.Concat(subs).ToList();

            return dirs;
        }

        /// <inheritdoc />
        public override void Delete()
        {
            Delete(false);
        }
    }
}
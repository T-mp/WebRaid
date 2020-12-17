using System.Collections.Generic;
using System.ComponentModel;
using System.Text.Json.Serialization;
using WebRaid.Abstraction.VDS;
using WebRaid.VDS.JsonConverter;

namespace WebRaid.VDS
{
    /// <inheritdoc cref="IFileSystemInfo" />
    [JsonConverter(typeof(JsonConverterFileSystemInfo))]
    public abstract class FileSystemInfo : IFileSystemInfo
    {
        /// <inheritdoc />
        public string Adresse { get; internal set; }
        /// <inheritdoc />
        public string FullName { get; protected set; }
        /// <inheritdoc />
        [JsonIgnore]
        public string Name => Pfad.GetFileName(FullName);
        /// <inheritdoc />
        [JsonIgnore]
        public virtual string Extension => Pfad.GetExtension(FullName);

        /// <inheritdoc />
        public virtual bool Exists { get; protected set; }
        /// <inheritdoc />
        public abstract void Delete();
        /// <inheritdoc />
        public IList<IFileSystemInfoProperty> Properties { get; protected set; } = new List<IFileSystemInfoProperty>();
    }
}

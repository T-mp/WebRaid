using System;
using System.IO;
using System.Text.Json.Serialization;
using WebRaid.Abstraction.VDS;
using WebRaid.VDS.JsonConverter;

namespace WebRaid.VDS
{
    /// <inheritdoc cref="IFileInfo" />
    [JsonConverter(typeof(JsonConverterFileSystemInfo))]
    public class FileInfo : FileSystemInfo, IFileInfo
    {
        /// <inheritdoc />
        public ulong Length { get; protected set; }

        /// <inheritdoc />
        public bool IsReadOnly { get; set; }

        /// <inheritdoc />
        public Stream Open()
        {
            throw new NotImplementedException();
        }
        /// <inheritdoc />
        public override void Delete()
        {
            throw new NotImplementedException();
        }
    }
}
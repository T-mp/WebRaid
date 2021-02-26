using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace WebRaid.VDS.JsonConverter
{
    internal class JsonConverterDirectoryInfo : JsonConverter<DirectoryInfo>
    {
        public override DirectoryInfo Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var converter = (JsonConverter<FileSystemInfo>)options.GetConverter(typeof(FileSystemInfo));
            return converter.Read(ref reader, typeToConvert, options) as DirectoryInfo;
        }

        public override void Write(Utf8JsonWriter writer, DirectoryInfo value, JsonSerializerOptions options)
        {
            var converter = (JsonConverter<FileSystemInfo>)options.GetConverter(typeof(FileSystemInfo));
            converter.Write(writer, value, options);
        }
    }
}
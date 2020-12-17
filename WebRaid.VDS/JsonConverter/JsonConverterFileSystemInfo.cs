using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using WebRaid.Abstraction.VDS;

namespace WebRaid.VDS.JsonConverter
{
    public class JsonConverterFileSystemInfo : JsonConverter<FileSystemInfo>
    {
        private static System.Text.Json.Serialization.JsonConverter instance;
        internal static ILogger<JsonConverterFileSystemInfo> Logger = null;

        public override FileSystemInfo Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            throw new NotSupportedException();
        }

        public override void Write(Utf8JsonWriter writer, FileSystemInfo value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            //Selector für die Deserialization
            writer.WriteString("type", value.GetType().Name);
            writer.WriteString(nameof(value.FullName), value.FullName);
            writer.WriteString(nameof(value.Adresse), value.Adresse);

            writer.WritePropertyName(nameof(value.Properties));
            writer.WriteStartArray();
            var propertyConverter = options.GetConverter(typeof(IFileSystemInfoProperty));
            foreach (var fileSystemInfo in value.Properties)
            {
                ((JsonConverter<IFileSystemInfoProperty>)propertyConverter).Write(writer, fileSystemInfo, options);
            }
            writer.WriteEndArray();

            switch (value)
            {
                case DirectoryInfo dir:
                    writer.WritePropertyName(nameof(dir.Inhalt));
                    writer.WriteStartObject();
                    var inhaltConverter = options.GetConverter(typeof(object));
                    foreach (var eintrag in dir.Inhalt)
                    {
                        writer.WritePropertyName(eintrag.Key);
                        writer.WriteStartObject();
                        var systemInfo = (FileSystemInfo)eintrag.Value;
                        writer.WriteString("type", systemInfo.GetType().Name);
                        writer.WriteString(nameof(systemInfo.FullName), systemInfo.FullName);
                        writer.WriteString(nameof(systemInfo.Adresse), systemInfo.Adresse);

                        writer.WritePropertyName(nameof(value.Properties));
                        writer.WriteStartArray();
                        foreach (var fileSystemInfo in value.Properties)
                        {
                            ((JsonConverter<IFileSystemInfoProperty>)propertyConverter).Write(writer, fileSystemInfo, options);
                        }
                        writer.WriteEndArray();
                        writer.WriteEndObject();
                    }
                    writer.WriteEndObject();
                    break;
                case FileInfo file:
                    writer.WriteNumber(nameof(file.Length), file.Length);
                    break;
            }

            writer.WriteEndObject();
        }

        public static System.Text.Json.Serialization.JsonConverter Instance => instance ?? (instance = new JsonConverterFileSystemInfo());
    }
}
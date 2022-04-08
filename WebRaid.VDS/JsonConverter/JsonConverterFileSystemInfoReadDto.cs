using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using WebRaid.Abstraction.VDS;

namespace WebRaid.VDS.JsonConverter
{
    internal class JsonConverterFileSystemInfoReadDto : JsonConverter<FileSystemInfoReadDto>
    {
        private static System.Text.Json.Serialization.JsonConverter instance;
        internal static ILogger<JsonConverterFileSystemInfoReadDto> Logger = null;

        public override FileSystemInfoReadDto Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            Logger?.LogTrace($"FileSystemInfoReadDto()");
            if (reader.TokenType != JsonTokenType.StartObject)
            {
                throw new JsonException();
            }
            FileSystemInfoReadDto result;
            reader.Read();
            var type = GetString(ref reader, "type");
            switch (type)
            {
                case "DirectoryInfo":
                    Logger?.LogTrace($"DirectoryInfo()");
                    result = new DirectoryInfoReadDto();
                    break;
                case "FileInfo":
                    Logger?.LogTrace($"FileInfo()");
                    result = new FileInfoReadDto();
                    break;
                default:
                    throw new JsonException();
            }
            result.FullName = GetString(ref reader, nameof(result.FullName));
            result.Adresse = GetString(ref reader, nameof(result.Adresse));

            var propertyConverter = (JsonConverter<IFileSystemInfoProperty>)options.GetConverter(typeof(IFileSystemInfoProperty));
            //var list = propertyConverter.Read(ref reader, typeToConvert, options);
            result.Properties = new List<IFileSystemInfoProperty>();
            AssertPropertyName(ref reader, nameof(result.Properties));
            if (reader.TokenType != JsonTokenType.StartArray)
            {
                throw new JsonException();
            }
            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndArray)
                {
                    break;
                }

                var property = propertyConverter.Read(ref reader, typeof(IFileSystemInfoProperty), options);
                result.Properties.Add(property);
            }
            reader.Read(); //EndArray


            switch (type)
            {
                case "DirectoryInfo":
                    if (reader.TokenType == JsonTokenType.EndObject)
                    {
                        break;
                    }
                    AssertPropertyName(ref reader, nameof(DirectoryInfoReadDto.Inhalt));
                    if (reader.TokenType != JsonTokenType.StartObject)
                    {
                        throw new JsonException();
                    }

                    while (reader.Read())
                    {
                        if (reader.TokenType == JsonTokenType.EndObject)
                        {
                            break;
                        }
                        if (reader.TokenType != JsonTokenType.String)
                        {
                            throw new JsonException();
                        }
                        var subDir = new DirectoryInfoReadDto();
                        var name = reader.GetString();
                        if (reader.TokenType != JsonTokenType.StartObject)
                        {
                            throw new JsonException();
                        }

                        //writer.WriteString("type", systemInfo.GetType().Name);
                        //writer.WriteString(nameof(systemInfo.FullName), systemInfo.FullName);
                        //writer.WriteString(nameof(systemInfo.Adresse), systemInfo.Adresse);

                        //writer.WritePropertyName(nameof(value.Properties));
                        //writer.WriteStartArray();
                        //foreach (var fileSystemInfo in value.Properties)
                        //{
                        //    ((JsonConverter<IFileSystemInfoProperty>)propertyConverter).Write(writer, fileSystemInfo, options);
                        //}
                        //writer.WriteEndArray();
                        //writer.WriteEndObject();

                        ((DirectoryInfoReadDto) result).Inhalt.Add(name, subDir);
                    }

                    break;
                case "FileInfo":
                    ((FileInfoReadDto) result).Length = GetULong(ref reader, nameof(FileInfoReadDto.Length));
                    break;
                default:
                    throw new JsonException();
            }

            return result;
        }

        private string GetString(ref Utf8JsonReader reader, string name)
        {
            AssertPropertyName(ref reader, name);

            if (reader.TokenType != JsonTokenType.String)
            {
                throw new JsonException();
            }

            var wert = reader.GetString();
            Logger?.LogTrace($"GetString({name})=>'{wert}'");
            reader.Read();
            return wert;
        }        
        private ulong GetULong(ref Utf8JsonReader reader, string name)
        {
            AssertPropertyName(ref reader, name);

            if (reader.TokenType != JsonTokenType.String)
            {
                throw new JsonException();
            }

            var wert = reader.GetUInt64();
            Logger?.LogTrace($"GetULong({name})=>'{wert}'");
            reader.Read();
            return wert;
        }        
        private void AssertPropertyName(ref Utf8JsonReader reader, string name)
        {
            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                Logger?.LogTrace($"AssertPropertyName({name}):TokenType '{reader.TokenType}' != PropertyName");
                throw new JsonException();
            }

            if (reader.GetString() != name)
            {
                Logger?.LogTrace($"AssertPropertyName({name}) != '{reader.GetString()}'");
                throw new JsonException();
            }
            reader.Read();
        }

        public override void Write(Utf8JsonWriter writer, FileSystemInfoReadDto value, JsonSerializerOptions options)
        {
            throw new NotSupportedException();
        }

        public static System.Text.Json.Serialization.JsonConverter Instance => instance ?? (instance = new JsonConverterFileSystemInfoReadDto());
    }
}
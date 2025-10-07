using Shared.FileSystem;
using Shared.JSON;
using System.Text.Json;
using System.Text.Json.Serialization;
using FileInfo = Shared.FileSystem.FileInfo;

namespace ModelTextureMaker.Settings
{
    class MdlTextureMakingHistoryJsonSerializer : JsonConverter<MdlTextureMakingHistory>
    {
        public override void Write(Utf8JsonWriter writer, MdlTextureMakingHistory value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();

            writer.WritePropertyName("textures");
            writer.WriteStartArray();
            foreach (var texture in value.Textures.Values)
                WriteTextureHistory(writer, texture);
            writer.WriteEndArray();

            writer.WritePropertyName("sub-directory-names");
            writer.WriteStartArray();
            foreach (var subDirectory in value.SubDirectoryNames)
                writer.WriteStringValue(subDirectory);
            writer.WriteEndArray();

            writer.WriteEndObject();
        }

        public override MdlTextureMakingHistory? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var textures = new Dictionary<string, MdlTextureMakingHistory.MdlTextureHistory>();
            var subDirectoryNames = new List<string>();

            reader.ReadStartObject();
            while (reader.TokenType != JsonTokenType.EndObject)
            {
                switch (reader.ReadPropertyName())
                {
                    default: reader.SkipValue(); break;

                    case "textures":
                    {
                        reader.ReadStartArray();

                        while (reader.TokenType != JsonTokenType.EndArray)
                        {
                            var textureHistory = ReadTextureHistory(ref reader);
                            var textureName = Path.GetFileNameWithoutExtension(textureHistory.OutputFile.Path);
                            textures[textureName] = textureHistory;
                        }

                        reader.ReadEndArray();
                        break;
                    }

                    case "sub-directory-names":
                    {
                        reader.ReadStartArray();

                        while (reader.TokenType != JsonTokenType.EndArray)
                        {
                            var subDirectoryName = reader.ReadString();
                            if (subDirectoryName is not null)
                                subDirectoryNames.Add(subDirectoryName);
                        }

                        reader.ReadEndArray();
                        break;
                    }
                }
            }
            reader.ReadEndObject();

            return new MdlTextureMakingHistory(textures, subDirectoryNames);
        }


        private static void WriteTextureHistory(Utf8JsonWriter writer, MdlTextureMakingHistory.MdlTextureHistory textureHistory)
        {
            writer.WriteStartObject();
            writer.WritePropertyName("output-file");
            WriteFileInfo(writer, textureHistory.OutputFile);

            writer.WritePropertyName("input-files");
            writer.WriteStartArray();
            foreach (var inputFile in textureHistory.InputFiles)
                WriteTextureSourceFileInfo(writer, inputFile);
            writer.WriteEndArray();
            writer.WriteEndObject();
        }

        private static MdlTextureMakingHistory.MdlTextureHistory ReadTextureHistory(ref Utf8JsonReader reader)
        {
            FileInfo? outputFile = null;
            var inputFiles = new List<MdlTextureSourceFileInfo>();

            reader.ReadStartObject();
            while (reader.TokenType != JsonTokenType.EndObject)
            {
                switch (reader.ReadPropertyName())
                {
                    default: reader.SkipValue(); break;

                    case "output-file":
                    {
                        outputFile = ReadFileInfo(ref reader);
                        break;
                    }

                    case "input-files":
                    {
                        reader.ReadStartArray();
                        while (reader.TokenType != JsonTokenType.EndArray)
                            inputFiles.Add(ReadTextureSourceFileInfo(ref reader));
                        reader.ReadEndArray();
                        break;
                    }
                }
            }
            reader.ReadEndObject();

            return new MdlTextureMakingHistory.MdlTextureHistory(outputFile, inputFiles.ToArray());
        }


        private static void WriteFileInfo(Utf8JsonWriter writer, FileInfo fileInfo)
        {
            writer.WriteStartObject();

            writer.WriteString("path", fileInfo.Path);
            writer.WriteNumber("file-size", fileInfo.FileSize);
            writer.WriteString("file-hash", fileInfo.FileHash.ToString());
            writer.WriteNumber("last-modified", fileInfo.LastModified.ToUnixTimeMilliseconds());

            writer.WriteEndObject();
        }

        private static FileInfo ReadFileInfo(ref Utf8JsonReader reader)
        {
            string? path = null;
            var fileSize = 0;
            var fileHash = new FileHash();
            var lastModified = DateTimeOffset.MinValue;

            reader.ReadStartObject();
            while (reader.TokenType != JsonTokenType.EndObject)
            {
                switch (reader.ReadPropertyName())
                {
                    default: reader.SkipValue(); break;
                    case "path": path = reader.ReadString(); break;
                    case "file-size": fileSize = (int)reader.ReadInt64(); break;
                    case "file-hash": fileHash = FileHash.Parse(reader.ReadString() ?? ""); break;
                    case "last-modified": lastModified = DateTimeOffset.FromUnixTimeMilliseconds(reader.ReadInt64()); break;
                }
            }
            reader.ReadEndObject();

            return new FileInfo(path ?? "", fileSize, fileHash, lastModified);
        }


        private static void WriteTextureSourceFileInfo(Utf8JsonWriter writer, MdlTextureSourceFileInfo fileInfo)
        {
            writer.WriteStartObject();

            writer.WriteString("path", fileInfo.Path);
            writer.WriteNumber("file-size", fileInfo.FileSize);
            writer.WriteString("file-hash", fileInfo.FileHash.ToString());
            writer.WriteNumber("last-modified", fileInfo.LastModified.ToUnixTimeMilliseconds());
            writer.WritePropertyName("settings");
            WriteTextureSettings(writer, fileInfo.Settings);

            writer.WriteEndObject();
        }

        private static MdlTextureSourceFileInfo ReadTextureSourceFileInfo(ref Utf8JsonReader reader)
        {
            string? path = null;
            var fileSize = 0;
            var fileHash = new FileHash();
            var lastModified = DateTimeOffset.MinValue;
            MdlTextureSettings? settings = null;

            reader.ReadStartObject();
            while (reader.TokenType != JsonTokenType.EndObject)
            {
                switch (reader.ReadPropertyName())
                {
                    default: reader.SkipValue(); break;
                    case "path": path = reader.ReadString(); break;
                    case "file-size": fileSize = (int)reader.ReadInt64(); break;
                    case "file-hash": fileHash = FileHash.Parse(reader.ReadString() ?? ""); break;
                    case "last-modified": lastModified = DateTimeOffset.FromUnixTimeMilliseconds(reader.ReadInt64()); break;
                    case "settings": settings = ReadTextureSettings(ref reader); break;
                }
            }
            reader.ReadEndObject();

            return new MdlTextureSourceFileInfo(path ?? "", fileSize, fileHash, lastModified, settings ?? new MdlTextureSettings());
        }


        private static void WriteTextureSettings(Utf8JsonWriter writer, MdlTextureSettings settings)
        {
            writer.WriteStartObject();

            if (settings.Ignore != null) writer.WriteBoolean("ignore", settings.Ignore.Value);
            if (settings.ColorMask != null) writer.WriteString("color-mask", Serialization.ToString(settings.ColorMask.Value));
            if (settings.ColorCount != null) writer.WriteNumber("color-count", settings.ColorCount.Value);
            if (settings.IsModelPortrait != null) writer.WriteBoolean("is-model-portrait", settings.IsModelPortrait.Value);
            if (settings.PreservePalette != null) writer.WriteBoolean("preserve-palette", settings.PreservePalette.Value);
            if (settings.DitheringAlgorithm != null) writer.WriteString("dithering-algorithm", Serialization.ToString(settings.DitheringAlgorithm.Value));
            if (settings.DitherScale != null) writer.WriteNumber("dither-scale", settings.DitherScale.Value);
            if (settings.TransparencyThreshold != null) writer.WriteNumber("transparency-threshold", settings.TransparencyThreshold.Value);
            if (settings.TransparencyColor != null) writer.WriteString("transparency-color", Serialization.ToString(settings.TransparencyColor.Value));
            if (settings.Converter != null) writer.WriteString("converter", settings.Converter);
            if (settings.ConverterArguments != null) writer.WriteString("converter-arguments", settings.ConverterArguments);

            writer.WriteEndObject();
        }

        private static MdlTextureSettings ReadTextureSettings(ref Utf8JsonReader reader)
        {
            var settings = new MdlTextureSettings();

            reader.ReadStartObject();
            while (reader.TokenType != JsonTokenType.EndObject)
            {
                switch (reader.ReadPropertyName())
                {
                    default: reader.SkipValue(); break;
                    case "ignore": settings.Ignore = reader.ReadBoolean(); break;
                    case "color-mask": settings.ColorMask = Serialization.ReadColorMask(reader.ReadString()); break;
                    case "color-count": settings.ColorCount = (int)reader.ReadInt64(); break;
                    case "is-model-portrait": settings.IsModelPortrait = reader.ReadBoolean(); break;
                    case "preserve-palette": settings.PreservePalette = reader.ReadBoolean(); break;
                    case "dithering-algorithm": settings.DitheringAlgorithm = Serialization.ReadDitheringAlgorithm(reader.ReadString()); break;
                    case "dither-scale": settings.DitherScale = reader.ReadFloat(); break;
                    case "transparency-threshold": settings.TransparencyThreshold = (int)reader.ReadInt64(); break;
                    case "transparency-color": settings.TransparencyColor = Serialization.ReadRgba32(reader.ReadString()); break;
                    case "converter": settings.Converter = reader.ReadString(); break;
                    case "converter-arguments": settings.ConverterArguments = reader.ReadString(); break;
                }
            }
            reader.ReadEndObject();

            return settings;
        }
    }
}

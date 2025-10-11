using SixLabors.ImageSharp.PixelFormats;

namespace Shared
{
    [Flags]
    public enum MdlTextureFlags
    {
        None = 0,

        Flatshaded =            0x01,
        Chrome =                0x02,
        Fullbright =            0x04,
        Mipmaps =               0x08,
        Alpha =                 0x10,
        Additive =              0x20,
        MaskedTransparency =    0x40,   // 1-bit transparency
    }

    public class MdlTexture
    {
        public string Name { get; }
        public MdlTextureFlags Flags { get; }
        public int Width { get; }
        public int Height { get; }

        public byte[] ImageData { get; }
        public Rgba32[] Palette { get; }


        public MdlTexture(string name, MdlTextureFlags flags, int width, int height, byte[] imageData, IEnumerable<Rgba32> palette)
        {
            Name = name;
            Flags = flags;
            Width = width;
            Height = height;

            ImageData = imageData;
            Palette = palette.ToArray();
        }
    }

    public class MdlTextureInfo
    {
        public string Name { get; }
        public MdlTextureFlags Flags { get; }
        public int Width { get; }
        public int Height { get; }
        public int DataOffset { get; }


        public MdlTextureInfo(string name, MdlTextureFlags flags, int width, int height, int dataOffset)
        {
            Name = name;
            Flags = flags;
            Width = width;
            Height = height;
            DataOffset = dataOffset;
        }
    }

    public class MdlSkinData
    {
        public int TextureCount { get; }
        public int SkinCount { get; }
        public byte[] TextureIDs { get; }

        public MdlSkinData(int textureCount, int skinCount, byte[] textureIDs)
        {
            TextureCount = textureCount;
            SkinCount = skinCount;
            TextureIDs = textureIDs;
        }
    }


    public static class Mdl
    {
        private const int FilesizeFieldOffset = 72;
        private const int TextureCountFieldOffset = 180;
        private const int SkinTextureCountFieldOffset = 192;

        private const int TextureNameLength = 64;
        private const int TextureInfoStructSize = 80;


        /// <summary>
        /// Reads texture information from the specified model file.
        /// This does not include image and palette data.
        /// </summary>
        public static MdlTextureInfo[] GetTextureInformation(string path)
        {
            using (var file = File.OpenRead(path))
                return GetTextureInformation(file);
        }

        public static MdlTextureInfo[] GetTextureInformation(Stream stream)
        {
            VerifyFileHeader(stream);
            return ReadTextureInfos(stream);
        }

        public static string GetExternalTexturesFilePath(string path)
            => Path.Combine(Path.GetDirectoryName(path) ?? "", Path.GetFileNameWithoutExtension(path) + "T.mdl");

        /// <summary>
        /// Reads textures from the specified model file, or from its associated *T.mdl file if it has external textures.
        /// </summary>
        public static MdlTexture[] GetTextures(string path, bool includeExternalTextures = true)
        {
            var textures = Array.Empty<MdlTexture>();

            using (var file = File.OpenRead(path))
            {
                VerifyFileHeader(file);
                textures = GetTextures(file);
            }

            if (textures.Length == 0 && includeExternalTextures)
            {
                var externalTexturesFilePath = GetExternalTexturesFilePath(path);
                if (File.Exists(externalTexturesFilePath))
                {
                    using (var file = File.OpenRead(externalTexturesFilePath))
                    {
                        VerifyFileHeader(file);
                        textures = GetTextures(file);
                    }
                }
            }

            return textures;
        }

        public static MdlSkinData GetSkinData(string path)
        {
            using (var file = File.OpenRead(path))
                return GetSkinData(file);
        }

        public static MdlSkinData GetSkinData(Stream stream)
        {
            VerifyFileHeader(stream);

            stream.Position = SkinTextureCountFieldOffset;
            var textureCount = stream.ReadInt();
            var skinCount = stream.ReadInt();
            var skinDataOffset = stream.ReadInt();

            stream.Position = skinDataOffset;
            var textureIDs = stream.ReadBytes(textureCount * skinCount);

            return new MdlSkinData(textureCount, skinCount, textureIDs);
        }

        /// <summary>
        /// Adds textures and skin data to the given model stream.
        /// The model stream must not contain any textures or skin data already.
        /// </summary>
        public static void AddTextures(Stream stream, IReadOnlyList<MdlTexture> newTextures, MdlSkinData skinData, Logger logger)
        {
            stream.Position = 0;
            VerifyFileHeader(stream);
            var textureInfos = ReadTextureInfos(stream);

            if (textureInfos.Any())
                return;


            // New data will be appended, so it starts at the current file size (which is stored in the header, and needs to be updated afterwards):
            stream.Position = FilesizeFieldOffset;
            var oldFileSize = stream.ReadInt();

            // Add skin data:
            stream.Position = SkinTextureCountFieldOffset;
            stream.Write(skinData.TextureCount);
            stream.Write(skinData.SkinCount);
            stream.Write(oldFileSize);

            stream.Position = oldFileSize;
            stream.Write(skinData.TextureIDs);

            var textureInfoOffset = (int)stream.Position;
            var newTextureDataOffset = textureInfoOffset + newTextures.Count * TextureInfoStructSize;

            // Write texture count and file offsets:
            stream.Position = TextureCountFieldOffset;
            stream.Write(newTextures.Count);
            stream.Write(textureInfoOffset);
            stream.Write(newTextureDataOffset);

            // Write texture information:
            stream.Position = textureInfoOffset;
            foreach (var texture in newTextures)
            {
                stream.Write(texture.Name, TextureNameLength);
                stream.Write((int)texture.Flags);
                stream.Write(texture.Width);
                stream.Write(texture.Height);
                stream.Write(newTextureDataOffset);

                newTextureDataOffset += texture.Width * texture.Height + 3 * Constants.MaxPaletteSize;
            }

            // Write texture data:
            foreach (var texture in newTextures)
            {
                stream.Write(texture.ImageData);
                WritePalette(stream, texture.Palette);
            }

            // Update file size header field:
            var newFileSize = (int)stream.Position;

            stream.Position = FilesizeFieldOffset;
            stream.Write(newFileSize);

            stream.Position = 0;
        }

        /// <summary>
        /// Replaces textures in the given model stream.
        /// The model stream must contain the same number of textures already, and the new textures must have the same size as the existing textures.
        /// </summary>
        public static void ReplaceTextures(Stream stream, IReadOnlyList<MdlTexture> newTextures, Logger logger)
        {
            stream.Position = 0;
            VerifyFileHeader(stream);
            var textureInfos = ReadTextureInfos(stream);

            if (!textureInfos.Any())
                return;


            // Replace existing textures:
            if (newTextures.Count != textureInfos.Length)
            {
                logger.Log($"- WARNING: Texture count mismatch. Textures will not be replaced.");
                return;
            }

            stream.Position = 184;
            var textureInfoOffset = stream.ReadInt();
            for (int i = 0; i < textureInfos.Length; i++)
            {
                var textureInfo = textureInfos[i];
                var newTexture = newTextures[i];

                if (newTexture.Width != textureInfo.Width || newTexture.Height != textureInfo.Height)
                {
                    logger.Log($"- WARNING: '{newTexture.Name}' has different dimensions ({newTexture.Width} x {newTexture.Height} instead of {textureInfo.Width} x {textureInfo.Height}). Skipping texture.");
                    continue;
                }

                // Update texture name and flags:
                stream.Position = textureInfoOffset + i * TextureInfoStructSize;
                stream.Write(newTexture.Name, TextureNameLength);
                stream.Write((int)newTexture.Flags);
                // NOTE: width, height and data offset remain the same.

                // Write image data and palette:
                stream.Position = textureInfo.DataOffset;
                stream.Write(newTexture.ImageData);
                WritePalette(stream, newTexture.Palette);
            }

            stream.Position = 0;
        }


        private static void VerifyFileHeader(Stream stream)
        {
            var fileSignature = stream.ReadString(4);
            if (fileSignature != "IDST")
                throw new InvalidDataException($"Expected file to start with 'IDST' but found '{fileSignature}'.");

            var version = stream.ReadInt();
            if (version != 10)
                throw new NotSupportedException("Only MDL v10 is supported.");
        }

        private static MdlTextureInfo[] ReadTextureInfos(Stream stream)
        {
            // Skip most of the header to read texture information:
            stream.Position = TextureCountFieldOffset;
            var textureCount = stream.ReadInt();
            var textureOffset = stream.ReadInt();

            // Read texture information:
            stream.Position = textureOffset;
            var textureInfos = new MdlTextureInfo[textureCount];
            for (int i = 0; i < textureCount; i++)
            {
                textureInfos[i] = new MdlTextureInfo(
                    name: stream.ReadString(TextureNameLength),
                    flags: (MdlTextureFlags)stream.ReadInt(),
                    width: stream.ReadInt(),
                    height: stream.ReadInt(),
                    dataOffset: stream.ReadInt());
            }
            return textureInfos;
        }

        private static MdlTexture[] GetTextures(Stream stream)
        {
            var textureInfos = ReadTextureInfos(stream);

            var textures = new MdlTexture[textureInfos.Length];
            for (int i = 0; i < textureInfos.Length; i++)
            {
                var textureInfo = textureInfos[i];

                stream.Position = textureInfo.DataOffset;
                var imageData = stream.ReadBytes(textureInfo.Width * textureInfo.Height);
                var palette = Enumerable.Range(0, Constants.MaxPaletteSize)
                    .Select(i => stream.ReadColor())
                    .ToArray();
                textures[i] = new MdlTexture(textureInfo.Name, textureInfo.Flags, textureInfo.Width, textureInfo.Height, imageData, palette);
            }
            return textures;
        }

        private static void WritePalette(Stream stream, Rgba32[] palette)
        {
            for (int i = 0; i < Constants.MaxPaletteSize && i < palette.Length; i++)
                stream.Write(palette[i]);

            if (palette.Length < Constants.MaxPaletteSize)
            {
                for (int i = palette.Length; i < Constants.MaxPaletteSize; i++)
                    stream.Write(new Rgba32(0, 0, 0));
            }
        }
    }
}

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

    public static class Mdl
    {
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

        /// <summary>
        /// Reads textures from the specified model file, or from its associated *T.mdl file if it has external textures.
        /// </summary>
        public static MdlTexture[] GetTextures(string path, bool includeExternalTextures = true)
        {
            var modelName = "";
            var textures = Array.Empty<MdlTexture>();

            using (var file = File.OpenRead(path))
            {
                VerifyFileHeader(file);
                modelName = Path.GetFileNameWithoutExtension(file.ReadString(64));
                textures = GetTextures(file);
            }

            if (textures.Length == 0 && includeExternalTextures)
            {
                var externalTexturesFilePath = Path.Combine(Path.GetDirectoryName(path)!, modelName + "T.mdl");
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
            stream.Position = 180;
            var textureCount = stream.ReadInt();
            var textureOffset = stream.ReadInt();

            // Read texture information:
            stream.Position = textureOffset;
            var textureInfos = new MdlTextureInfo[textureCount];
            for (int i = 0; i < textureCount; i++)
            {
                textureInfos[i] = new MdlTextureInfo(
                    name: stream.ReadString(64),
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
    }
}

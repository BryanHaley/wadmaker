using Shared.FileFormats.Indexed;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Shared.FileFormats
{
    /// <summary>
    /// An image reader for common image formats (.png, .jpg, .gif, .bmp and .tga).
    /// </summary>
    public class ImageReader : IImageReader
    {
        public string[] SupportedExtensions => Configuration.Default.ImageFormats
            .SelectMany(format => format.FileExtensions)
            .ToArray();


        public Image<Rgba32> ReadImage(string path)
        {
            if (Path.GetExtension(path).ToLowerInvariant() == ".tga")
                return LoadTga(path);

            return Image.Load<Rgba32>(path);
        }


        /// <summary>
        /// Some libraries create 32-bit RGBA tga files where the image descriptor byte doesn't correctly indicate that each pixel has 8 bits of extra information (alpha channel),
        /// causing ImageSharp to ignore the alpha channel data entirely. This method patches that byte to ensure that such tga files are loaded correctly.
        /// </summary>
        private Image<Rgba32> LoadTga(string path)
        {
            using (var file = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                if (!HasIncorrectTgaImageDescriptor(file))
                    return Image.Load<Rgba32>(file);


                file.Position = 17;
                var imageDescriptorByte = file.ReadByte();
                file.Position = 0;
                using (var stream = new PatchingStream(file, true))
                {
                    // Patch the image descriptor byte to properly report 8 bits of extra data per pixel:
                    stream.AddPatch(17, new byte[] { (byte)(imageDescriptorByte | 0x08) });
                    return Image.Load<Rgba32>(stream);
                }
            }
        }

        private bool HasIncorrectTgaImageDescriptor(Stream stream)
        {
            var pos = stream.Position;

            var header = stream.ReadBytes(18);
            var isUncompressedRGB = header[2] == 2;
            var hasNoColorMap = header[5] == 0 && header[6] == 0;
            var is32Bpp = header[16] == 32;
            var hasZeroAlphaBits = header[17] == 0;

            stream.Position = pos;

            return isUncompressedRGB && is32Bpp && hasNoColorMap && hasZeroAlphaBits;
        }
    }
}

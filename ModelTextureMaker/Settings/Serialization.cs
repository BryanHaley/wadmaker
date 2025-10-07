using Shared;
using SixLabors.ImageSharp.PixelFormats;

namespace ModelTextureMaker.Settings
{
    static class Serialization
    {
        public static string ToString(ColorMask textureType)
        {
            switch (textureType)
            {
                default:
                case ColorMask.Main: return "main";
                case ColorMask.Color1: return "color1";
                case ColorMask.Color2: return "color2";
            }
        }

        public static ColorMask? ReadColorMask(string? str)
        {
            if (str is null)
                return null;

            switch (str.ToLowerInvariant())
            {
                default: throw new InvalidDataException($"Invalid color mask: '{str}'.");
                case "main": return ColorMask.Main;
                case "color1": return ColorMask.Color1;
                case "color2": return ColorMask.Color2;
            }
        }


        public static string ToString(DitheringAlgorithm ditheringAlgorithm)
        {
            switch (ditheringAlgorithm)
            {
                default:
                case DitheringAlgorithm.None: return "none";
                case DitheringAlgorithm.FloydSteinberg: return "floyd-steinberg";
            }
        }

        public static DitheringAlgorithm? ReadDitheringAlgorithm(string? str)
        {
            if (str is null)
                return null;

            switch (str.ToLowerInvariant())
            {
                default: throw new InvalidDataException($"Invalid dithering algorithm: '{str}'.");
                case "none": return DitheringAlgorithm.None;
                case "floyd-steinberg": return DitheringAlgorithm.FloydSteinberg;
            }
        }


        public static string ToString(Rgba32 color) => color.ToHex();

        public static Rgba32? ReadRgba32(string? str) => str is null ? null : Rgba32.ParseHex(str);
    }
}

using SixLabors.ImageSharp.PixelFormats;

namespace Shared
{
    public static class Util
    {
        public static Func<Rgba32, bool> MakeTransparencyPredicate(int transparencyThreshold, Rgba32? transparencyMarkingColor = null)
        {
            if (transparencyMarkingColor != null)
            {
                var transparencyColor = transparencyMarkingColor.Value;
                return color => color.A < transparencyThreshold || (color.R == transparencyColor.R && color.G == transparencyColor.G && color.B == transparencyColor.B);
            }
            else
            {
                return color => color.A < transparencyThreshold;
            }
        }
    }
}

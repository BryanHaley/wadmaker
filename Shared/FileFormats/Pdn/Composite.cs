using SixLabors.ImageSharp.PixelFormats;

namespace Shared.FileFormats.Pdn
{
    internal static class Composite
    {
        public static Rgba32 Over(Rgba32 bottomColor, Rgba32 topColor, byte topAlpha, Rgba32 blendedColor)
        {
            topAlpha = (byte)((topAlpha * topColor.A + 127) / 255);
            var scaledBottomAlpha = (bottomColor.A * (255 - topAlpha) + 127) / 255;
            var compositeAlpha = topAlpha + scaledBottomAlpha;
            if (compositeAlpha == 0)
                return bottomColor;

            var blendedAlpha = (topAlpha * bottomColor.A + 127) / 255;
            var topUnblendedAlpha = topAlpha - blendedAlpha;

            return new Rgba32(
                (byte)(((topColor.R * topUnblendedAlpha) + (bottomColor.R * scaledBottomAlpha) + (blendedColor.R * blendedAlpha)) / compositeAlpha),
                (byte)(((topColor.G * topUnblendedAlpha) + (bottomColor.G * scaledBottomAlpha) + (blendedColor.G * blendedAlpha)) / compositeAlpha),
                (byte)(((topColor.B * topUnblendedAlpha) + (bottomColor.B * scaledBottomAlpha) + (blendedColor.B * blendedAlpha)) / compositeAlpha),
                (byte)compositeAlpha);
        }
    }
}

using SixLabors.ImageSharp.PixelFormats;

namespace Shared.FileFormats.Pdn
{
    internal static class Blend
    {
        public static Rgba32 Normal(Rgba32 bottom, Rgba32 top) => top;

        public static Rgba32 Multiply(Rgba32 bottom, Rgba32 top)
        {
            return new Rgba32(
                (byte)((bottom.R * top.R + 127) / 255),
                (byte)((bottom.G * top.G + 127) / 255),
                (byte)((bottom.B * top.B + 127) / 255),
                top.A);
        }

        public static Rgba32 Additive(Rgba32 bottom, Rgba32 top)
        {
            return new Rgba32(
                (byte)(Math.Min(bottom.R + top.R, 255)),
                (byte)(Math.Min(bottom.G + top.G, 255)),
                (byte)(Math.Min(bottom.B + top.B, 255)),
                top.A);
        }

        public static Rgba32 ColorBurn(Rgba32 bottom, Rgba32 top)
        {
            return new Rgba32(
                (byte)(top.R == 0 ? 0 : 255 - Math.Min((255 - bottom.R) * 255 / top.R, 255)),
                (byte)(top.G == 0 ? 0 : 255 - Math.Min((255 - bottom.G) * 255 / top.G, 255)),
                (byte)(top.B == 0 ? 0 : 255 - Math.Min((255 - bottom.B) * 255 / top.B, 255)),
                top.A);
        }

        public static Rgba32 ColorDodge(Rgba32 bottom, Rgba32 top)
        {
            return new Rgba32(
                (byte)(top.R == 255 ? 255 : Math.Min(bottom.R * 255 / (255 - top.R), 255)),
                (byte)(top.G == 255 ? 255 : Math.Min(bottom.G * 255 / (255 - top.G), 255)),
                (byte)(top.B == 255 ? 255 : Math.Min(bottom.B * 255 / (255 - top.B), 255)),
                top.A);
        }

        public static Rgba32 Reflect(Rgba32 bottom, Rgba32 top)
        {
            return new Rgba32(
                (byte)(top.R == 255 ? 255 : Math.Min(bottom.R * bottom.R / (255 - top.R), 255)),
                (byte)(top.G == 255 ? 255 : Math.Min(bottom.G * bottom.G / (255 - top.G), 255)),
                (byte)(top.B == 255 ? 255 : Math.Min(bottom.B * bottom.B / (255 - top.B), 255)),
                top.A);
        }

        public static Rgba32 Glow(Rgba32 bottom, Rgba32 top)
        {
            return new Rgba32(
                (byte)(bottom.R == 255 ? 255 : Math.Min(top.R * top.R / (255 - bottom.R), 255)),
                (byte)(bottom.G == 255 ? 255 : Math.Min(top.G * top.G / (255 - bottom.G), 255)),
                (byte)(bottom.B == 255 ? 255 : Math.Min(top.B * top.B / (255 - bottom.B), 255)),
                top.A);
        }

        public static Rgba32 Overlay(Rgba32 bottom, Rgba32 top)
        {
            return new Rgba32(
                (byte)(bottom.R < 128 ? ((2 * bottom.R * top.R + 127) / 255) : (255 - (2 * (255 - bottom.R) * (255 - top.R) + 127) / 255)),
                (byte)(bottom.G < 128 ? ((2 * bottom.G * top.G + 127) / 255) : (255 - (2 * (255 - bottom.G) * (255 - top.G) + 127) / 255)),
                (byte)(bottom.B < 128 ? ((2 * bottom.B * top.B + 127) / 255) : (255 - (2 * (255 - bottom.B) * (255 - top.B) + 127) / 255)),
                top.A);
        }

        public static Rgba32 Difference(Rgba32 bottom, Rgba32 top)
        {
            return new Rgba32(
                (byte)(Math.Abs(bottom.R - top.R)),
                (byte)(Math.Abs(bottom.G - top.G)),
                (byte)(Math.Abs(bottom.B - top.B)),
                top.A);
        }

        public static Rgba32 Negation(Rgba32 bottom, Rgba32 top)
        {
            return new Rgba32(
                (byte)(255 - Math.Abs(255 - bottom.R - top.R)),
                (byte)(255 - Math.Abs(255 - bottom.G - top.G)),
                (byte)(255 - Math.Abs(255 - bottom.B - top.B)),
                top.A);
        }

        public static Rgba32 Lighten(Rgba32 bottom, Rgba32 top)
        {
            return new Rgba32(
                (byte)(Math.Max(bottom.R, top.R)),
                (byte)(Math.Max(bottom.G, top.G)),
                (byte)(Math.Max(bottom.B, top.B)),
                top.A);
        }

        public static Rgba32 Darken(Rgba32 bottom, Rgba32 top)
        {
            return new Rgba32(
                (byte)(Math.Min(bottom.R, top.R)),
                (byte)(Math.Min(bottom.G, top.G)),
                (byte)(Math.Min(bottom.B, top.B)),
                top.A);
        }

        public static Rgba32 Screen(Rgba32 bottom, Rgba32 top)
        {
            return new Rgba32(
                (byte)(255 - ((255 - bottom.R) * (255 - top.R) + 127) / 255),
                (byte)(255 - ((255 - bottom.G) * (255 - top.G) + 127) / 255),
                (byte)(255 - ((255 - bottom.B) * (255 - top.B) + 127) / 255),
                top.A);
        }

        public static Rgba32 Xor(Rgba32 bottom, Rgba32 top)
        {
            return new Rgba32(
                (byte)(bottom.R ^ top.R),
                (byte)(bottom.G ^ top.G),
                (byte)(bottom.B ^ top.B),
                top.A);
        }
    }
}

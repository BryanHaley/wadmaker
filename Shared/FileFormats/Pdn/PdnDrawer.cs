using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp;

namespace Shared.FileFormats.Pdn
{
    internal class PdnDrawer
    {
        internal static Image<Rgba32> CreateCompositeImage(PdnDocument pdnDocument)
        {
            var image = new Image<Rgba32>(pdnDocument.Width, pdnDocument.Height);
            var firstLayer = true;
            foreach (var layer in pdnDocument.Layers)
            {
                if (layer?.Properties.IsVisible != true)
                    continue;

                if (firstLayer)
                {
                    DrawFirstLayer(layer, image);
                    firstLayer = false;
                }
                else
                {
                    DrawLayer(layer, image);
                }
            }
            return image;
        }

        private static void DrawFirstLayer(PdnLayer layer, Image<Rgba32> image)
        {
            image.ProcessPixelRows(accessor =>
            {
                var pixelData = layer.Surface.Data.Data;

                for (int y = 0; y < image.Height; y++)
                {
                    var dataOffset = y * layer.Surface.Stride;
                    var rowSpan = accessor.GetRowSpan(y);
                    for (int x = 0; x < image.Width; x++)
                    {
                        var color = GetPixel(pixelData, ref dataOffset);
                        color.A = (byte)((color.A * layer.Properties.Opacity + 127) / 255);
                        rowSpan[x] = color;
                    }
                }
            });
        }

        private static void DrawLayer(PdnLayer layer, Image<Rgba32> image)
        {
            image.ProcessPixelRows(accessor =>
            {
                var pixelData = layer.Surface.Data.Data;
                var blendFunction = GetBlendFunction(layer.Properties.BlendMode);

                for (int y = 0; y < image.Height; y++)
                {
                    var dataOffset = y * layer.Surface.Stride;
                    var rowSpan = accessor.GetRowSpan(y);
                    for (int x = 0; x < image.Width; x++)
                    {
                        var bottomColor = rowSpan[x];
                        var topColor = GetPixel(pixelData, ref dataOffset);
                        var blendedColor = blendFunction(bottomColor, topColor);
                        var compositedColor = Composite.Over(bottomColor, topColor, layer.Properties.Opacity, blendedColor);

                        rowSpan[x] = compositedColor;
                    }
                }
            });
        }

        private static Func<Rgba32, Rgba32, Rgba32> GetBlendFunction(PdnLayerBlendMode blendMode)
        {
            switch (blendMode)
            {
                case PdnLayerBlendMode.Normal: return Blend.Normal;
                case PdnLayerBlendMode.Multiply: return Blend.Multiply;
                case PdnLayerBlendMode.Additive: return Blend.Additive;
                case PdnLayerBlendMode.ColorBurn: return Blend.ColorBurn;
                case PdnLayerBlendMode.ColorDodge: return Blend.ColorDodge;
                case PdnLayerBlendMode.Reflect: return Blend.Reflect;
                case PdnLayerBlendMode.Glow: return Blend.Glow;
                case PdnLayerBlendMode.Overlay: return Blend.Overlay;
                case PdnLayerBlendMode.Difference: return Blend.Difference;
                case PdnLayerBlendMode.Negation: return Blend.Negation;
                case PdnLayerBlendMode.Lighten: return Blend.Lighten;
                case PdnLayerBlendMode.Darken: return Blend.Darken;
                case PdnLayerBlendMode.Screen: return Blend.Screen;
                case PdnLayerBlendMode.Xor: return Blend.Xor;

                default: throw new NotSupportedException($"Unknown blend mode: {blendMode}.");
            }
        }

        private static Rgba32 GetPixel(byte[] layerData, ref int offset)
        {
            var b = layerData[offset++];
            var g = layerData[offset++];
            var r = layerData[offset++];
            var a = layerData[offset++];
            return new Rgba32(r, g, b, a);
        }
    }
}

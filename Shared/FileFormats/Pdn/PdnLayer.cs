namespace Shared.FileFormats.Pdn
{
    internal class PdnLayer
    {
        public int Width { get; }
        public int Height { get; }
        public PdnSurface Surface { get; }
        public PdnLayerProperties Properties { get; }


        public PdnLayer(int width, int height, PdnSurface surface, PdnLayerProperties properties)
        {
            Width = width;
            Height = height;
            Surface = surface;
            Properties = properties;
        }
    }
}

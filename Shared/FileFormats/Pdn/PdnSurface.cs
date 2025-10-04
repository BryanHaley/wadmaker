namespace Shared.FileFormats.Pdn
{
    internal class PdnSurface
    {
        public int Width { get; }
        public int Height { get; }
        public int Stride { get; }
        public PdnMemoryBlock Data { get; }


        public PdnSurface(int width, int height, int stride, PdnMemoryBlock data)
        {
            Width = width;
            Height = height;
            Stride = stride;
            Data = data;
        }
    }
}

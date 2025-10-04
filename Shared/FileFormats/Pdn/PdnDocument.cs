namespace Shared.FileFormats.Pdn
{
    internal class PdnDocument
    {
        public int Width { get; }
        public int Height { get; }
        public List<PdnLayer> Layers { get; }
        public Version SavedWith { get; }
        public KeyValuePair<string, string>[] UserMetadata { get; }


        public PdnDocument(int width, int height, PdnLayer[] layers, Version savedWith, KeyValuePair<string, string>[] userMetadata)
        {
            Width = width;
            Height = height;
            Layers = layers.ToList();
            SavedWith = savedWith;
            UserMetadata = userMetadata;
        }
    }
}

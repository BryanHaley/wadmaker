namespace Shared.FileFormats.Pdn
{
    internal class PdnLayerProperties
    {
        public string Name { get; }
        public bool IsVisible { get; }
        public bool IsBackground { get; }
        public byte Opacity { get; }
        public PdnLayerBlendMode BlendMode { get; }
        public KeyValuePair<string, string>[] UserMetadata { get; }


        public PdnLayerProperties(string name, bool isVisible, bool isBackground, byte opacity, PdnLayerBlendMode blendMode, KeyValuePair<string, string>[] userMetadata)
        {
            Name = name;
            IsVisible = isVisible;
            IsBackground = isBackground;
            Opacity = opacity;
            BlendMode = blendMode;
            UserMetadata = userMetadata;
        }
    }
}

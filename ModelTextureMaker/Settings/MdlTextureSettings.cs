using Shared;
using SixLabors.ImageSharp.PixelFormats;

namespace ModelTextureMaker.Settings
{
    enum ColorMask
    {
        Main = 0,
        Color1 = 1,
        Color2 = 2,
    }

    class MdlTextureSettings : IEquatable<MdlTextureSettings>
    {
        /// <summary>
        /// When true, the source image(s) are ignored - as if they don't exist.
        /// </summary>
        public bool? Ignore { get; set; }


        /// <summary>
        /// The color mask, for color remap textures. Defaults to <see cref="ColorMask.Main"/>.
        /// </summary>
        public ColorMask? ColorMask { get; set; }

        /// <summary>
        /// The color count, for color remap textures. Defaults to 32 for color 1 and 2 masks.
        /// </summary>
        public int? ColorCount { get; set; }

        /// <summary>
        /// When true, the texture is treated as a color remap texture, but with color 1 and 2 swapped.
        /// </summary>
        public bool? IsModelPortrait { get; set; }


        /// <summary>
        /// When true, and if the source image is in an indexed format,
        /// the source image's palette and image data are used directly,
        /// without quantization, dithering or other type-specific processing.
        /// </summary>
        public bool? PreservePalette { get; set; }


        /// <summary>
        /// The dithering algorithm to apply when converting a source image to an 8-bit indexed texture.
        /// Defaults to <see cref="DitheringAlgorithm.FloydSteinberg"/>.
        /// </summary>
        public DitheringAlgorithm? DitheringAlgorithm { get; set; }

        /// <summary>
        /// When dithering is enabled, error diffusion is scaled by this factor (0 - 1).
        /// Setting this too high can result in dithering artifacts, setting it too low essentially disables dithering, resulting in banding.
        /// Defaults to 0.75.
        /// </summary>
        public float? DitherScale { get; set; }


        /// <summary>
        /// Pixels with an alpha value below this value will be ignored when the palette is created.
        /// Such pixels will be mapped to the last color in the palette.
        /// Defaults to <see cref="Constants.DefaultTransparencyThreshold">.
        /// </summary>
        public int? TransparencyThreshold { get; set; }

        /// <summary>
        /// Pixels with this color will be ignored when the palette is created.
        /// Such pixels will be mapped to the last color in the palette.
        /// This is not used by default.
        /// </summary>
        public Rgba32? TransparencyColor { get; set; }


        /// <summary>
        /// The command-line application that ModelTextureMaker will call to convert the current file.
        /// This also requires <see cref="ConverterArguments"/> to be set.
        /// ModelTextureMaker will use the output image to create a texture. The output image will be removed afterwards.
        /// </summary>
        public string? Converter { get; set; }

        /// <summary>
        /// The arguments to pass to the converter application. These must include {input} and {output} markers, so ModelTextureMaker can pass the
        /// current file path and the location where the converter application must save the output image.
        /// </summary>
        public string? ConverterArguments { get; set; }


        public MdlTextureSettings()
        {
        }

        public MdlTextureSettings(MdlTextureSettings settings)
        {
            OverrideWith(settings);
        }

        /// <summary>
        /// Updates the current settings with the given settings.
        /// </summary>
        public void OverrideWith(MdlTextureSettings overrideSettings)
        {
            if (overrideSettings.Ignore != null) Ignore = overrideSettings.Ignore;
            if (overrideSettings.ColorMask != null) ColorMask = overrideSettings.ColorMask;
            if (overrideSettings.ColorCount != null) ColorCount = overrideSettings.ColorCount;
            if (overrideSettings.IsModelPortrait != null) IsModelPortrait = overrideSettings.IsModelPortrait;
            if (overrideSettings.PreservePalette != null) PreservePalette = overrideSettings.PreservePalette;
            if (overrideSettings.DitheringAlgorithm != null) DitheringAlgorithm = overrideSettings.DitheringAlgorithm;
            if (overrideSettings.DitherScale != null) DitherScale = overrideSettings.DitherScale;
            if (overrideSettings.TransparencyThreshold != null) TransparencyThreshold = overrideSettings.TransparencyThreshold;
            if (overrideSettings.TransparencyColor != null) TransparencyColor = overrideSettings.TransparencyColor;
            if (overrideSettings.Converter != null) Converter = overrideSettings.Converter;
            if (overrideSettings.ConverterArguments != null) ConverterArguments = overrideSettings.ConverterArguments;
        }

        public bool Equals(MdlTextureSettings? other)
        {
            return other is not null &&
                Ignore == other.Ignore &&
                ColorMask == other.ColorMask &&
                ColorCount == other.ColorCount &&
                IsModelPortrait == other.IsModelPortrait &&
                PreservePalette == other.PreservePalette &&
                DitheringAlgorithm == other.DitheringAlgorithm &&
                DitherScale == other.DitherScale &&
                TransparencyThreshold == other.TransparencyThreshold &&
                TransparencyColor == other.TransparencyColor &&
                Converter == other.Converter &&
                ConverterArguments == other.ConverterArguments;
        }

        public override bool Equals(object? obj) => obj is MdlTextureSettings other && Equals(other);

        public override int GetHashCode() => 0; // Just do an equality check.

        public static bool operator ==(MdlTextureSettings? left, MdlTextureSettings? right) => left?.Equals(right) ?? right is null;
        public static bool operator !=(MdlTextureSettings? left, MdlTextureSettings? right) => !(left?.Equals(right) ?? right is null);
    }
}

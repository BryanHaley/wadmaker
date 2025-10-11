using Shared;
using Shared.FileFormats;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp;
using System.Diagnostics;
using Shared.FileFormats.Indexed;
using ModelTextureMaker.Settings;

namespace ModelTextureMaker
{
    public class MdlExtractionSettings
    {
        public bool OverwriteExistingFiles { get; set; }
        public ImageFormat OutputFormat { get; set; }
        public bool SaveAsIndexed { get; set; }
    }


    public static class MdlTextureExtracting
    {
        public static void ExtractTextures(string inputFilePath, string outputDirectory, MdlExtractionSettings settings, Logger logger)
        {
            var stopwatch = Stopwatch.StartNew();

            logger.Log($"Extracting textures from '{inputFilePath}' and saving the result to '{outputDirectory}'.");

            var imageFilesCreated = 0;

            logger.Log($"Loading model file: '{inputFilePath}'.");
            var textures = Mdl.GetTextures(inputFilePath, includeExternalTextures: true);

            var modelPortraitTexture = TryLoadModelPortrait(inputFilePath, logger);
            if (modelPortraitTexture != null)
                textures = textures.Append(modelPortraitTexture).ToArray();

            Util.CreateDirectory(outputDirectory);

            foreach (var texture in textures)
            {
                try
                {
                    logger.Log($"- Extracting '{texture.Name}'...");

                    if (settings.SaveAsIndexed)
                    {
                        var filePath = Path.Combine(outputDirectory, Path.ChangeExtension(texture.Name, "." + ImageFileIO.GetDefaultExtension(settings.OutputFormat)));
                        if (!settings.OverwriteExistingFiles && File.Exists(filePath))
                        {
                            logger.Log($"  - WARNING: '{filePath}' already exists. Skipping texture.");
                            continue;
                        }

                        logger.Log($"  - Creating image file '{filePath}'.");

                        var indexedImage = new IndexedImage(texture.ImageData, texture.Width, texture.Height, texture.Palette);
                        ImageFileIO.SaveIndexedImage(indexedImage, filePath, settings.OutputFormat);
                        imageFilesCreated += 1;
                    }
                    else
                    {
                        // For remapX textures, take only the "remapX" part, ignoring the subsequent numbers:
                        var baseFilename = texture.Name;
                        var mainColorSettings = new MdlTextureSettings();
                        if (ModelTextureName.IsRemap(texture.Name, out var color1Start, out var color1End, out var color2End))
                        {
                            baseFilename = ModelTextureName.GetRemapName(texture.Name);
                            mainColorSettings.ColorMask = ColorMask.Main;

                            if (color2End < Constants.MaxPaletteSize - 1)
                                mainColorSettings.ColorCount = color1Start;
                        }

                        var isModelPortrait = texture == modelPortraitTexture;
                        if (isModelPortrait)
                            mainColorSettings.IsModelPortrait = true;

                        var baseFilePath = Path.Combine(outputDirectory, Path.ChangeExtension(baseFilename, "." + ImageFileIO.GetDefaultExtension(settings.OutputFormat)));
                        var mainFilePath = MdlTextureMakingSettings.InsertTextureSettingsIntoFilename(baseFilePath, mainColorSettings);
                        if (!settings.OverwriteExistingFiles && File.Exists(mainFilePath))
                        {
                            logger.Log($"  - WARNING: '{mainFilePath}' already exists. Skipping texture.");
                            continue;
                        }

                        logger.Log($"  - Creating image file '{mainFilePath}'.");

                        using (var image = TextureToImage(texture))
                        {
                            ImageFileIO.SaveImage(image, mainFilePath, settings.OutputFormat);
                            imageFilesCreated += 1;
                        }

                        // Create remap mask images for remap textures:
                        if (ModelTextureName.IsDmBase(texture.Name, out color1Start, out color1End, out color2End) ||
                            ModelTextureName.IsRemap(texture.Name, out color1Start, out color1End, out color2End) ||
                            isModelPortrait)
                        {
                            // Model portraits use the same color ranges as dm_base textures:
                            if (isModelPortrait)
                            {
                                color1Start = 160;
                                color1End = 191;
                                color2End = 223;
                            }

                            foreach (var colorMask in new[] { ColorMask.Color1, ColorMask.Color2 })
                            {
                                var colorStart = colorMask == ColorMask.Color1 ? color1Start : color1End + 1;
                                var colorEnd = colorMask == ColorMask.Color1 ? color1End : color2End;

                                // Skip this file if the color range is empty:
                                if (colorEnd < colorStart)
                                    continue;

                                using (var remapColorImage = TextureToRemapColorImage(texture, colorStart, colorEnd))
                                {
                                    var remapColorSettings = new MdlTextureSettings { ColorMask = colorMask };
                                    var colorCount = colorEnd - colorStart + 1;
                                    if (colorCount != 32)
                                        remapColorSettings.ColorCount = colorCount;

                                    // Color1 and color2 are swapped in model portraits:
                                    if (isModelPortrait)
                                        remapColorSettings.ColorMask = (colorMask == ColorMask.Color1) ? ColorMask.Color2 : ColorMask.Color1;


                                    var remapColorFilePath = MdlTextureMakingSettings.InsertTextureSettingsIntoFilename(baseFilePath, remapColorSettings);

                                    if (!settings.OverwriteExistingFiles && File.Exists(remapColorFilePath))
                                    {
                                        logger.Log($"  - WARNING: '{remapColorFilePath}' already exists. Skipping {colorMask} image.");
                                    }
                                    else
                                    {
                                        logger.Log($"  - Creating image file '{remapColorFilePath}'.");

                                        ImageFileIO.SaveImage(remapColorImage, remapColorFilePath, settings.OutputFormat);
                                        imageFilesCreated += 1;
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    logger.Log($"  - ERROR: failed to extract '{texture.Name}': {ex.GetType().Name}: '{ex.Message}'.");
                }
            }

            logger.Log($"Extracted {imageFilesCreated} images from {textures.Length} textures from '{inputFilePath}' to '{outputDirectory}', in {stopwatch.Elapsed.TotalSeconds:0.000} seconds.");
        }


        private static MdlTexture? TryLoadModelPortrait(string inputFilePath, Logger logger)
        {
            try
            {
                // Extract model portait image, if it exists:
                var modelPortraitFilePath = Path.ChangeExtension(inputFilePath, ".bmp");
                if (!File.Exists(modelPortraitFilePath))
                    return null;

                var indexedImage = ImageFileIO.LoadIndexedImage(modelPortraitFilePath);
                return new MdlTexture(Path.GetFileName(modelPortraitFilePath), MdlTextureFlags.None, indexedImage.Width, indexedImage.Height, indexedImage.ImageData, indexedImage.Palette);
            }
            catch (Exception ex)
            {
                logger.Log($"- WARNING: Failed to open '{TryLoadModelPortrait}', skipping file.");
                return null;
            }
        }


        public static Image<Rgba32> TextureToImage(MdlTexture texture)
        {
            var hasColorKey = texture.Flags.HasFlag(MdlTextureFlags.MaskedTransparency);
            var image = new Image<Rgba32>(texture.Width, texture.Height);
            image.ProcessPixelRows(accessor =>
            {
                for (int y = 0; y < image.Height; y++)
                {
                    var rowSpan = accessor.GetRowSpan(y);
                    for (int x = 0; x < image.Width; x++)
                    {
                        var paletteIndex = texture.ImageData[y * texture.Width + x];
                        if (paletteIndex == Constants.TransparentColorIndex && hasColorKey)
                        {
                            rowSpan[x] = new Rgba32(0, 0, 0, 0);
                        }
                        else
                        {
                            rowSpan[x] = texture.Palette[paletteIndex];
                        }
                    }
                }
            });

            return image;
        }

        private static Image<Rgba32> TextureToRemapColorImage(MdlTexture texture, int firstColor, int lastColor)
        {
            var image = new Image<Rgba32>(texture.Width, texture.Height);
            image.ProcessPixelRows(accessor =>
            {
                for (int y = 0; y < image.Height; y++)
                {
                    var rowSpan = accessor.GetRowSpan(y);
                    for (int x = 0; x < image.Width; x++)
                    {
                        var paletteIndex = texture.ImageData[y * texture.Width + x];
                        if (paletteIndex < firstColor || paletteIndex > lastColor)
                        {
                            rowSpan[x] = new Rgba32(0, 0, 0, 0);
                        }
                        else
                        {
                            rowSpan[x] = texture.Palette[paletteIndex];
                        }
                    }
                }
            });

            return image;
        }
    }
}

using ModelTextureMaker.Settings;
using Shared;
using Shared.FileFormats;
using System.Diagnostics;

namespace ModelTextureMaker
{
    public static class MdlTextureReplacing
    {
        public static void ReplaceTextures(string inputDirectory, string modelFilePath, string outputFilePath, Logger logger)
        {
            var stopwatch = Stopwatch.StartNew();

            logger.Log($"Replacing textures in '{modelFilePath}', using images from '{inputDirectory}', and saving the result to '{outputFilePath}'.");

            // First load the model textures:
            var modelTextures = Mdl.GetTextures(modelFilePath, includeExternalTextures: false);
            var hasExternalTextures = !modelTextures.Any();
            if (hasExternalTextures)
                modelTextures = Mdl.GetTextures(modelFilePath, includeExternalTextures: true);

            var textureNames = modelTextures
                .Select(info => Path.GetFileNameWithoutExtension(info.Name).ToLowerInvariant())
                .Select(name => ModelTextureName.IsRemap(name) ? ModelTextureName.GetRemapName(name) : name);
            var textureNameLookup = textureNames
                .Select((name, index) => new { name, index })
                .ToDictionary(item => item.name, item => item.index);


            // Next, select all files whose texture name matches an existing model texture:
            var textureMakingSettings = MdlTextureMakingSettings.Load(inputDirectory);
            var conversionOutputDirectory = ExternalConversion.GetConversionOutputDirectory(inputDirectory);

            var textureSourceFileGroups = MdlTextureMaking.GetInputFilePaths(inputDirectory)
                .GroupBy(path => MdlTextureMakingSettings.GetTextureName(path).ToLowerInvariant())
                .Where(group => textureNameLookup.ContainsKey(group.Key))
                .SelectMany(group => group)
                .Select(textureMakingSettings.GetTextureSourceFileInfo)
                .Where(file => file.Settings.Ignore != true && (ImageFileIO.CanLoad(file.Path) || !string.IsNullOrEmpty(file.Settings.Converter)))
                .GroupBy(file => MdlTextureMakingSettings.GetTextureName(file.Path).ToLowerInvariant())
                .ToArray();

            // Build the textures, and replace them in the original model textures array:
            var replacedTexturesCount = 0;
            try
            {
                foreach (var textureSourceFileGroup in textureSourceFileGroups)
                {
                    try
                    {
                        var textureName = textureSourceFileGroup.Key;
                        var textureSourceFiles = textureSourceFileGroup.ToArray();

                        var texture = MdlTextureMaking.MakeTexture(textureName, textureSourceFiles, textureMakingSettings, conversionOutputDirectory, logger);
                        if (texture == null)
                            continue;


                        if (textureNameLookup.TryGetValue(texture.Name, out var index))
                        {
                            var oldTexture = modelTextures[index];

                            if (texture.Width != oldTexture.Width || texture.Height != oldTexture.Height)
                            {
                                logger.Log($"- WARNING: '{texture.Name}' has a different size than the existing texture. Skipping texture.");
                                continue;
                            }


                            logger.Log($"- Replacing '{texture.Name}'.");

                            var flags = oldTexture.Flags;
                            if (texture.Flags.HasFlag(MdlTextureFlags.MaskedTransparency))
                                flags &= MdlTextureFlags.MaskedTransparency;
                            else
                                flags &= ~MdlTextureFlags.MaskedTransparency;

                            modelTextures[index] = new MdlTexture(texture.Name + ".bmp", flags, texture.Width, texture.Height, texture.ImageData, texture.Palette);
                            replacedTexturesCount += 1;
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.Log($"- WARNING: Failed to make texture '{textureSourceFileGroup.Key}': {ex.GetType().Name}: '{ex.Message}'.");
                    }
                }
            }
            finally
            {
                try
                {
                    if (Directory.Exists(conversionOutputDirectory))
                        Directory.Delete(conversionOutputDirectory, true);
                }
                catch (Exception ex)
                {
                    logger.Log($"- WARNING: Failed to delete temporary conversion output directory: {ex.GetType().Name}: '{ex.Message}'.");
                }
            }


            // Load the model file into memory and replace the textures (or add them, if the textures were originally stored externally):
            var modelFileContent = new MemoryStream();
            using (var file = File.Open(modelFilePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                file.CopyTo(modelFileContent);

            if (hasExternalTextures)
            {
                logger.Log("Replacing textures and merging them into the output model file.");

                var skinData = Mdl.GetSkinData(Mdl.GetExternalTexturesFilePath(modelFilePath));
                Mdl.AddTextures(modelFileContent, modelTextures, skinData, logger);
            }
            else
            {
                logger.Log("Replacing textures in the output model file.");

                Mdl.ReplaceTextures(modelFileContent, modelTextures, logger);
            }

            // Finally, save the result:
            using (var file = File.Create(outputFilePath))
                modelFileContent.CopyTo(file);

            logger.Log($"Replaced {replacedTexturesCount} textures in '{modelFilePath}' from '{inputDirectory}', and saved output to '{outputFilePath}', in {stopwatch.Elapsed.TotalSeconds:0.000} seconds.");
        }
    }
}

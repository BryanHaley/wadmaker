using Shared;
using SixLabors.ImageSharp;
using Shared.FileFormats;
using System.Reflection;
using System.Text;
using System.Diagnostics.CodeAnalysis;

namespace ModelTextureMaker
{
    class ProgramSettings
    {
        // Build settings:
        public bool FullRebuild { get; set; }                   // -full            forces a full rebuild instead of an incremental one
        public bool IncludeSubDirectories { get; set; }         // -subdirs         also processes files in sub-directories
        public bool EnableSubDirectoryRemoval { get; set; }     // -subdirremoval   enables deleting of output sub-directories when input sub-directories are removed

        // Extract settings:
        [MemberNotNullWhen(true, nameof(InputFilePath))]
        [MemberNotNullWhen(true, nameof(OutputDirectory))]
        public bool Extract { get; set; }                       //                  Texture extraction mode is enabled when the first argument (path) is an mdl file.
        public bool OverwriteExistingFiles { get; set; }        // -overwrite       Extract mode only, enables overwriting of existing image files (off by default).
        public ImageFormat OutputImageFormat { get; set; }      // -format          Extracted images output format (png, jpg, gif, bmp or tga).
        public bool ExtractAsIndexed { get; set; }              // -indexed         Extracted images are indexed and contain the original texture's palette. Only works with png, gif and bmp.

        // Replacement settings:
        [MemberNotNullWhen(true, nameof(InputDirectory))]
        [MemberNotNullWhen(true, nameof(ExtraInputFilePath))]
        [MemberNotNullWhen(true, nameof(OutputFilePath))]
        public bool ReplaceTextures { get; set; }               //                  Replacement mode is enabled when the first argument (path) is a directory, and the second path is an mdl file.

        // Other settings:
        public string? InputDirectory { get; set; }             // Build mode only
        public string? InputFilePath { get; set; }              // Mdl path
        public string? ExtraInputFilePath { get; set; }         // Mdl path (when replacing textures)
        public string? OutputDirectory { get; set; }            // Build and extract modes only
        public string? OutputFilePath { get; set; }             // Output mdl path (when replacing textures)

        public bool DisableFileLogging { get; set; }            // -nologfile       Disables logging to a file (parent-directory\modeltexturemaker.log)
    }

    class Program
    {
        static TextWriter? LogFile;


        static void Main(string[] args)
        {
            try
            {
                var assemblyName = Assembly.GetExecutingAssembly().GetName();
                var launchInfo = $"{assemblyName.Name}.exe (v{assemblyName.Version}) {string.Join(" ", args)}";
                Log(launchInfo);

                var settings = ParseArguments(args);
                if (!settings.DisableFileLogging)
                {
                    var logName = Path.GetFileNameWithoutExtension(settings.InputDirectory ?? settings.InputFilePath);
                    var logFilePath = Path.Combine(Path.GetDirectoryName(settings.InputDirectory ?? settings.InputFilePath) ?? "", $"modeltexturemaker - {logName}.log");
                    LogFile = new StreamWriter(logFilePath, false, Encoding.UTF8);
                    LogFile.WriteLine(launchInfo);
                }

                var logger = new Logger(Log);
                if (settings.Extract)
                {
                    var extractionSettings = new MdlExtractionSettings {
                        OverwriteExistingFiles = settings.OverwriteExistingFiles,
                        OutputFormat = settings.OutputImageFormat,
                        SaveAsIndexed = settings.ExtractAsIndexed,
                    };
                    MdlTextureExtracting.ExtractTextures(settings.InputFilePath, settings.OutputDirectory, extractionSettings, logger);
                }
                else if (settings.ReplaceTextures)
                {
                    MdlTextureReplacing.ReplaceTextures(settings.InputDirectory, settings.ExtraInputFilePath, settings.OutputFilePath, logger);
                }
                else
                {
                    MdlTextureMaking.MakeTextures(settings.InputDirectory!, settings.OutputDirectory!, settings.FullRebuild, settings.IncludeSubDirectories, settings.EnableSubDirectoryRemoval, logger);
                }
            }
            catch (InvalidUsageException ex)
            {
                Log($"ERROR: {ex.Message}");
            }
            catch (Exception ex)
            {
                Log($"ERROR: {ex.GetType().Name}: '{ex.Message}'.");
                Log(ex.StackTrace);
            }
            finally
            {
                LogFile?.Dispose();
            }
        }


        private static ProgramSettings ParseArguments(string[] args)
        {
            var settings = new ProgramSettings();

            // First parse options:
            var index = 0;
            while (index < args.Length && args[index].StartsWith("-"))
            {
                var arg = args[index++];
                switch (arg)
                {
                    case "-subdirs": settings.IncludeSubDirectories = true; break;
                    case "-full": settings.FullRebuild = true; break;
                    case "-subdirremoval": settings.EnableSubDirectoryRemoval = true; break;
                    case "-overwrite": settings.OverwriteExistingFiles = true; break;

                    case "-format":
                        if (index >= args.Length)
                            throw new InvalidUsageException("The -format parameter must be set to either png, jpg, gif, bmp or tga.");

                        settings.OutputImageFormat = ParseOutputImageFormat(args[index++]);
                        break;

                    case "-indexed": settings.ExtractAsIndexed = true; break;
                    case "-nologfile": settings.DisableFileLogging = true; break;

                    default: throw new InvalidUsageException($"Unknown argument: '{arg}'.");
                }
            }

            // Then handle arguments (paths):
            var paths = args.Skip(index).ToArray();
            if (paths.Length == 0)
                throw new InvalidUsageException("Missing input folder (for texture building) or file (for texture extraction) argument.");

            if (File.Exists(paths[0]))
            {
                // Extract mode: input.mdl output_dir*
                var extension = Path.GetExtension(paths[0]).ToLowerInvariant();
                if (extension == ".mdl")
                {
                    settings.Extract = true;
                    settings.InputFilePath = paths[0];

                    if (paths.Length > 1)
                        settings.OutputDirectory = paths[1];
                    else
                        settings.OutputDirectory = Path.Combine(Path.GetDirectoryName(settings.InputFilePath)!, Path.GetFileNameWithoutExtension(settings.InputFilePath) + "_extracted");
                }
            }
            else if (paths.Length > 1 && File.Exists(paths[1]))
            {
                // Replace mode: input_dir input.mdl output.mdl*
                var extension = Path.GetExtension(paths[1]).ToLowerInvariant();
                if (extension == ".mdl")
                {
                    settings.ReplaceTextures = true;
                    settings.InputDirectory = paths[0];
                    settings.ExtraInputFilePath = paths[1];

                    if (paths.Length > 2)
                        settings.OutputFilePath = paths[2];
                    else
                        settings.OutputFilePath = settings.ExtraInputFilePath;
                }
            }
            else
            {
                // Build mode: input_dir output_dir*
                settings.InputDirectory = paths[0];

                if (paths.Length > 1)
                    settings.OutputDirectory = paths[1];
                else
                    settings.OutputDirectory = paths[0] + "_textures";
            }

            return settings;
        }

        private static ImageFormat ParseOutputImageFormat(string str)
        {
            switch (str.ToLowerInvariant())
            {
                case "png": return ImageFormat.Png;
                case "jpg": return ImageFormat.Jpg;
                case "gif": return ImageFormat.Gif;
                case "bmp": return ImageFormat.Bmp;
                case "tga": return ImageFormat.Tga;

                default: throw new InvalidDataException($"Unknown image format: {str}.");
            }
        }


        private static void Log(string? message)
        {
            Console.WriteLine(message);
            LogFile?.WriteLine(message);
        }
    }
}

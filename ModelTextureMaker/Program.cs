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

        // Other settings:
        public string? InputDirectory { get; set; }             // Build mode only
        public string? OutputDirectory { get; set; }            // Build and extract modes only

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
                    var logName = Path.GetFileNameWithoutExtension(settings.InputDirectory);
                    var logFilePath = Path.Combine(Path.GetDirectoryName(settings.InputDirectory) ?? "", $"modeltexturemaker - {logName}.log");
                    LogFile = new StreamWriter(logFilePath, false, Encoding.UTF8);
                    LogFile.WriteLine(launchInfo);
                }

                var logger = new Logger(Log);
                MdlTextureMaking.MakeTextures(settings.InputDirectory!, settings.OutputDirectory!, settings.FullRebuild, settings.IncludeSubDirectories, settings.EnableSubDirectoryRemoval, logger);
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

                    case "-nologfile": settings.DisableFileLogging = true; break;

                    default: throw new InvalidUsageException($"Unknown argument: '{arg}'.");
                }
            }

            // Then handle arguments (paths):
            var paths = args.Skip(index).ToArray();
            if (paths.Length == 0)
                throw new InvalidUsageException("Missing input folder (for texture building).");

            // Build mode: input_dir output_dir*
            settings.InputDirectory = paths[0];

            if (paths.Length > 1)
                settings.OutputDirectory = paths[1];
            else
                settings.OutputDirectory = paths[0] + "_textures";

            return settings;
        }


        private static void Log(string? message)
        {
            Console.WriteLine(message);
            LogFile?.WriteLine(message);
        }
    }
}

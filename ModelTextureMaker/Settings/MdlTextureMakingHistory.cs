using System.Text.Json;
using FileInfo = Shared.FileSystem.FileInfo;

namespace ModelTextureMaker.Settings
{
    class MdlTextureMakingHistory
    {
        public class MdlTextureHistory
        {
            public FileInfo OutputFile { get; }
            public MdlTextureSourceFileInfo[] InputFiles { get; }

            public MdlTextureHistory(FileInfo outputFile, MdlTextureSourceFileInfo[] inputFiles)
            {
                OutputFile = outputFile;
                InputFiles = inputFiles;
            }
        }


        private static JsonSerializerOptions SerializerOptions { get; }

        static MdlTextureMakingHistory()
        {
            SerializerOptions = new JsonSerializerOptions();
            SerializerOptions.Converters.Add(new MdlTextureMakingHistoryJsonSerializer());
        }


        const string HistoryFilename = "modeltexturemaker.dat";


        public Dictionary<string, MdlTextureHistory> Textures { get; }
        public string[] SubDirectoryNames { get; }


        public MdlTextureMakingHistory(IDictionary<string, MdlTextureHistory> textures, IEnumerable<string> subDirectoryNames)
        {
            Textures = textures.ToDictionary(kv => kv.Key, kv => kv.Value);
            SubDirectoryNames = subDirectoryNames.ToArray();
        }

        public static MdlTextureMakingHistory? Load(string folder)
        {
            try
            {
                var historyFilePath = Path.Combine(folder, HistoryFilename);
                if (!File.Exists(historyFilePath))
                    return null;

                var json = File.ReadAllText(historyFilePath);
                return JsonSerializer.Deserialize<MdlTextureMakingHistory>(json, SerializerOptions);
            }
            catch
            {
                // Error reading file? Just ignore - history only matters when doing incremental updates, and we can always fall back to doing a full rebuild!
                return null;
            }
        }

        public static bool IsHistoryFile(string path) => Path.GetFileName(path) == HistoryFilename;


        public void Save(string folder)
        {
            var historyFilePath = Path.Combine(folder, HistoryFilename);
            File.WriteAllText(historyFilePath, JsonSerializer.Serialize(this, SerializerOptions));
        }
    }
}

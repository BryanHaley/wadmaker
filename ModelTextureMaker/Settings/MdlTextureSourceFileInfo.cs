using Shared.FileSystem;

namespace ModelTextureMaker.Settings
{
    class MdlTextureSourceFileInfo : Shared.FileSystem.FileInfo
    {
        public MdlTextureSettings Settings { get; }


        public MdlTextureSourceFileInfo(string path, int fileSize, FileHash fileHash, DateTimeOffset lastModified, MdlTextureSettings settings)
            : base(path, fileSize, fileHash, lastModified)
        {
            Settings = settings;
        }
    }
}

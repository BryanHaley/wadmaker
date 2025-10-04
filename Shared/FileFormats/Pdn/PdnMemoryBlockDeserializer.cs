using System.IO.Compression;

namespace Shared.FileFormats.Pdn
{
    internal class PdnMemoryBlockDeserializer
    {
        private List<PdnMemoryBlock> _registeredBlocks = new List<PdnMemoryBlock>();

        public void Register(PdnMemoryBlock block)
            => _registeredBlocks.Add(block);

        public void Deserialize(Stream stream)
        {
            var buffer = new byte[4096];
            foreach (var block in _registeredBlocks)
            {
                var dataPosition = 0;

                var compressionLevel = (CompressionLevel)stream.ReadByte();
                var maxChunkSize = stream.ReadUintBigEndian();
                var chunkCount = (block.Data.Length + maxChunkSize - 1) / maxChunkSize;

                for (int i = 0; i < chunkCount; i++)
                {
                    var chunkNumber = stream.ReadUintBigEndian();
                    var chunkSize = stream.ReadUintBigEndian();
                    var endPos = stream.Position + chunkSize;
                    using (var gzipStream = new GZipStream(stream, CompressionMode.Decompress, leaveOpen: true))
                    {
                        while (true)
                        {
                            var bytesRead = gzipStream.Read(buffer, 0, buffer.Length);
                            if (bytesRead == 0)
                                break;

                            Array.Copy(buffer, 0, block.Data, dataPosition, bytesRead);
                            dataPosition += bytesRead;
                        }
                    }

                    // A GZipStream can read past the end of the gzip content, so go back to the end of the gzip content:
                    stream.Position = endPos;
                }
            }
        }
    }
}

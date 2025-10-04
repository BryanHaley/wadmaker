namespace Shared.FileFormats.Pdn
{
    internal class PdnMemoryBlock
    {
        public byte[] Data { get; }


        public PdnMemoryBlock(long length)
        {
            Data = new byte[length];
        }
    }
}

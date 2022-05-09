using System.Text;

namespace RlImGuiApp.LG;

public class DbChunk
{
    public DbChunkHeader Header { get; }
    public Byte[] Data { get; }
    public int DataSize { get; }

    public DbChunk(BinaryReader reader, int dataSize)
    {
        Header = new DbChunkHeader(reader);
        Data = reader.ReadBytes(dataSize);
        DataSize = dataSize;
    }
}

public struct DbChunkHeader
{
    public string Name { get; }
    public DbVersion Version { get; }

    public DbChunkHeader(BinaryReader reader)
    {
        Name = Encoding.UTF8.GetString(reader.ReadBytes(12)).Replace("\0", string.Empty);
        Version = new DbVersion(reader);
        reader.ReadBytes(4);
    }
}
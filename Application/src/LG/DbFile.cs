using System.Text;

namespace RlImGuiApp.LG;

public class DbFile
{
    public DbFileHeader Header { get; }
    public DbToc TableOfContents { get; }
    public string? Filename { get; }

    public DbFile(string filename)
    {
        if (!File.Exists(filename)) return;

        Filename = filename;
        FileStream stream = File.Open(filename, FileMode.Open);
        var reader = new BinaryReader(stream, Encoding.UTF8, false);

        Header = new DbFileHeader(reader);
        stream.Seek(Header.TocOffset, SeekOrigin.Begin);
        TableOfContents = new DbToc(reader);

        reader.Dispose();
    }

    public DbChunk? GetChunk(string chunkName)
    {
        if (Filename == null) return null;
        if (!TableOfContents.Items.ContainsKey(chunkName)) return null;

        var tocEntry = TableOfContents.Items[chunkName];
        FileStream stream = File.Open(Filename, FileMode.Open);
        var reader = new BinaryReader(stream, Encoding.UTF8, false);
        stream.Seek(tocEntry.Offset, SeekOrigin.Begin);
        return new DbChunk(reader, (int) tocEntry.Size);
    }
}

public readonly struct DbFileHeader
{
    public uint TocOffset { get; }
    public DbVersion Version { get; }
    public string Deadbeef { get; }

    public DbFileHeader(BinaryReader reader)
    {
        TocOffset = reader.ReadUInt32();
        Version = new DbVersion(reader);
        reader.ReadBytes(256);
        Deadbeef = BitConverter.ToString(reader.ReadBytes(4));
    }
}

public readonly struct DbToc
{
    public uint ItemCount { get; }
    public Dictionary<string, DbTocEntry> Items { get; }

    public DbToc(BinaryReader reader)
    {
        ItemCount = reader.ReadUInt32();
        Items = new Dictionary<string, DbTocEntry>();
        for (int i = 0; i < ItemCount; i++)
        {
            var entry = new DbTocEntry(reader);
            Items.Add(entry.Name, entry);
        }
    }
}

public readonly struct DbTocEntry
{
    public string Name { get; }
    public uint Offset { get; }
    public uint Size { get; }

    public DbTocEntry(BinaryReader reader)
    {
        Name = Encoding.UTF8.GetString(reader.ReadBytes(12)).Replace("\0", string.Empty);
        Offset = reader.ReadUInt32();
        Size = reader.ReadUInt32();
    }
}
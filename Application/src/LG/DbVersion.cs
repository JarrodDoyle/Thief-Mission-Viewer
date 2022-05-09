using System.Text;

namespace RlImGuiApp.LG;

public struct DbVersion
{
    public uint Major { get; }
    public uint Minor { get; }
    
    public DbVersion(BinaryReader reader)
    {
        Major = reader.ReadUInt32();
        Minor = reader.ReadUInt32();
    }

    public override string ToString()
    {
        return $"{Major}.{Minor}";
    }
}
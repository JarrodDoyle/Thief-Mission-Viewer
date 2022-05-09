using System.Numerics;
using System.Text;

namespace RlImGuiApp.LG;

public class WorldRep
{
    public DbChunk? Chunk { get; private set; }
    public WrHeader Header { get; }
    public WrCell[] Cells { get; }

    public WorldRep(DbFile dbFile)
    {
        // TODO: Raise errors when the chunk isn't valid
        if (!GetChunk(dbFile) || Chunk == null) return;

        // TODO: Lightmap scaling and stuff
        var stream = new MemoryStream(Chunk.Data);
        var reader = new BinaryReader(stream, Encoding.UTF8, false);
        bool extended = Chunk.Header.Version.Minor == 30;
        Header = new WrHeader(reader, extended);

        int lmFormat = 1;
        if (Chunk.Header.Name != "WR") lmFormat = 2;
        if (Chunk.Header.Name == "WREXT" && Header.LightmapFormat != 0) lmFormat = 4;

        Cells = new WrCell[Header.CellCount];
        for (int i = 0; i < Header.CellCount; i++)
            Cells[i] = new WrCell(reader, extended, lmFormat);
        reader.Dispose();
    }

    private bool GetChunk(DbFile dbFile)
    {
        Chunk = (dbFile.GetChunk("WR") ?? dbFile.GetChunk("WRRGB")) ?? dbFile.GetChunk("WREXT");
        if (Chunk == null) return false;

        // Check that the version is valid
        var chunkVersion = Chunk.Header.Version;
        if (chunkVersion.Major != 0) return false;
        switch (Chunk.Header.Name)
        {
            case "WR" when chunkVersion.Minor != 23:
            case "WRRGB" when chunkVersion.Minor != 24:
            case "WREXT" when chunkVersion.Minor != 30:
                return false;
        }

        return true;
    }
}

public struct WrHeader
{
    public uint LightmapFormat { get; }
    public int LightmapScale { get; }
    public uint DataSize { get; }
    public uint CellCount { get; }

    public WrHeader(BinaryReader reader, bool extended)
    {
        if (extended)
        {
            reader.ReadBytes(12);
            LightmapFormat = reader.ReadUInt32();
            LightmapScale = reader.ReadInt32();
        }
        else
        {
            LightmapFormat = 0;
            LightmapScale = 0;
        }

        if (LightmapScale == 0) LightmapScale = 1;
        DataSize = reader.ReadUInt32();
        CellCount = reader.ReadUInt32();
    }
}

public struct WrCellHeader
{
    public uint VertexCount { get; }
    public uint PolyCount { get; }
    public uint RenderPolyCount { get; }
    public uint PortalPolyCount { get; }
    public uint PlaneCount { get; }
    public uint Medium { get; }
    public uint Flags { get; }
    public int PortalVertexList { get; }
    public uint NumVList { get; }
    public uint AnimLightCount { get; }
    public uint MotionIndex { get; }
    public Vector3 SphereCenter { get; }
    public float SphereRadius { get; }

    public WrCellHeader(BinaryReader reader)
    {
        VertexCount = reader.ReadByte();
        PolyCount = reader.ReadByte();
        RenderPolyCount = reader.ReadByte();
        PortalPolyCount = reader.ReadByte();
        PlaneCount = reader.ReadByte();
        Medium = reader.ReadByte();
        Flags = reader.ReadByte();
        PortalVertexList = reader.ReadInt32();
        NumVList = reader.ReadUInt16();
        AnimLightCount = reader.ReadByte();
        MotionIndex = reader.ReadByte();
        SphereCenter = new Vector3 {X = reader.ReadSingle(), Y = reader.ReadSingle(), Z = reader.ReadSingle()};
        SphereRadius = reader.ReadSingle();
    }
}

public struct WrRenderPoly
{
    public Vector3 TexU { get; }
    public Vector3 TexV { get; }
    public float BaseU { get; }
    public float BaseV { get; }
    public uint TextureID { get; }
    public uint TextureAnchor { get; }
    public uint CachedSurface { get; }
    public float TextureMag { get; }
    public Vector3 Center { get; }

    public WrRenderPoly(BinaryReader reader, bool extended)
    {
        TexU = new Vector3 {X = reader.ReadSingle(), Y = reader.ReadSingle(), Z = reader.ReadSingle()};
        TexV = new Vector3 {X = reader.ReadSingle(), Y = reader.ReadSingle(), Z = reader.ReadSingle()};
        if (extended)
        {
            BaseU = reader.ReadSingle();
            BaseV = reader.ReadSingle();
            TextureID = reader.ReadUInt16();
            TextureAnchor = 0;
        }
        else
        {
            BaseU = reader.ReadUInt16();
            BaseV = reader.ReadUInt16();
            TextureID = reader.ReadByte();
            TextureAnchor = reader.ReadByte();
        }

        CachedSurface = reader.ReadUInt16();
        TextureMag = reader.ReadSingle();
        Center = new Vector3 {X = reader.ReadSingle(), Y = reader.ReadSingle(), Z = reader.ReadSingle()};
    }
}

public struct WrPlane
{
    public Vector3 Normal { get; }
    public float Distance { get; }

    public WrPlane(BinaryReader reader)
    {
        Normal = new Vector3 {X = reader.ReadSingle(), Y = reader.ReadSingle(), Z = reader.ReadSingle()};
        Distance = reader.ReadSingle();
    }
}

public struct WrLightMapInfo
{
    public int BaseU { get; }
    public int BaseV { get; }
    public int PaddedWidth { get; }
    public uint Height { get; }
    public uint Width { get; }
    public uint DataPtr { get; }
    public uint DynamicLightPtr { get; }
    public uint AnimLightBitmask { get; }

    public WrLightMapInfo(BinaryReader reader)
    {
        BaseU = reader.ReadInt16();
        BaseV = reader.ReadInt16();
        PaddedWidth = reader.ReadInt16();
        Height = reader.ReadByte();
        Width = reader.ReadByte();
        DataPtr = reader.ReadUInt32();
        DynamicLightPtr = reader.ReadUInt32();
        AnimLightBitmask = reader.ReadUInt32();
    }
}

public struct WrPoly
{
    public uint Flags { get; }
    public uint VertexCount { get; }
    public uint PlaneId { get; }
    public uint ClutId { get; }
    public uint Destination { get; }
    public uint MotionIndex { get; }
    public uint Padding { get; }

    public WrPoly(BinaryReader reader)
    {
        Flags = reader.ReadByte();
        VertexCount = reader.ReadByte();
        PlaneId = reader.ReadByte();
        ClutId = reader.ReadByte();
        Destination = reader.ReadUInt16();
        MotionIndex = reader.ReadByte();
        Padding = reader.ReadByte();
    }
}

public struct WrCell
{
    public WrCellHeader Header { get; }
    public Vector3[] PVertices { get; }
    public WrPoly[] PPolys { get; }
    public WrRenderPoly[] PRenderPolys { get; }
    public uint IndexCount { get; }
    public uint[] PIndexList { get; }
    public WrPlane[] PPlaneList { get; }
    public uint[] PAnimLights { get; }
    public WrLightMapInfo[] PLightList { get; }
    public int LightIndicesCount { get; }
    public uint[] PLightIndices { get; }
    public List<Vector4[,,]> Lightmaps { get; }
    public List<int> Triangles { get; }

    public WrCell(BinaryReader reader, bool extended, int lightmapFormat)
    {
        Header = new WrCellHeader(reader);
        PVertices = new Vector3[Header.VertexCount];
        for (int i = 0; i < Header.VertexCount; i++)
            PVertices[i] = new Vector3 {X = -reader.ReadSingle(), Z = reader.ReadSingle(), Y = reader.ReadSingle()};
        PPolys = new WrPoly[Header.PolyCount];
        for (int i = 0; i < Header.PolyCount; i++)
            PPolys[i] = new WrPoly(reader);
        PRenderPolys = new WrRenderPoly[Header.RenderPolyCount];
        for (int i = 0; i < Header.RenderPolyCount; i++)
            PRenderPolys[i] = new WrRenderPoly(reader, extended);
        IndexCount = reader.ReadUInt32();
        PIndexList = new uint[IndexCount];
        for (int i = 0; i < IndexCount; i++)
            PIndexList[i] = reader.ReadByte();
        PPlaneList = new WrPlane[Header.PlaneCount];
        for (int i = 0; i < Header.PlaneCount; i++)
            PPlaneList[i] = new WrPlane(reader);
        PAnimLights = new uint[Header.AnimLightCount];
        for (int i = 0; i < Header.AnimLightCount; i++)
            PAnimLights[i] = reader.ReadUInt16();
        PLightList = new WrLightMapInfo[Header.RenderPolyCount];
        for (int i = 0; i < Header.RenderPolyCount; i++)
            PLightList[i] = new WrLightMapInfo(reader);

        Lightmaps = new List<Vector4[,,]>();
        for (int i = 0; i < Header.RenderPolyCount; i++)
        {
            // TODO: Actually handle lightmaps instead of leaving them as raw values
            var info = PLightList[i];
            var count = 1; // 1 base lightmap plus 1 per animlight
            var n = info.AnimLightBitmask;
            while (n != 0)
            {
                if ((n & 1) == 1) count++;
                n >>= 1;
            }

            Vector4[,,] lightmap = new Vector4[count, info.Height, info.Width];
            // var raw = reader.ReadBytes(count * (int) info.Width * (int) info.Height * lightmapFormat);
            for (int c = 0; c < count; c++)
            for (int y = 0; y < info.Height; y++)
            for (int x = 0; x < info.Width; x++)
            {
                // TODO: rgb and rgba shift orders might be reversed
                Vector4 lm = default;
                uint raw = 0;
                switch (lightmapFormat)
                {
                    case 1:
                        raw = reader.ReadByte();
                        lm = new Vector4(raw, raw, raw, 255) / 255;
                        break;
                    case 2:
                        raw = BitConverter.ToUInt16(reader.ReadBytes(2));
                        lm = new Vector4(raw & 31, (raw >> 5) & 31, (raw >> 10) & 31, 32f) / 32f;
                        break;
                    case 4:
                        raw = BitConverter.ToUInt32(reader.ReadBytes(4));
                        lm = new Vector4(raw & 255, (raw >> 8) & 255, (raw >> 16) & 255, (raw >> 24) & 255) / 255f;
                        break;
                }

                lightmap[c, y, x] = lm;
            }

            Lightmaps.Add(lightmap);
        }

        LightIndicesCount = reader.ReadInt32();
        PLightIndices = new uint[LightIndicesCount];
        for (int i = 0; i < LightIndicesCount; i++)
            PLightIndices[i] = reader.ReadUInt16();

        // Precalc visible poly triangles
        var numPolys = Header.PolyCount;
        var numRenderPolys = Header.RenderPolyCount;
        var numPortalPolys = Header.PortalPolyCount;
        var idxOffset = 0;
        Triangles = new List<int>();
        for (int i = 0; i < numPolys; i++)
        {
            var poly = PPolys[i];
            if (i >= numRenderPolys || i >= numPolys - numPortalPolys)
            {
                idxOffset += (int) poly.VertexCount;
                continue;
            }

            var numPolyVertices = poly.VertexCount;
            for (int k = 1; k < numPolyVertices - 1; k++)
            {
                Triangles.Add((int) PIndexList[idxOffset]);
                Triangles.Add((int) PIndexList[idxOffset + k]);
                Triangles.Add((int) PIndexList[idxOffset + k + 1]);
            }

            idxOffset += (int) poly.VertexCount;
        }
    }
}
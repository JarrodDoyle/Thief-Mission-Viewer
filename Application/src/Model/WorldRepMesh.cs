using System.Numerics;
using Raylib_cs;
using RectpackSharp;

namespace RlImGuiApp.Model;

public class WorldRepMesh
{
    private Raylib_cs.Model[] Models { get; }
    private RenderTexture2D[] LmTextures { get; }

    public WorldRepMesh(LG.WorldRep worldRep)
    {
        // TODO: Lightmaps have a lot of blank space
        // TODO: Apply lightmaps to the model
        var models = new List<Raylib_cs.Model>();
        var lmTextures = new List<RenderTexture2D>();
        var numCells = worldRep.Cells.Length;
        for (int i = 0; i < numCells; i++)
        {
            // If the cell has no visible tris we ignore it
            var cell = worldRep.Cells[i];
            if (cell.Triangles.Count == 0) continue;

            BuildCellVertices(cell, out var vertices);
            BuildCellIndices(cell, out var indices);
            BuildCellVertexColours(cell, out var colours);
            BuildMesh(vertices, indices, colours, out var mesh);

            var numRects = cell.Header.RenderPolyCount;
            var packingRects = new PackingRectangle[numRects];
            for (int j = 0; j < numRects; j++)
            {
                var info = cell.PLightList[j];
                packingRects[j] = new PackingRectangle(0, 0, info.Width, info.Height);
            }

            RectanglePacker.Pack(packingRects, out var bounds);
            var lmRenderTexture = Raylib.LoadRenderTexture((int) bounds.Width, (int) bounds.Height);
            Raylib.BeginTextureMode(lmRenderTexture);
            Raylib.ClearBackground(Color.PURPLE);
            for (var j = 0; j < numRects; j++)
            {
                var rect = packingRects[j];
                var lightmap = cell.Lightmaps[j];
                for (var y = 0; y < lightmap.GetLength(1); y++)
                for (var x = 0; x < lightmap.GetLength(2); x++)
                {
                    var c = lightmap[0, y, x];
                    Raylib.DrawPixel((int) rect.X + x, (int) rect.Y + y,
                        new Color((int) c.X, (int) c.Y, (int) c.Z, (int) c.W));
                }
            }

            Raylib.EndTextureMode();
            lmTextures.Add(lmRenderTexture);

            Raylib.UploadMesh(ref mesh, false);
            var model = Raylib.LoadModelFromMesh(mesh);
            // Raylib.SetMaterialTexture(ref model, 0, MaterialMapIndex.MATERIAL_MAP_DIFFUSE, ref lmRenderTexture.texture);
            models.Add(model);
        }

        Models = models.ToArray();
        LmTextures = lmTextures.ToArray();
    }

    public void Render()
    {
        foreach (var t in Models)
        {
            Raylib.DrawModel(t, Vector3.Zero, 1, Color.WHITE);
            Raylib.DrawModelWires(t, Vector3.Zero, 1, Color.BLACK);
        }
    }

    public void ExportLightmaps(string dirPath)
    {
        for (var i = 0; i < LmTextures.Length; i++)
        {
            var filePath = $"{dirPath}/Outputs/lm-{i}.png";
            Raylib.ExportImage(Raylib.LoadImageFromTexture(LmTextures[i].texture), filePath);
        }
    }

    private static void BuildMesh(float[] vertices, ushort[] indices, byte[] colours, out Mesh mesh)
    {
        mesh = new Mesh {vertexCount = vertices.Length / 3, triangleCount = indices.Length / 3};
        unsafe
        {
            fixed (float* verticesPtr = vertices)
                mesh.vertices = verticesPtr;
            fixed (byte* colorsPtr = colours)
                mesh.colors = colorsPtr;
            fixed (ushort* indicesPtr = indices)
                mesh.indices = indicesPtr;
        }
    }

    private static void BuildCellVertices(LG.WrCell cell, out float[] vertices)
    {
        // Read geometry vertices and convert to float arr
        var numVertices = cell.PVertices.Length;
        vertices = new float[numVertices * 3];
        for (var i = 0; i < numVertices; i++)
        {
            var v = cell.PVertices[i];
            var idx = i * 3;
            // Reverse winding-order
            vertices[idx + 2] = v.X;
            vertices[idx + 1] = v.Y;
            vertices[idx + 0] = v.Z;
        }
    }

    private static void BuildCellIndices(LG.WrCell cell, out ushort[] indices)
    {
        // Read geometry triangles
        var tris = cell.Triangles;
        indices = new ushort[tris.Count];
        for (var i = 0; i < tris.Count; i++)
            indices[i] = (ushort) tris[i];
    }

    private static void BuildCellVertexColours(LG.WrCell cell, out byte[] colours)
    {
        // Build some debug vertex colours
        var rnd = new Random();
        var numVertices = cell.PVertices.Length;
        colours = new byte[numVertices * 4];
        // for (var i = 0; i < numVertices * 4; i++)
        //     colours[i] = 255;
        for (var i = 0; i < numVertices; i++)
        {
            var idx = i * 4;
            colours[idx] = (byte) rnd.Next(100, 255);
            colours[idx + 1] = (byte) rnd.Next(100, 255);
            colours[idx + 2] = (byte) rnd.Next(100, 255);
            colours[idx + 3] = 255;
        }
    }
}
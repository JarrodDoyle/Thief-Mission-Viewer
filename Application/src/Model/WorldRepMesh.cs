using System.Numerics;
using Raylib_cs;

namespace RlImGuiApp.Model;

public class WorldRepMesh
{
    private Raylib_cs.Model[] Models { get; }

    public WorldRepMesh(LG.WorldRep worldRep)
    {
        var models = new List<Raylib_cs.Model>();
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

            Raylib.UploadMesh(ref mesh, false);
            var model = Raylib.LoadModelFromMesh(mesh);
            models.Add(model);
        }

        Models = models.ToArray();
    }

    public void Render()
    {
        foreach (var t in Models)
            Raylib.DrawModel(t, Vector3.Zero, 1, Color.WHITE);
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
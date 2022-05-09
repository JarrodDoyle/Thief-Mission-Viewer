using System.Numerics;

namespace RlImGuiApp.Model;

public class WorldRepMesh
{
    private Raylib_cs.Model[] Models { get; }

    public WorldRepMesh(LG.WorldRep worldRep)
    {
        var rnd = new Random();
        var models = new List<Raylib_cs.Model>();
        var numCells = worldRep.Cells.Length;
        for (int i = 0; i < numCells; i++)
        {
            // If the cell has no visible tris we ignore it
            var cell = worldRep.Cells[i];
            if (cell.Triangles.Count == 0) continue;

            var mesh = new Raylib_cs.Mesh();
            var numVertices = cell.PVertices.Length;
            mesh.vertexCount = numVertices;

            // Read geometry vertices and convert to float arr
            var vertices = new float[numVertices * 3];
            for (int j = 0; j < numVertices; j++)
            {
                var v = cell.PVertices[j];
                var idx = j * 3;
                // Reverse winding-order
                vertices[idx + 2] = v.X;
                vertices[idx + 1] = v.Y;
                vertices[idx + 0] = v.Z;
            }

            var colors = new byte[numVertices * 4];
            for (int j = 0; j < numVertices; j++)
            {
                var c = rnd.Next(100, 255);
                var idx = j * 4;
                colors[idx] = (byte) rnd.Next(100, 255);
                colors[idx + 1] = (byte) rnd.Next(100, 255);
                colors[idx + 2] = (byte) rnd.Next(100, 255);
                colors[idx + 3] = 255;
            }

            // Read geometry triangles
            var tris = cell.Triangles;
            mesh.triangleCount = tris.Count / 3;
            var indices = new ushort[tris.Count];
            for (int j = 0; j < tris.Count; j++)
                indices[j] = (ushort) tris[j];

            // Set the data on the mesh
            unsafe
            {
                fixed (float* verticesPtr = vertices)
                    mesh.vertices = verticesPtr;
                fixed (byte* colorsPtr = colors)
                    mesh.colors = colorsPtr;
                fixed (ushort* indicesPtr = indices)
                    mesh.indices = indicesPtr;
            }

            Raylib_cs.Raylib.UploadMesh(ref mesh, false);
            var model = Raylib_cs.Raylib.LoadModelFromMesh(mesh);
            models.Add(model);
        }

        Models = models.ToArray();
    }

    public void Render()
    {
        foreach (var t in Models)
            Raylib_cs.Raylib.DrawModel(t, Vector3.Zero, 1, Raylib_cs.Color.WHITE);
    }
}
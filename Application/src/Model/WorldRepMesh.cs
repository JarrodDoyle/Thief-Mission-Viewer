using System.Numerics;

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
            var cell = worldRep.Cells[i];
            if (cell.Triangles.Count == 0) continue;

            var mesh = new Raylib_cs.Mesh();
            var numVertices = cell.PVertices.Length;
            mesh.vertexCount = numVertices;
            
            var vertices = new float[numVertices * 3];
            for (int j = 0; j < numVertices; j++)
            {
                var v = cell.PVertices[j];
                var idx = j * 3;
                vertices[idx + 0] = v.X;
                vertices[idx + 1] = v.Y;
                vertices[idx + 2] = v.Z;
            }
            
            var tris = cell.Triangles;
            mesh.triangleCount = tris.Count / 3;
            var indices = new ushort[tris.Count];
            for (int j = 0; j < tris.Count; j++)
                indices[j] = (ushort) tris[j];

            var colors = new byte[numVertices * 4];
            for (int j = 0; j < numVertices * 4; j++)
                colors[j] = 255;
            
            unsafe
            {
                fixed (float* verticesPtr = vertices)
                    mesh.vertices = verticesPtr;
                fixed (byte* colorsPtr = colors)
                    mesh.colors = colorsPtr;
                fixed (ushort* indicesPtr = indices)
                    mesh.indices = indicesPtr;
            }

            // mesh = Raylib_cs.Raylib.GenMeshCube(5, 5, 5);
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
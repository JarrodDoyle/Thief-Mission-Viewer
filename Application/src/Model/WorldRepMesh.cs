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
            BuildLightmap(cell, out var lmRenderTexture, out var packingRects, out var bounds);
            BuildUvs(cell, packingRects, bounds, out var uvs);
            BuildCellVertexColours(cell, out var colours);
            BuildMesh(vertices, indices, colours, uvs, out var mesh);

            lmTextures.Add(lmRenderTexture);

            Raylib.UploadMesh(ref mesh, false);
            var model = Raylib.LoadModelFromMesh(mesh);
            Raylib.SetMaterialTexture(ref model, 0, MaterialMapIndex.MATERIAL_MAP_DIFFUSE, ref lmRenderTexture.texture);
            models.Add(model);
        }

        Models = models.ToArray();
        LmTextures = lmTextures.ToArray();
    }

    private static void BuildMesh(float[] vertices, ushort[] indices, byte[] colours, float[] uvs, out Mesh mesh)
    {
        mesh = new Mesh {vertexCount = vertices.Length / 3, triangleCount = indices.Length / 3};
        unsafe
        {
            fixed (float* verticesPtr = vertices) mesh.vertices = verticesPtr;
            fixed (byte* colorsPtr = colours) mesh.colors = colorsPtr;
            fixed (ushort* indicesPtr = indices) mesh.indices = indicesPtr;
            fixed (float* uvsPtr = uvs) mesh.texcoords = uvsPtr;
        }
    }

    private static void BuildUvs(LG.WrCell cell, PackingRectangle[] rects, PackingRectangle bounds, out float[] lmUvs)
    {
        // TODO: UVs are wrong bruhh
        var numRenderPolys = cell.Header.RenderPolyCount;
        var portalStartIdx = cell.Header.PolyCount - cell.Header.PortalPolyCount;
        
        var lmUvList = new List<Vector2>();
        var idToIdxMap = new List<int[]>();
        var idxOffset = 0;
        for (var i = 0; i < numRenderPolys; i++)
        {
            var poly = cell.PPolys[i];
            if (i >= portalStartIdx)
            {
                idxOffset += (int) poly.VertexCount;
                continue;
            }

            var renderPoly = cell.PRenderPolys[i];
            var light = cell.PLightList[i];
            var uu = Vector3.Dot(renderPoly.TexU, renderPoly.TexU);
            var vv = Vector3.Dot(renderPoly.TexV, renderPoly.TexV);
            var uv = Vector3.Dot(renderPoly.TexU, renderPoly.TexV);
            var lmUScale = 4.0f / light.Width;
            var lmVScale = 4.0f / light.Height;

            // TODO: Support newdark (requires some importer changes)
            var renderUBase = renderPoly.BaseU / (16 * 256);
            var renderVBase = renderPoly.BaseV / (16 * 256);
            var lmUBase = lmUScale * (renderUBase + (0.5f - light.BaseU) / 4);
            var lmVBase = lmVScale * (renderVBase + (0.5f - light.BaseV) / 4);
            var anchor = cell.PVertices[cell.PIndexList[idxOffset + renderPoly.TextureAnchor]];

            var numVertices = (int) poly.VertexCount;
            var uvIdx = new int[numVertices];
            // TODO: Might need to reverse uv (1 - uv)
            if (uv == 0.0)
            {
                var lmUVec = renderPoly.TexU * lmUScale / uu;
                var lmVVec = renderPoly.TexV * lmVScale / vv;

                for (var j = numVertices - 1; j >= 0; j--)
                {
                    var delta = cell.PVertices[cell.PIndexList[idxOffset + j]] - anchor;
                    var lmU = Vector3.Dot(delta, lmUVec) + lmUBase;
                    var lmV = Vector3.Dot(delta, lmVVec) + lmVBase;
                    uvIdx[j] = lmUvList.Count;
                    lmUvList.Add(new Vector2(lmU, lmV));
                }
            }
            else
            {
                var denom = 1 / (uu * vv - uv * uv);
                var lmUu = uu * lmVScale * denom;
                var lmVv = vv * lmUScale * denom;
                var lmUvu = lmUScale * denom * uv;
                var lmUvv = lmVScale * denom * uv;

                for (var j = numVertices - 1; j >= 0; j--)
                {
                    var delta = cell.PVertices[cell.PIndexList[idxOffset + j]] - anchor;
                    var du = Vector3.Dot(delta, renderPoly.TexU);
                    var dv = Vector3.Dot(delta, renderPoly.TexV);
                    var lmU = lmUBase + lmVv * du - lmUvu * dv;
                    var lmV = lmVBase + lmUu * dv - lmUvv * du;
                    uvIdx[j] = lmUvList.Count;
                    lmUvList.Add(new Vector2(lmU, lmV));
                }
            }

            idToIdxMap.Add(uvIdx);
            idxOffset += (int) poly.VertexCount;
        }

        // Transform UVs to lightmap texture space
        var lmUvVecs = lmUvList.ToArray();
        TransformUvs(rects, bounds, idToIdxMap.ToArray(), portalStartIdx, ref lmUvVecs);

        // Output the UVs as a float array
        lmUvs = new float[lmUvList.Count * 2];
        var idx = 0;
        foreach (var uv in lmUvVecs)
        {
            lmUvs[idx] = uv.X;
            lmUvs[idx + 1] = uv.Y;
            idx += 2;
        }
    }

    private static void TransformUvs(PackingRectangle[] rects, PackingRectangle bounds, int[][] uvIdxs,
        uint portalStartIdx, ref Vector2[] uvs)
    {
        foreach (var rect in rects)
        {
            if (rect.Id >= portalStartIdx) continue;

            var lmUvIdxs = uvIdxs[rect.Id];
            foreach (var idx in lmUvIdxs)
            {
                var lmUv = uvs[idx];

                // Clamp uv range to [0..1]
                lmUv.X %= 1;
                lmUv.Y %= 1;
                if (lmUv.X < 0) lmUv.X = Math.Abs(lmUv.X);
                if (lmUv.Y < 0) lmUv.Y = Math.Abs(lmUv.Y);

                // Transform!
                lmUv.X = (rect.X + rect.Width * lmUv.X) / (int) bounds.Width;
                lmUv.Y = (rect.Y + rect.Height * lmUv.Y) / (int) bounds.Height;
                uvs[idx] = lmUv;
            }
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
            vertices[idx + 0] = v.X;
            vertices[idx + 1] = v.Y;
            vertices[idx + 2] = v.Z;
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
        var numVertices = cell.PVertices.Length;
        colours = new byte[numVertices * 4];
        for (var i = 0; i < numVertices * 4; i++)
            colours[i] = 255;
    }

    private static void BuildLightmap(LG.WrCell cell, out RenderTexture2D texture, out PackingRectangle[] rects,
        out PackingRectangle bounds)
    {
        var numRects = cell.Header.RenderPolyCount;
        rects = new PackingRectangle[numRects];
        for (var i = 0; i < numRects; i++)
        {
            var light = cell.PLightList[i];
            rects[i] = new PackingRectangle(0, 0, light.Width, light.Height, i);
        }

        RectanglePacker.Pack(rects, out bounds);
        texture = Raylib.LoadRenderTexture((int) bounds.Width, (int) bounds.Height);
        Raylib.BeginTextureMode(texture);
        Raylib.ClearBackground(Color.PURPLE);
        foreach (var rect in rects)
        {
            var lightmap = cell.Lightmaps[rect.Id];
            for (var y = 0; y < lightmap.GetLength(1); y++)
            for (var x = 0; x < lightmap.GetLength(2); x++)
            {
                var values = lightmap[0, y, x];
                var colour = new Color((int) values.X, (int) values.Y, (int) values.Z, (int) values.W);
                Raylib.DrawPixel((int) rect.X + x, (int) rect.Y + y, colour);
            }
        }

        Raylib.EndTextureMode();
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
}
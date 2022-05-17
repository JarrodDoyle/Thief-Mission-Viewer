using System.Numerics;
using Raylib_cs;
using RectpackSharp;

namespace RlImGuiApp.Model;

public class WorldRepMesh
{
    private Raylib_cs.Model[] Models { get; }
    private BoundingBox[] BoundingBoxes { get; }
    private RenderTexture2D[] LmTextures { get; }
    private int ActiveBBox { get; set; }

    public WorldRepMesh(LG.WorldRep worldRep)
    {
        ActiveBBox = -1;
        // TODO: Lightmap UVs are still incorrect
        var models = new List<Raylib_cs.Model>();
        var boundingBoxes = new List<BoundingBox>();
        var lmTextures = new List<RenderTexture2D>();
        var numCells = worldRep.Cells.Length;
        for (int i = 0; i < numCells; i++)
        {
            // If the cell has no visible tris we ignore it
            var cell = worldRep.Cells[i];
            if (cell.Triangles.Count == 0) continue;

            // Iterate over rendered polys and build their vertices, indices, and uvs
            var vertexList = new List<float>();
            var indexList = new List<ushort>();
            var lmUvList = new List<float>();
            var rectIdToUvIdxMap = new List<int[]>();

            var numRenderPolys = cell.Header.RenderPolyCount;
            var portalStartIdx = cell.Header.PolyCount - cell.Header.PortalPolyCount;
            var vertCount = 0;
            var offset = 0;
            for (var j = 0; j < numRenderPolys; j++)
            {
                if (j >= portalStartIdx) break;

                var poly = cell.PPolys[j];
                var polyVertCount = (int) poly.VertexCount;

                // Build indices using the vertex map
                BuildPolyVertexMap(cell, vertexList, polyVertCount, offset, ref vertCount, out var polyVertexMap);
                BuildPolyIndices(polyVertCount, indexList, polyVertexMap, cell, offset);

                // Build uvs
                var renderPoly = cell.PRenderPolys[j];
                var light = cell.PLightList[j];
                CalcBaseUvs(renderPoly, light, cell, offset, polyVertCount, lmUvList, out var uvIdx);
                rectIdToUvIdxMap.Add(uvIdx);

                offset += polyVertCount;
            }

            // Build the mesh
            var lmUvArr = lmUvList.ToArray();
            BuildLightmap(cell, out var lmRenderTexture, out var packingRects, out var bounds);
            TransformUvs(packingRects, bounds, rectIdToUvIdxMap.ToArray(), portalStartIdx, ref lmUvArr);
            BuildMesh(vertexList, indexList, lmUvArr, out var mesh);

            Raylib.UploadMesh(ref mesh, false);
            var model = Raylib.LoadModelFromMesh(mesh);
            Raylib.SetMaterialTexture(ref model, 0, MaterialMapIndex.MATERIAL_MAP_DIFFUSE, ref lmRenderTexture.texture);
            models.Add(model);
            boundingBoxes.Add(Raylib.GetModelBoundingBox(model));
            lmTextures.Add(lmRenderTexture);
        }

        Models = models.ToArray();
        BoundingBoxes = boundingBoxes.ToArray();
        LmTextures = lmTextures.ToArray();
    }

    private static void BuildPolyIndices(int polyVertCount, List<ushort> indexList, Dictionary<uint, int> polyVertexMap,
        LG.WrCell cell, int idxOffset)
    {
        for (var k = 1; k < polyVertCount - 1; k++)
        {
            indexList.Add((ushort) polyVertexMap[cell.PIndexList[idxOffset + k + 1]]);
            indexList.Add((ushort) polyVertexMap[cell.PIndexList[idxOffset + k]]);
            indexList.Add((ushort) polyVertexMap[cell.PIndexList[idxOffset]]);
        }
    }

    private static void CalcBaseUvs(LG.WrRenderPoly renderPoly, LG.WrLightMapInfo light, LG.WrCell cell, int idxOffset,
        int polyVertCount, List<float> lmUvList, out int[] uvIdx)
    {
        var uu = Vector3.Dot(renderPoly.TexU, renderPoly.TexU);
        var vv = Vector3.Dot(renderPoly.TexV, renderPoly.TexV);
        var uv = Vector3.Dot(renderPoly.TexU, renderPoly.TexV);
        var lmUScale = 4.0f / light.Width;
        var lmVScale = 4.0f / light.Height;

        // TODO: Support newdark (requires some importer changes)
        var renderUBase = renderPoly.BaseU / 4096.0f;
        var renderVBase = renderPoly.BaseV / 4096.0f;
        var lmUBase = lmUScale * (renderUBase + (0.5f - light.BaseU) / 4.0f);
        var lmVBase = lmVScale * (renderVBase + (0.5f - light.BaseV) / 4.0f);
        var anchor = cell.PVertices[cell.PIndexList[idxOffset + renderPoly.TextureAnchor]];
        uvIdx = new int[polyVertCount];
        if (uv == 0.0)
        {
            var lmUVec = renderPoly.TexU * lmUScale / uu;
            var lmVVec = renderPoly.TexV * lmVScale / vv;
            for (var i = 0; i < polyVertCount; i++)
            {
                uvIdx[i] = lmUvList.Count / 2;

                var delta = cell.PVertices[cell.PIndexList[idxOffset + i]] - anchor;
                lmUvList.Add(Vector3.Dot(delta, lmUVec) + lmUBase);
                lmUvList.Add(Vector3.Dot(delta, lmVVec) + lmVBase);
            }
        }
        else
        {
            var denom = 1.0f / (uu * vv - uv * uv);
            var lmUu = uu * lmVScale * denom;
            var lmVv = vv * lmUScale * denom;
            var lmUvu = lmUScale * denom * uv;
            var lmUvv = lmVScale * denom * uv;
            for (var i = 0; i < polyVertCount; i++)
            {
                uvIdx[i] = lmUvList.Count / 2;

                var delta = cell.PVertices[cell.PIndexList[idxOffset + i]] - anchor;
                var du = Vector3.Dot(delta, renderPoly.TexU);
                var dv = Vector3.Dot(delta, renderPoly.TexV);
                lmUvList.Add(lmUBase + lmVv * du - lmUvu * dv);
                lmUvList.Add(lmVBase + lmUu * dv - lmUvv * du);
            }
        }
    }

    private static void BuildPolyVertexMap(LG.WrCell cell, List<float> vertexList,
        int polyVertCount, int idxOffset, ref int vertCount,
        out Dictionary<uint, int> polyVertexMap)
    {
        polyVertexMap = new Dictionary<uint, int>();
        for (var k = 0; k < polyVertCount; k++)
        {
            var idx = cell.PIndexList[idxOffset + k];
            if (!polyVertexMap.TryAdd(idx, vertCount)) continue;

            var v = cell.PVertices[idx];
            vertexList.Add(v.X);
            vertexList.Add(v.Y);
            vertexList.Add(v.Z);
            vertCount++;
        }
    }

    private static void BuildMesh(List<float> vertices, List<ushort> indices, float[] uvs, out Mesh mesh)
    {
        mesh = new Mesh {vertexCount = vertices.Count / 3, triangleCount = indices.Count / 3};
        unsafe
        {
            fixed (float* verticesPtr = vertices.ToArray()) mesh.vertices = verticesPtr;
            fixed (ushort* indicesPtr = indices.ToArray()) mesh.indices = indicesPtr;
            fixed (float* uvsPtr = uvs) mesh.texcoords = uvsPtr;
        }
    }

    private static void TransformUvs(PackingRectangle[] rects, PackingRectangle bounds, int[][] uvIdxs,
        uint portalStartIdx, ref float[] uvs)
    {
        foreach (var rect in rects)
        {
            if (rect.Id >= portalStartIdx) continue;

            var lmUvIdxs = uvIdxs[rect.Id];
            foreach (var idx in lmUvIdxs)
            {
                var u = uvs[2 * idx];
                var v = uvs[2 * idx + 1];

                // Clamp uv range to [0..1]
                u %= 1;
                v %= 1;
                if (u < 0) u = Math.Abs(u);
                if (v < 0) v = Math.Abs(v);

                // Transform!
                u = (rect.X + rect.Width * u) / (int) bounds.Width;
                v = (rect.Y + rect.Height * v) / (int) bounds.Height;
                uvs[2 * idx] = u;
                uvs[2 * idx + 1] = 1 - v;
            }
        }
    }

    private static void BuildLightmap(LG.WrCell cell, out RenderTexture2D texture, out PackingRectangle[] rects,
        out PackingRectangle bounds)
    {
        var numRects = Math.Min(cell.Header.RenderPolyCount, cell.Header.PolyCount - cell.Header.PortalPolyCount);
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
        var frustum = new Frustum();
        var numModels = Models.Length;
        for (var i = 0; i < numModels; i++)
        {
            var model = Models[i];
            var bb = BoundingBoxes[i];

            // Frustum culling
            if (!frustum.AabbInside(bb)) continue;

            Raylib.DrawModel(model, Vector3.Zero, 1, Color.WHITE);
            // Raylib.DrawModelWires(model, Vector3.Zero, 1, Color.BLACK);
            
            if (i == ActiveBBox) Raylib.DrawBoundingBox(bb, Color.GOLD);
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

    public void SelectCell(Ray ray)
    {
        // TODO: Ignore backfaces during selection
        // TODO: Fix bug where it just suddenly stops working??
        var numModels = Models.Length;
        var minDistance = float.MaxValue;
        for (var i = 0; i < numModels; i++)
        {
            var bbResult = Raylib.GetRayCollisionBox(ray, BoundingBoxes[i]);
            if (!bbResult.hit) continue;
            
            var mdResult = Raylib.GetRayCollisionModel(ray, Models[i]);
            if (!mdResult.hit || mdResult.distance >= minDistance) continue;
            // if (Vector3.Dot(mdResult.normal, ray.direction) >= 0) continue;

            ActiveBBox = i;
            minDistance = mdResult.distance;
        }
    }
}
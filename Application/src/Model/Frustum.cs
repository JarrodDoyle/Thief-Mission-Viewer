using System.Numerics;
using Raylib_cs;

namespace RlImGuiApp.Model;

public class Frustum
{
    private readonly Plane[] _planes;

    public Frustum()
    {
        var mat = Rlgl.rlGetMatrixProjection() * Rlgl.rlGetMatrixModelview();
        var right = new Vector4(mat.M41 - mat.M11, mat.M42 - mat.M12, mat.M43 - mat.M13, mat.M44 - mat.M14);
        var left = new Vector4(mat.M41 + mat.M11, mat.M42 + mat.M12, mat.M43 + mat.M13, mat.M44 + mat.M14);
        var top = new Vector4(mat.M41 - mat.M21, mat.M42 - mat.M22, mat.M43 - mat.M23, mat.M44 - mat.M24);
        var bottom = new Vector4(mat.M41 + mat.M21, mat.M42 + mat.M22, mat.M43 + mat.M23, mat.M44 + mat.M24);
        var front = new Vector4(mat.M41 - mat.M31, mat.M42 - mat.M32, mat.M43 - mat.M33, mat.M44 - mat.M34);
        var back = new Vector4(mat.M41 + mat.M31, mat.M42 + mat.M32, mat.M43 + mat.M33, mat.M44 + mat.M34);

        _planes = new Plane[6];
        _planes[0] = Plane.Normalize(new Plane(back));
        _planes[1] = Plane.Normalize(new Plane(front));
        _planes[2] = Plane.Normalize(new Plane(bottom));
        _planes[3] = Plane.Normalize(new Plane(top));
        _planes[4] = Plane.Normalize(new Plane(right));
        _planes[5] = Plane.Normalize(new Plane(left));
    }

    public bool AabbInside(BoundingBox aabb)
    {
        if (PointInside(new Vector3(aabb.min.X, aabb.min.Y, aabb.min.Z))) return true;
        if (PointInside(new Vector3(aabb.min.X, aabb.min.Y, aabb.max.Z))) return true;
        if (PointInside(new Vector3(aabb.min.X, aabb.max.Y, aabb.min.Z))) return true;
        if (PointInside(new Vector3(aabb.min.X, aabb.max.Y, aabb.max.Z))) return true;
        if (PointInside(new Vector3(aabb.max.X, aabb.min.Y, aabb.min.Z))) return true;
        if (PointInside(new Vector3(aabb.max.X, aabb.min.Y, aabb.max.Z))) return true;
        if (PointInside(new Vector3(aabb.max.X, aabb.max.Y, aabb.min.Z))) return true;
        if (PointInside(new Vector3(aabb.max.X, aabb.max.Y, aabb.max.Z))) return true;

        return false;
    }

    private bool PointInside(Vector3 point)
    {
        foreach (var plane in _planes)
            if (Vector3.Dot(plane.Normal, point) + plane.D <= 0)
                return false;

        return true;
    }
}
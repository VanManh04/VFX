using System.Runtime.CompilerServices;
using UnityEngine;

public static partial class VAT_Utilities
{
    #region <====================| CylinderWithEllipticalFaces |====================>

    public static Vector3 GetClosestPointOnRay(Ray ray, Vector3 point)
    {
        // Vector từ điểm đầu của ray đến point
        Vector3 pointToRay = point - ray.origin;
    
        // Tính độ dài của projection
        float dot = Vector3.Dot(pointToRay, ray.direction);

        return dot < 0
            // Nếu dot < 0, điểm nằm phía sau ray
            // trong trường hợp này lấy điểm gốc của ray
            ? ray.origin
                
            // Tính điểm chiếu
            : ray.origin + ray.direction * dot;
    }
    
    public static bool CheckSphereBoxCollision(Vector3 center, float radius, Vector3 boxCenter, Vector3 boxSize, out float distanceSq)
    {
        Vector3 closestPoint = ClampVector(center, boxCenter - boxSize, boxCenter + boxSize);
        distanceSq = Vector3.SqrMagnitude(center - closestPoint);
        return distanceSq <= (radius * radius);
    }

    public static Vector3 ClampVector(Vector3 value, Vector3 min, Vector3 max)
    {
        return new Vector3(
                Mathf.Clamp(value.x, min.x, max.x),
                Mathf.Clamp(value.y, min.y, max.y),
                Mathf.Clamp(value.z, min.z, max.z)
            );
    }
    

    public static bool IntersectRayBox(Ray ray, BoxCollider box, out float distance, out Vector3 rayPoint, out Vector3 rayNormal)
    {
        distance  = float.MinValue;
        rayPoint  = Vector3.zero;
        rayNormal = Vector3.zero;
        
        Vector3 min = box.center - box.size;
        Vector3 max = box.center + box.size;
        
        float tMin = 0f;             // Start interval at ray origin
        float tMax = float.MaxValue; // End interval at infinity
        
        for (var i = 0; i < 3; i++)
        {
            if (Mathf.Abs(ray.direction[i]) < float.Epsilon)
            {
                if (ray.origin[i] < min[i] || ray.origin[i] > max[i])
                {
                    return false;
                }

                continue;
            }
            
            float t1 = (min[i] - ray.origin[i]) / ray.direction[i];
            float t2 = (max[i] - ray.origin[i]) / ray.direction[i];
                
            if (t1 > t2) (t1, t2) = (t2, t1);

            if (t1 > tMin)
            {
                tMin = t1;
            }

            tMax = Mathf.Min(tMax, t2);

            if (tMin > tMax)
            {
                return false;
            }
        }
        distance = tMin;
        
        rayPoint  = ray.origin + ray.direction * distance;
        rayNormal = CalculateNormalImproved(rayPoint, box.center, box.size);
        return true;
    }

    public static Vector3 CalculateNormalImproved(Vector3 intersectionPoint, Vector3 Center, Vector3 size)
    {
        Vector3 min = Center - size;
        Vector3 max = Center + size;

        // Tìm điểm giao với các mặt của hộp
        float epsilon = 0.0001f; // Giá trị ngưỡng nhỏ để xác định độ sát của điểm

        // Kiểm tra từng mặt của hộp
        if (Mathf.Abs(intersectionPoint.x - min.x) < epsilon)
            return Vector3.left;
        if (Mathf.Abs(intersectionPoint.x - max.x) < epsilon)
            return Vector3.right;

        if (Mathf.Abs(intersectionPoint.y - min.y) < epsilon)
            return Vector3.down;
        if (Mathf.Abs(intersectionPoint.y - max.y) < epsilon)
            return Vector3.up;

        if (Mathf.Abs(intersectionPoint.z - min.z) < epsilon)
            return Vector3.back;
        if (Mathf.Abs(intersectionPoint.z - max.z) < epsilon)
            return Vector3.forward;

        return Vector3.zero;
    }
    
    public static bool IntersectRayCylinder(Ray ray, BoxCollider boxCollider, out Vector3 intersectionPoint)
    {
        intersectionPoint   =  Vector3.zero;
        // Quaternion rotation = Quaternion.FromToRotation(boxCollider.axis, Vector3.up);
        // Ray rotatedRay      = new Ray(rotation * (ray.origin - boxCollider.center), rotation * ray.direction);
        //
        // if (IntersectRayWithVerticalEllipticalCylinder(rotatedRay, boxCollider, out Vector3 rotatedIntersectionPoint))
        // {
        //     intersectionPoint = Quaternion.Inverse(rotation) * rotatedIntersectionPoint + boxCollider.center;
        //     return true;
        // }
        //
        return false;
    }
    
    public static bool IntersectRayCylinder(Ray ray, BoxCollider boxCollider, out Vector3 intersectionPoint, out Vector3 intersectionNormal)
    {
        intersectionPoint     = intersectionNormal = Vector3.zero;
        // Quaternion rotation   = Quaternion.FromToRotation(boxCollider.axis, Vector3.up);
        // Ray        rotatedRay = new Ray(rotation * (ray.origin - boxCollider.center), rotation * ray.direction);
        //
        // if (IntersectRayWithVerticalEllipticalCylinder(rotatedRay, boxCollider, out Vector3 rotatedIntersectionPoint))
        // {
        //     var rotate = Quaternion.Inverse(rotation);
        //     intersectionPoint  = rotate * rotatedIntersectionPoint + boxCollider.center;
        //     intersectionNormal = rotate * (boxCollider.center - rotatedIntersectionPoint);
        //     return true;
        // }

        return false;
    }
    
    public static bool IntersectSphereRayCylinder(Ray ray, float rayRadius, BoxCollider boxCollider, out Vector3 intersectionPoint)
    {
        // Vector3 centerRate  =  new Vector3(
        //     boxCollider.radius.x / (boxCollider.radius.x + rayRadius),
        //       boxCollider.height / (boxCollider.height + rayRadius),
        //     boxCollider.radius.y / (boxCollider.radius.y + rayRadius)
        // );
        intersectionPoint   =  Vector3.zero;
        // boxCollider.height     += rayRadius;
        // boxCollider.radius     =  new Vector2(boxCollider.radius.x + rayRadius, boxCollider.radius.y + rayRadius);
        // Quaternion rotation   =  Quaternion.FromToRotation(boxCollider.axis, Vector3.up);
        // Ray        rotatedRay = new Ray(rotation * (ray.origin - boxCollider.center), rotation * ray.direction);
        //
        // if (IntersectRayWithVerticalEllipticalCylinder(rotatedRay, boxCollider, out Vector3 rotatedIntersectionPoint))
        // {
        //     var intersection = Quaternion.Inverse(rotation) * rotatedIntersectionPoint;
        //     intersectionPoint = boxCollider.center + new Vector3(
        //         intersection.x * centerRate.x,
        //         intersection.y * centerRate.y,
        //         intersection.z * centerRate.z
        //     );
        //     return true;
        // }

        return false;
    }

    private static bool IntersectRayWithVerticalEllipticalCylinder(Ray ray, BoxCollider boxCollider, out Vector3 intersectionPoint)
    {
        intersectionPoint = default;

        // Lấy bán kính theo trục x và z
        // float radiusX = boxCollider.radius.x;
        // float radiusZ = boxCollider.radius.y;
        // float hHalf = boxCollider.height / 2;
        //
        // float dx = ray.direction.x;
        // float dy = ray.direction.y;
        // float dz = ray.direction.z;
        // float ox = ray.origin.x;
        // float oy = ray.origin.y;
        // float oz = ray.origin.z;
        //
        // // Kiểm tra giao điểm với hình elip trên và dưới
        // if (CheckCylinderEllipticalFaces(ray, ref intersectionPoint, dy, oy, hHalf, radiusX, radiusZ))
        //     return true;
        //
        // // Phương trình bậc 2 cho hình trụ elip: (dx²/a² + dz²/b²)t² + 2(ox*dx/a² + oz*dz/b²)t + (ox²/a² + oz²/b² - 1) = 0
        // float a = (dx * dx) / (radiusX * radiusX) + (dz * dz) / (radiusZ * radiusZ);
        //
        // // Kiểm tra va chạm với thành hình trụ elip
        // if (CheckCylinderEllipticalLateralSurface(ray, ref intersectionPoint, a, ox, oz, oy, hHalf, radiusX, radiusZ))
        //     return true;
        //
        // float b = 2 * ((ox * dx) / (radiusX * radiusX) + (oz * dz) / (radiusZ * radiusZ));
        // float c = (ox * ox) / (radiusX * radiusX) + (oz * oz) / (radiusZ * radiusZ) - 1;
        //
        // // Giải phương trình bậc 2
        // if (CalculateQuadraticEquation(ray, ref intersectionPoint, b, a, c, hHalf))
        //     return true;

        return false;
    }

    /// <summary>
    /// Tính toán phương trình bậc 2
    /// - Tìm nghiệm dương nhỏ nhất
    /// </summary>
    private static bool CalculateQuadraticEquation(Ray ray, ref Vector3 intersectionPoint, float b, float a, float c, float hHalf)
    {
        float discriminant = b * b - 4 * a * c;
        if (discriminant < 0) return false;

        float t1 = (-b - Mathf.Sqrt(discriminant)) / (2 * a);
        float t2 = (-b + Mathf.Sqrt(discriminant)) / (2 * a);

        // Lấy t nhỏ nhất mà >= 0
        float t;
        if (t1 >= 0 && t2 >= 0)
            t = Mathf.Min(t1, t2);
        else if (t1 >= 0)
            t = t1;
        else if (t2 >= 0)
            t = t2;
        else
            return false;

        // Tính điểm giao
        Vector3 hitPoint = ray.origin + ray.direction * t;

        // Kiểm tra chiều cao
        if (Mathf.Abs(hitPoint.y) <= hHalf)
        {
            intersectionPoint = hitPoint;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Kiểm tra giao điểm với bề mặt hình trụ elip
    /// </summary>
    private static bool CheckCylinderEllipticalLateralSurface(Ray ray, ref Vector3 intersectionPoint, 
        float a, float ox, float oz, float oy, float hHalf, float radiusX, float radiusZ)
    {
        if (Mathf.Abs(a) >= float.Epsilon) return false;
        
        // Kiểm tra xem điểm gốc tia có nằm trên bề mặt hình trụ elip không
        float ellipseValue = (ox * ox) / (radiusX * radiusX) + (oz * oz) / (radiusZ * radiusZ);
        if (Mathf.Abs(ellipseValue - 1) <= float.Epsilon && Mathf.Abs(oy) <= hHalf)
        {
            intersectionPoint = ray.origin;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Kiểm tra giao điểm với 2 đầu hình trụ elip
    /// </summary>
    private static bool CheckCylinderEllipticalFaces(Ray ray, ref Vector3 intersectionPoint, 
        float dy, float oy, float hHalf, float radiusX, float radiusZ)
    {
        if (Mathf.Abs(dy) <= float.Epsilon) return false;

        float tTop = (hHalf - oy) / dy;
        float tBottom = (-hHalf - oy) / dy;
        
        // Kiểm tra mặt trên
        if (tTop >= 0)
        {
            Vector3 hitTop = ray.origin + ray.direction * tTop;
            float ellipseValueTop = (hitTop.x * hitTop.x) / (radiusX * radiusX) + (hitTop.z * hitTop.z) / (radiusZ * radiusZ);
            if (ellipseValueTop <= 1)
            {
                intersectionPoint = hitTop;
                return true;
            }
        }
        
        // Kiểm tra mặt dưới
        if (tBottom >= 0)
        {
            Vector3 hitBottom = ray.origin + ray.direction * tBottom;
            float ellipseValueBottom = (hitBottom.x * hitBottom.x) / (radiusX * radiusX) + (hitBottom.z * hitBottom.z) / (radiusZ * radiusZ);
            if (ellipseValueBottom <= 1)
            {
                intersectionPoint = hitBottom;
                return true;
            }
        }

        return false;
    }

 
    #endregion <=============================================>
    
    
    #region <====================| Conversion |====================>
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector4 ToVector4(this Vector3 vector, float w = 1f) => new Vector4(vector.x, vector.y, vector.z, w);
 
    #endregion <=============================================>
}
    
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Unity.Collections.LowLevel.Unsafe;
using UnityEditor;
#if UNITY_EDITOR
using Unity.Mathematics;
using UnityEditor.Animations;
#endif
using UnityEngine;

public static partial class VAT_Utilities
{
    public unsafe struct ColliderInfo
    {
        [NativeDisableUnsafePtrRestriction] public Cylinder** colliderAddress;
        // [NativeDisableUnsafePtrRestriction] public HitInfo*  hitInfos;
        public                                     int       colliderCount;
        public                                     bool      enabled;

        public int InstanceID;
    }

    [Serializable]
    public struct Plane
    {
        public Vector3 normal;
        public Vector3 point;
    }

    [Serializable]
    public struct Line
    {
        public Vector3 startPoint;
        public Vector3 endPoint;

        public Line(Vector3 startPoint, Vector3 endPoint)
        {
            this.startPoint = startPoint;
            this.endPoint   = endPoint;
        }

        public Vector3 direction => (endPoint - startPoint).normalized;
    }

    public class HitElement
    {
        public int RegisterID;
        // public IHittable Hittable;
    }

    [Serializable]
    public struct BoundingBox
    {
        public Vector3 min;
        public Vector3 max;
    }

    // public struct HitInfo
    // {
    //     public Vector3 point;
    //     public Vector3 normal;
    //     public float   distance;
    //     public bool    hit;
    //
    //     public int InstanceID;
    //     public int ChildIndex;
    //
    //     public WorldHitInfo WorldHitinfo => new WorldHitInfo()
    //     {
    //         RaycastHit = new RaycastHit()
    //         {
    //             point    = point,
    //             normal   = normal,
    //             distance = distance
    //         },
    //             
    //         Damage =  0
    //     };
    // }

    [Serializable]
    public struct AnimationInfo
    {
        public float frameRate;
        public float clipLength;
        public Vector2 frameRange;
    }

    [Serializable]
    public struct Cylinder
    {
        public Vector3 center;
        public Vector3 axis;
        public float   radius;
        public float   height;
    }

    [Serializable]
    public struct BoxCollider
    {
        public Vector3    center;
        public Vector3    size;
        public int        InstanceID;
        public int        ChildIndex;
        public MaskLayers layer;
        public GOTag      tag;
        
        public Vector3 HalfSize => size * 0.5f;
    }

    [Serializable]
    public class ColliderCustomCacheData
    {
        // public HittableObject HittableObjects;
        // public BoxCollider BoxColliders;
        //
        // public ColliderCustomCacheData(BoxCollider boxCollider, HittableObject hittableObjects)
        // {
        //     BoxColliders       = boxCollider;
        //     HittableObjects = hittableObjects;
        // }
        //
        // public BotNetwork GetBotNetwork()
        // {
        //     return HittableObjects.BotNetwork;
        // }
        //
        // public Vector3 WorldCenter => HittableObjects.transform.position + BoxColliders.center;
    }

#if UNITY_EDITOR
    public static List<string> GetAllStateNames(this Animator animator)
    {
        var                stateNames = new List<string>();
        AnimatorController controller = animator.runtimeAnimatorController as AnimatorController;

        if (controller != null)
        {
            stateNames.AddRange(from layer in controller.layers
                from state in layer.stateMachine.states
                select state.state.name);
        }

        return stateNames;
    }
    
    public static List<int> GetAllStateHashes(this Animator animator)
    {
        var                stateHashes = new List<int>();
        AnimatorController controller  = animator.runtimeAnimatorController as AnimatorController;

        if (controller != null)
        {
            stateHashes.AddRange(from layer in controller.layers
                from state in layer.stateMachine.states
                select state.state.nameHash);
        }

        return stateHashes;
    }
    public static AnimationClip GetAnimationClip(this Animator animator, string stateName)
    {
        AnimatorController controller = animator.runtimeAnimatorController as AnimatorController;
        return controller != null
            ? (from layer in controller.layers
                from state in layer.stateMachine.states
                where state.state.name == stateName
                select state.state.motion as AnimationClip).FirstOrDefault()
            : null;
    }
    
    public static AnimationClip GetAnimationClip(this Animator animator, int stateHash)
    {
        AnimatorController controller = animator.runtimeAnimatorController as AnimatorController;
        return controller != null
            ? (from layer in controller.layers
                from state in layer.stateMachine.states
                where state.state.nameHash == stateHash
                select state.state.motion as AnimationClip).FirstOrDefault()
            : null;
    }
    
    public static void DrawCylinder(Cylinder cylinder, Transform root, int segments = 16)
    {
        Gizmos.color = Color.green;

        // Ensure axis is normalized
        Vector3 normalizedAxis = cylinder.axis.normalized;
        float   scaledRadiusX  = cylinder.radius * root.lossyScale.x; // Scale radius along the x-axis
        float   scaledRadiusZ  = cylinder.radius * root.lossyScale.z; // Scale radius along the z-axis
        float   scaledHeight   = cylinder.height;                     // Scale height along the y-axis
        

        // Calculate the top and bottom center points of the cylinder in local space
        Vector3 bottomCenter   = cylinder.center - normalizedAxis * (scaledHeight / 2);
        Vector3 topCenter      = cylinder.center + normalizedAxis * (scaledHeight / 2);

        // Convert to world space
        bottomCenter = root.TransformPoint(bottomCenter);
        topCenter    = root.TransformPoint(topCenter);

        // Convert axis to world space (as it's a direction, we use TransformDirection)
        Vector3 worldAxis      = root.TransformDirection(normalizedAxis);

        // Draw the top and bottom circles
        // var minSize = Mathf.Min(scaledRadiusX, scaledRadiusZ);
        // DrawCircle(bottomCenter, worldAxis, minSize,  segments);
        // DrawCircle(topCenter,    worldAxis, minSize, segments);

        // Calculate a perpendicular vector to use as the rotation reference
        var perpVector = !(Mathf.Abs(Vector3.Dot(worldAxis, Vector3.up)) >= 0.99f)
            ? Vector3.Cross(worldAxis, Vector3.right).normalized
            : Vector3.Cross(worldAxis, Vector3.up).normalized;

        // Calculate rotation to align with the axis
        Quaternion rotation = Quaternion.LookRotation(perpVector, worldAxis);

        // Draw the connecting lines between circles
        for (int i = 0; i < segments; i++)
        {
            float angle     = (i       * Mathf.PI * 2) / segments;
            float nextAngle = ((i + 1) * Mathf.PI * 2) / segments;

            // Calculate points on the circles
            Vector3 offset     = new Vector3(Mathf.Sin(angle) * scaledRadiusX,     0, Mathf.Cos(angle) * scaledRadiusZ);
            Vector3 nextOffset = new Vector3(Mathf.Sin(nextAngle) * scaledRadiusX, 0, Mathf.Cos(nextAngle) * scaledRadiusZ);

            // Calculate world space points
            Vector3 bottomPoint     = bottomCenter + rotation * offset;
            Vector3 nextBottomPoint = bottomCenter + rotation * nextOffset;
            Vector3 topPoint        = topCenter    + rotation * offset;
            Vector3 nextTopPoint    = topCenter    + rotation * nextOffset;

            // Draw the lines
            Gizmos.DrawLine(bottomPoint, topPoint);        // Vertical lines
            Gizmos.DrawLine(bottomPoint, nextBottomPoint); // Bottom circle segment
            Gizmos.DrawLine(topPoint,    nextTopPoint);    // Top circle segment
        }
    }
    public static void DrawCircle(Vector3 center, Vector3 normal, float radius, int segments = 16)
    {
        // Normalize the normal vector
        normal = normal.normalized;

        // Find a vector perpendicular to the normal
        Vector3 startDir;
        if (Math.Abs(Vector3.Dot(normal, Vector3.up)) < 0.99f)
        {
            startDir = Vector3.Cross(normal, Vector3.up).normalized;
        }
        else
        {
            startDir = Vector3.Cross(normal, Vector3.right).normalized;
        }

        // Calculate the step angle
        float angleStep = 360f / segments;

        // Draw the circle segments
        Vector3 previousPoint = center + startDir * radius;
        for (int i = 1; i <= segments; i++)
        {
            // Calculate the next point on the circle
            float      angle     = angleStep * i;
            Quaternion rotation  = Quaternion.AngleAxis(angle, normal);
            Vector3    nextPoint = center + (rotation * startDir) * radius;

            // Draw the line segment
            Debug.DrawLine(previousPoint, nextPoint);

            previousPoint = nextPoint;
        }
    }
#endif


    // private static readonly Dictionary<string, BotBodyPart> tagToBodyPartMap = new Dictionary<string, BotBodyPart>
    // {
    //     { Tag.Head, BotBodyPart.Head },
    //     { Tag.Body, BotBodyPart.Body },
    //     { Tag.Armor, BotBodyPart.Body },
    //     { Tag.WeakPoint, BotBodyPart.WeakPoint }
    // };
    //
    // public static BotBodyPart GetBodyPart(Transform transform)
    // {
    //     return tagToBodyPartMap.GetValueOrDefault(transform.tag, BotBodyPart.None);
    // }

    /// <summary>
    /// Tính toán điểm giao nhau giữa một tia và một hình trụ.
    /// Đầu tiên, nó xoay tia và hình trụ để căn chỉnh hình trụ theo chiều dọc.
    /// Sau đó, nó tính toán giao điểm trong không gian đã biến đổi bằng phương pháp IntersectRayWithVerticalCylinder.
    /// Nếu tìm thấy giao điểm, nó xoay điểm giao nhau trở lại không gian ban đầu và trả về true.
    /// Nếu không tìm thấy giao điểm, nó trả về false.
    /// </summary>
    /// <param name="ray">Tia để giao với hình trụ.</param>
    /// <param name="cylinderAxis">Trục của hình trụ, đại diện cho hướng từ đáy đến đỉnh của hình trụ (thường là `transform.up`).</param>
    /// <param name="cylinder">Hình trụ để giao với tia.</param>
    /// <param name="intersectionPoint">Điểm giao nhau nếu tìm thấy giao điểm.</param>
    public static bool IntersectRayCylinder(in Ray ray, in Cylinder cylinder, out Vector3 intersectionPoint)
    {
        intersectionPoint = Vector3.zero;

        // // Step 1: Rotate the ray and cylinder to align the cylinder vertically
        // Quaternion rotation   = Quaternion.FromToRotation(cylinder.axis, Vector3.up);
        // Ray        rotatedRay = new Ray(rotation * (ray.origin - cylinder.center), rotation * ray.direction);
        //
        // // Step 2: Calculate the intersection in the transformed space
        // if (IntersectRayWithVerticalCylinder(rotatedRay, cylinder, out Vector3 rotatedIntersectionPoint))
        // {
        //     // Step 3: Rotate the intersection result back to the original space
        //     intersectionPoint = Quaternion.Inverse(rotation) * rotatedIntersectionPoint + cylinder.center;
        //     return true;
        // }

        return false;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float GetRadius(float x, float y)
    {
        return Mathf.Sqrt(x * x + y * y);
    }

    // private static bool IntersectRayWithVerticalCylinder(in Ray ray, in Cylinder cylinder, out Vector3 intersectionPoint)
    // {
    //     intersectionPoint = default;
    //
    //     // Tính các hệ số cho phương trình bậc 2
    //     float dx = ray.direction.x;
    //     float dy = ray.direction.y;
    //     float dz = ray.direction.z;
    //     float ox = ray.origin.x;
    //     float oy = ray.origin.y;
    //     float oz = ray.origin.z;float hHalf = cylinder.height / 2;  
    //
    //     // Kiểm tra giao điểm với hình tròn trên
    //     if (CheckCylinderCircularFaces(ray, cylinder, ref intersectionPoint, dy, oy, hHalf))
    //         return true;
    //
    //     // (dx²+dz²)t² + 2(ox*dx+oz*dz)t + (ox²+oz²-r²) = 0
    //     float a = dx * dx + dz * dz;
    //     
    //     // 3. Kiểm tra va chạm với thành hình trụ
    //     if (CheckCylinderLateralSurface(ray, cylinder, ref intersectionPoint, a, ox, oz, oy, hHalf))
    //         return true;
    //
    //     float b = 2 * (ox * dx + oz * dz);
    //     float c = ox * ox + oz * oz - cylinder.radius * cylinder.radius;
    //
    //     // Giải phương trình bậc 2
    //     if (CalculateQuadraticEquation(ray, ref intersectionPoint, b, a, c, hHalf))
    //         return true;
    //
    //     return false;
    // }
    //
    // /// <summary>
    // /// Tính toán phương trình bậch 2
    // /// - Tìm nghiệm dưong nhỏ nhất
    // /// </summary>
    // private static bool CalculateQuadraticEquation(in Ray ray, ref Vector3 intersectionPoint, float b, float a, float c, float hHalf)
    // {
    //     float discriminant = b * b - 4 * a * c;
    //     if (discriminant < 0) return false;
    //
    //     float t1 = (-b - Mathf.Sqrt(discriminant)) / (2 * a);
    //     float t2 = (-b + Mathf.Sqrt(discriminant)) / (2 * a);
    //
    //     // Lấy t nhỏ nhất mà >= 0
    //     float t = t1 >= 0 ? t1 : t2;
    //     if (t < 0) return false;
    //
    //     // Tính điểm giao
    //     Vector3 hitPoint = ray.origin + ray.direction * t;
    //
    //     // Kiểm tra chiều cao
    //     if (Mathf.Abs(hitPoint.y) <= hHalf)
    //     {
    //         intersectionPoint = hitPoint;
    //         return true;
    //     }
    //
    //     return false;
    // }
    //
    // /// <summary>
    // /// Kiểm tra giao điểm với bề mặt hình trụ
    // /// </summary>
    // private static bool CheckCylinderLateralSurface(in Ray ray, in Cylinder cylinder, ref Vector3 intersectionPoint, float a, float ox, float oz, float oy, float hHalf)
    // {
    //     if (Mathf.Abs(a) >= float.Epsilon) return false;
    //     
    //     float distFromAxis = Mathf.Sqrt(ox * ox + oz * oz);
    //     if (distFromAxis  <= cylinder.radius && Mathf.Abs(oy) <= hHalf)
    //     {
    //         intersectionPoint = ray.origin;
    //         return true;
    //     }
    //
    //     return false;
    // }
    //
    // /// <summary>
    // /// Kiểm tra giao điểm với 2 đầu hình trụ
    // /// </summary>
    // private static bool CheckCylinderCircularFaces(in Ray ray, in Cylinder cylinder, ref Vector3 intersectionPoint, float dy, float oy, float hHalf)
    // {
    //     if (Mathf.Abs(dy) <= float.Epsilon) return false;
    //
    //     float tTop    = (hHalf - oy) / dy;
    //     float tBottom = (-hHalf - oy) / dy;
    //     float tMin = tTop >= 0 && tTop < tBottom ? tTop : tBottom;
    //     if (tMin >= 0)
    //     {
    //         Vector3 hitMin       = ray.origin + ray.direction * tMin;
    //         float   distanceTop  = Mathf.Sqrt(hitMin.x * hitMin.x + hitMin.z * hitMin.z);
    //         if (distanceTop <= cylinder.radius)
    //         {
    //             intersectionPoint = hitMin;
    //             return true;
    //         }
    //     }
    //
    //     return false;
    // }
    public static T ToEnum<T>(this string value, T defaultValue) where T : struct
    {
        if (string.IsNullOrEmpty(value))
        {
            return defaultValue;
        }

        return Enum.TryParse<T>(value, true, out T result) ? result : defaultValue;
    }
    
    // Chuyển đổi ngầm định từ MaskLayers -> int

    // Chuyển đổi ngầm định từ int -> MaskLayers
    [MethodImpl(MethodImplOptions.AggressiveInlining)]

    public static MaskLayers ToMaskLayers(this int value)
    {
        return (MaskLayers)value;
    }
    
    // Chuyển đổi ngầm định từ Tag -> int
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static GOTag ToTag(this string value)
    {
        return value.ToEnum(GOTag.Untagged);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsLayerGround(int layer)
    {
        // cần sửa
        return false;
    }

    [System.Flags]
    public enum MaskLayers : int
    {
        Default = 0,
        
        TransparentFX           = 1 << 1,
        IgnoreRaycast           = 1 << 2,
        DefaultIngoreNavigation = 1 << 3,
        Water                   = 1 << 4,
        UI                      = 1 << 5,
        PopUp                   = 1 << 6,
        Player                  = 1 << 7,
        Arsenal                 = 1 << 8,
        Bot                     = 1 << 9,
        Obstacle                = 1 << 10,
        Pickup                  = 1 << 11,
        Inventory               = 1 << 12,
        MissionDetail           = 1 << 13,
        Supporter               = 1 << 14,
        IndieObstacle           = 1 << 15,
        PopupUpgrade            = 1 << 16,
        CharacterBG             = 1 << 17,
        Sky                     = 1 << 18,
        BakeNavMesh             = 1 << 31
    }

    public enum GOTag : byte
    {
        Untagged = 0,
        Respawn = 1,
        Finish = 2,
        EditorOnly = 3,
        MainCamera = 4,
        Player = 5,
        GameController = 6,
        Head = 7, 
        Body = 8,
        WeakPoint = 9,
        Ground = 10,
        Virtual = 11,
        Enemy = 12,
        Armor = 13,
        Wood = 14,
        PlayerPosition = 15
    }
}
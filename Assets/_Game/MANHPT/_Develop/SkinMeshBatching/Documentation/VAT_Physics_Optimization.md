# Tối ưu hệ thống Physics VAT

## 6. Các file code liên quan

### 6.1. Core Physics
1. **VAT_Physics.cs**
   - Chứa logic chính cho raycast và collision detection
   - Triển khai các phương thức: RayCast, SphereCast, OverlapSphere
   - Sử dụng Job System và Burst Compiler
   ```csharp
   public partial class VAT_Physics
   {
       public static unsafe bool RayCast(Ray ray, float nearestDistance, out VAT_Utilities.HitInfo closestHit);
       public static unsafe bool OverlapSphere(ref NativeList<VAT_Utilities.HitInfo> hitResults, Vector3 sphereCenter, float sphereRadius);
       public static bool CheckLineCast(Vector3 start, Vector3 end);
   }
   ```

2. **VAT_Physics.NonVAT.cs**
   - Triển khai phiên bản non-VAT của các phương thức physics
   - Tối ưu cho các đối tượng không sử dụng VAT
   ```csharp
   public partial class VAT_Physics
   {
       public static unsafe bool CheckRayCast(Ray ray, float nearestDistance);
       public static unsafe bool CheckCapsule(Ray ray, float radius, float height);
       public static unsafe bool CheckSphere(Ray ray, float radius);
       public static unsafe bool TryHandleRayCastHit(Ray ray, float nearestDistance, HitInfo hitInfo);
   }
   ```

3. **VAT_Physics.JobHandle.cs**
   - Chứa các job definitions cho physics calculations
   - Triển khai các job cho tìm kiếm hit:
     + FindAnyHitJob: Tìm bất kỳ hit nào
     + FindClosestHitJob: Tìm hit gần nhất
     + FindEntireHitJob: Tìm tất cả các hit
   ```csharp
   [BurstCompile]
   private unsafe struct FindClosestHitJob : IJobParallelFor
   {
       public NativeReference<VAT_Utilities.HitInfo> HitResult;
       public VAT_Utilities.ColliderInfo* ColliderInfos;
   }
   ```

4. **VAT_Physics.UnitTest.cs**
   - Chứa các công cụ test và debug cho physics system
   - Hỗ trợ visualization cho raycast và collision
   ```csharp
   public partial class VAT_Physics : MonoBehaviour
   {
       public enum RayCastType { Raycast, SphereCast, OverlapSphere }
       public bool isDrawDebug;
       private void DrawDebugRay();
   }
   ```

### 6.2. Collider Management
1. **VAT_ColliderManager.cs**
   - Quản lý collider data và transforms
   - Xử lý đăng ký và xóa collider
   - Quản lý cache cho bot network
   ```csharp
   public partial class VAT_ColliderManager : MonoBehaviour
   {
       public static TransformAccessArray _colliderTransforms;
       public static UnsafeList<VAT_Utilities.CylinderExtend> _colliderInfos;
       public static Dictionary<int, BotNetworkCache> _hittableObjects;
       public static UnsafeList<VAT_Utilities.HitInfo> _hitInfos;
   }
   ```

2. **CylinderCacheData.cs**
   - Lưu trữ và quản lý cylinder collider data
   - Tích hợp với hệ thống animation
   - Tự động đăng ký/hủy đăng ký với ColliderManager
   ```csharp
   public class CylinderCacheData : MonoBehaviour
   {
       public VAT_Utilities.ColliderCustomCacheData[] cacheData;
       public int ColliderCount => cacheData.Length;
       
       private void OnEnable() => VAT_ColliderManager.Add(this);
       private void OnDisable() => VAT_ColliderManager.Remove(this);
   }
   ```

### 6.3. Cấu trúc dữ liệu Physics

1. **Cylinder Collider**
```csharp
public struct Cylinder
{
    public Vector3 center;    // Tâm của cylinder
    public Vector3 axis;      // Trục của cylinder
    public float radius;      // Bán kính
    public float height;      // Chiều cao
}

public struct CylinderExtend : Cylinder
{
    public int InstanceID;    // ID của instance
    public int ChildIndex;    // Index của collider con
}
```

2. **Hit Info**
```csharp
public struct HitInfo
{
    public bool hit;          // Có hit hay không
    public Vector3 point;     // Điểm hit
    public Vector3 normal;    // Normal tại điểm hit
    public float distance;    // Khoảng cách từ origin đến hit
    public int InstanceID;    // ID của object bị hit
    public int ChildIndex;    // Index của collider bị hit
}
```

### 6.4. Job System Integration

1. **Raycast Job**
```csharp
[BurstCompile]
public struct NonVAT_RayCastJob : IJobParallelForTransform
{
    public Ray Ray;
    [NativeDisableUnsafePtrRestriction]
    public VAT_Utilities.CylinderExtend* ColliderInfoAddress;
    [NativeDisableUnsafePtrRestriction]
    public VAT_Utilities.HitInfo* hitInfos;
}
```

2. **Sphere Cast Job**
```csharp
[BurstCompile]
public struct NonVAT_SphereCastJob : IJobParallelForTransform
{
    public Ray SphereAxisRay;
    public float SphereRadius;
    [NativeDisableUnsafePtrRestriction]
    public VAT_Utilities.CylinderExtend* ColliderInfoAddress;
    [NativeDisableUnsafePtrRestriction]
    public VAT_Utilities.HitInfo* hitInfos;
}
```

### 6.5. Memory Management

1. **Native Collections**
```csharp
// Collider data
public static TransformAccessArray _colliderTransforms;
public static UnsafeList<VAT_Utilities.CylinderExtend> _colliderInfos;
public static UnsafeList<VAT_Utilities.HitInfo> _hitInfos;

// Cache
public static Dictionary<int, BotNetworkCache> _hittableObjects;
public static Dictionary<int, (Material _animation, Material _crossFade)> Materials;
```

2. **Lifecycle Management**
```csharp
private void OnEnable()
{
    VAT_ColliderManager.Add(this);
}

private void OnDisable()
{
    VAT_ColliderManager.Remove(this);
}

private void OnDestroy()
{
    if (HitInfos.IsCreated)
        HitInfos.Dispose();
}
```

### 6.6. Performance Considerations

1. **Burst Compilation**
- Tất cả các job đều được đánh dấu với [BurstCompile]
- Sử dụng unsafe code để tối ưu hiệu năng
- Tránh boxing/unboxing trong hot paths

2. **Memory Layout**
- Sử dụng struct để tối ưu memory layout
- Áp dụng SoA (Structure of Arrays) thay vì AoS (Array of Structures)
- Pre-allocate buffers với kích thước phù hợp

3. **Thread Safety**
- Sử dụng [NativeDisableParallelForRestriction] khi cần thiết
- Đảm bảo thread safety khi truy cập shared data
- Tránh race conditions trong jobs

## 7. Lưu ý quan trọng

### 7.1. Memory Management
- Sử dụng Native Collections cho job system
- Cần dispose đúng cách để tránh memory leak
- Pre-allocate buffers khi có thể

### 7.2. Thread Safety
- Đảm bảo thread safety khi sử dụng job system
- Sử dụng [NativeDisableParallelForRestriction] khi cần thiết
- Cẩn thận với việc truy cập shared data

### 7.3. Performance Considerations
- Sử dụng Burst Compiler cho các hot paths
- Tránh boxing/unboxing trong physics calculations
- Minimize garbage collection pressure

### 7.4. Dependencies
- Unity Job System
- Unity Mathematics
- Unity Collections
- Unity Burst 
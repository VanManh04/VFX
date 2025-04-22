# Hệ thống Collider trong VAT

Hệ thống Vertex Animation Texture (VAT) sử dụng một hệ thống collider tùy chỉnh thay vì sử dụng hệ thống collider mặc định của Unity. Điều này giúp tối ưu hiệu năng và đồng bộ hóa collider với animation VAT.

## Vấn đề với Collider mặc định

Khi sử dụng VAT, các vấn đề sau xuất hiện với hệ thống collider mặc định của Unity:

1. **Hiệu năng thấp**:
   - Việc đồng bộ collider với transform của Unity tốn nhiều tài nguyên
   - Mỗi collider yêu cầu một component riêng, gây áp lực lên CPU

2. **Không đồng bộ với VAT**:
   - Collider mặc định không biết vị trí vertex từ VAT
   - Khó khăn trong việc cập nhật collider theo animation VAT

## Giải pháp: Hệ thống Collider tùy chỉnh

Hệ thống VAT sử dụng một hệ thống collider tùy chỉnh dựa trên các hình trụ (cylinder) để phát hiện va chạm.

### Cấu trúc dữ liệu

#### 1. Cylinder

```csharp
[Serializable]
public struct Cylinder
{
    public Vector3 center;
    public Vector3 axis;
    public float   radius;
    public float   height;
}
```

Đây là cấu trúc cơ bản để mô tả một collider hình trụ.

#### 2. CylinderExtend

```csharp
[Serializable]
public struct CylinderExtend
{
    public Vector3 center;
    public Vector3 axis;
    public Vector2 radius;
    public float   height;
    public int     InstanceID;
    public int     ChildIndex;
}
```

Mở rộng từ Cylinder, thêm thông tin về InstanceID và ChildIndex để xác định đối tượng.

#### 3. ColliderCustomCacheData

```csharp
[Serializable]
public class ColliderCustomCacheData
{
    public HittableObject HittableObjects;
    public CylinderExtend Cylinders;

    public ColliderCustomCacheData(CylinderExtend cylinderExtend, HittableObject hittableObjects)
    {
        Cylinders       = cylinderExtend;
        HittableObjects = hittableObjects;
    }
    
    public BotNetwork GetBotNetwork()
    {
        return HittableObjects.BotNetwork;
    }
}
```

Kết hợp thông tin về collider và đối tượng có thể bị hit.

#### 4. CylinderCacheData

```csharp
public class CylinderCacheData : MonoBehaviour
{
    public bool                           ActiveGizmos = false;
    public VAT_Utilities.ColliderCustomCacheData[] cacheData;

    public int ColliderCount => cacheData.Length;
    public BotNetwork BotNetwork => cacheData[0].HittableObjects.BotNetwork;
    
    private void OnEnable()
    {
        VAT_ColliderManager.Add(this);
    }
    
    private void OnDisable()
    {
        VAT_ColliderManager.Remove(this);
    }
    
    public void InitData(int cacheCount)
    {
        cacheData = new VAT_Utilities.ColliderCustomCacheData[cacheCount];
    }
    
    // ... (phần còn lại của class)
}
```

Component quản lý các collider của một đối tượng.

### Quản lý Collider

#### VAT_ColliderManager

```csharp
public static class VAT_ColliderManager
{
    public static Dictionary<int, (Material _animation, Material _crossFade)> Materials = new();
    public static Dictionary<int, BotNetworkCache>         _hittableObjects = new ();
    public static TransformAccessArray                     _colliderTransforms;
    
    // ... (phần còn lại của class)
    
    public static void Add(CylinderCacheData cylinderCacheData)
    {
        // ... (code thêm collider)
    }
    
    public static void Remove(CylinderCacheData cylinderCacheData)
    {
        // ... (code xóa collider)
    }
}
```

Quản lý tất cả các collider trong hệ thống, cung cấp các phương thức để thêm, xóa và truy cập collider.

### Cập nhật Collider

#### ColliderUpdate

```csharp
public class ColliderUpdate : BaseAnimationUpdate, ISetCacheData
{
    [SerializeField] private ColliderCacheData _colliderCacheData;
    
    // ... (phần còn lại của class)
    
    public override void FrameUpdate(int currentFrame)
    {
        // Cập nhật vị trí và kích thước collider dựa trên frame hiện tại
    }
    
    public override void ChangeState(string stateName, float duration = 0, bool isLoop = false)
    {
        // Chuyển đổi trạng thái collider
    }
    
    public override void Init()
    {
        // Khởi tạo collider
    }
}
```

Component cập nhật vị trí và kích thước collider theo animation VAT.

### Phát hiện va chạm

Hệ thống VAT sử dụng các phương thức Raycast và SphereCast tùy chỉnh để phát hiện va chạm.

#### VAT_Physics

```csharp
public partial class VAT_Physics
{
    // Raycast
    public static unsafe bool RayCast(Ray ray, float nearestDistance, out VAT_Utilities.HitInfo closestHit)
    {
        // ... (code raycast)
    }
    
    // SphereCast
    public static unsafe bool SphereCast(Ray ray, float radius, float nearestDistance, out VAT_Utilities.HitInfo closestHit)
    {
        // ... (code spherecast)
    }
    
    // OverlapSphere
    public static unsafe bool OverlapSphere(ref NativeList<VAT_Utilities.HitInfo> hitResults, Vector3 sphereCenter, float sphereRadius)
    {
        // ... (code overlapsphere)
    }
    
    // ... (phần còn lại của class)
}
```

Cung cấp các phương thức để kiểm tra va chạm với collider tùy chỉnh.

### Job System và Burst Compile

Hệ thống collider sử dụng Unity Job System và Burst Compile để tối ưu hiệu năng.

```csharp
[BurstCompile]
public struct VAT_RaycastJob : IJobParallelForTransform
{
    public Ray Ray;
    [NativeDisableUnsafePtrRestriction] public VAT_Utilities.ColliderInfo* ColliderInfoAddress;
    
    public void Execute(int index, TransformAccess transform)
    {
        // ... (code thực hiện raycast)
    }
}
```

Các job được thực hiện song song trên nhiều luồng, tăng hiệu năng phát hiện va chạm.

## Quy trình hoạt động

### 1. Khởi tạo

1. **Editor Time**:
   - Tạo và cấu hình các collider hình trụ cho nhân vật
   - Bake thông tin collider theo từng frame animation vào ColliderCacheData

2. **Runtime (Awake/Start)**:
   - Khởi tạo CylinderCacheData
   - Đăng ký với VAT_ColliderManager

### 2. Cập nhật

1. **Mỗi frame**:
   - ColliderUpdate cập nhật vị trí và kích thước collider dựa trên frame animation hiện tại
   - VAT_ColliderManager cập nhật thông tin collider trong hệ thống

### 3. Phát hiện va chạm

1. **Raycast/SphereCast**:
   - Tạo job để kiểm tra va chạm với tất cả collider
   - Thực hiện job song song trên nhiều luồng
   - Trả về kết quả va chạm

2. **Xử lý va chạm**:
   - Nếu có va chạm, xác định đối tượng bị hit
   - Gọi callback để xử lý damage hoặc hiệu ứng

## Tối ưu hóa và thách thức

### Tối ưu hóa hiện tại

1. **Job System và Burst Compile**:
   - Thực hiện kiểm tra va chạm song song trên nhiều luồng
   - Tối ưu hóa code với Burst Compile

2. **Cấu trúc dữ liệu hiệu quả**:
   - Sử dụng cấu trúc dữ liệu đơn giản (hình trụ) để giảm chi phí tính toán
   - Tổ chức dữ liệu để tối ưu cho cache locality

### Thách thức

1. **Độ chính xác**:
   - Collider hình trụ có thể không khớp hoàn toàn với mesh
   - Cần cân bằng giữa độ chính xác và hiệu năng

2. **Đồng bộ hóa**:
   - Đảm bảo collider luôn đồng bộ với animation VAT
   - Xử lý các trường hợp đặc biệt (ví dụ: cross fade)

## Kế hoạch phát triển

### 1. Cải thiện độ chính xác

- Phát triển các loại collider phức tạp hơn (ví dụ: capsule, box)
- Tối ưu hóa thuật toán phát hiện va chạm

### 2. Tích hợp với Ragdoll

- Mở rộng hệ thống collider để hỗ trợ ragdoll
- Tích hợp với Verlet Integration để mô phỏng vật lý

### 3. Tối ưu hóa hiệu năng

- Cải thiện cấu trúc dữ liệu và thuật toán
- Tối ưu hóa Job System và Burst Compile 
# Kiến trúc hệ thống VAT

Hệ thống Vertex Animation Texture (VAT) được thiết kế theo kiến trúc module, với các thành phần tách biệt nhưng có sự tương tác chặt chẽ với nhau. Tài liệu này mô tả chi tiết về kiến trúc hệ thống, các thành phần chính và cách chúng tương tác với nhau.

## Tổng quan kiến trúc

Hệ thống VAT được chia thành các module chính sau:

1. **Animation Module**: Quản lý và cập nhật animation
2. **Rendering Module**: Xử lý việc render mesh với VAT
3. **Physics Module**: Xử lý va chạm và tương tác vật lý
4. **Cache Module**: Quản lý dữ liệu cache cho animation, collider, v.v.
5. **Utility Module**: Cung cấp các tiện ích và công cụ hỗ trợ

## Sơ đồ lớp

```
+------------------+     +------------------+     +------------------+
| AnimationController |<----| VertexUpdate      |<----| VertexCacheData   |
+------------------+     +------------------+     +------------------+
         |                       |                        |
         v                       v                        v
+------------------+     +------------------+     +------------------+
| VAT_AnimationUpdater |     | ColliderUpdate    |<----| ColliderCacheData |
+------------------+     +------------------+     +------------------+
                                |
                                v
                        +------------------+
                        | VAT_Physics      |
                        +------------------+
                                |
                                v
                        +------------------+
                        | VAT_ColliderManager |
                        +------------------+
```

## Các thành phần chính

### 1. Animation Module

#### AnimationController

```csharp
public class AnimationController : MonoBehaviour
{
    [ReadOnly] public  StateInforsData           StateInforsData;
    [ReadOnly] public  BaseAnimationUpdate[]     AnimationUpdates;
    [ReadOnly] public  StateInforsData.StateInfo CurrentStateInfo;
    
    // ... (phần còn lại của class)
    
    public void ChangeState(string stateName, float duration = 0, bool isLoop = false)
    {
        // Chuyển đổi trạng thái animation
    }
    
    public void UpdateAnim()
    {
        // Cập nhật animation
    }
}
```

**Trách nhiệm**:
- Quản lý trạng thái animation
- Cập nhật frame hiện tại
- Điều phối các AnimationUpdate

#### VAT_AnimationUpdater

```csharp
public class VAT_AnimationUpdater : MonoBehaviour
{
    private readonly List<AnimationController> _animationControllers = new List<AnimationController>();
    public static    VAT_AnimationUpdater      Instance { get; private set; }
    
    // ... (phần còn lại của class)
    
    private void Update()
    {
        for (var i = 0; i < _animationControllers.Count; i++)
        {
            _animationControllers[i].UpdateAnim();
        }
    }
}
```

**Trách nhiệm**:
- Quản lý tất cả các AnimationController
- Cập nhật tất cả các animation mỗi frame

#### BaseAnimationUpdate

```csharp
public abstract class BaseAnimationUpdate : MonoBehaviour
{
    public abstract void Init();
    public abstract void FrameUpdate(int currentFrame);
    public abstract void ChangeState(string stateName, float duration = 0, bool isLoop = false);
}
```

**Trách nhiệm**:
- Định nghĩa interface chung cho các loại animation update
- Cung cấp các phương thức cơ bản để khởi tạo, cập nhật và chuyển đổi trạng thái

### 2. Rendering Module

#### VertexUpdate

```csharp
public class VertexUpdate : BaseAnimationUpdate, ISetCacheData
{
    [ReadOnly] [SerializeField] private VertexCacheData.StateInfo _currentStateInfo;
    [ReadOnly] [SerializeField] private Vector4 _currentTimingData;
    [ReadOnly] [SerializeField] private string _currentStateName = "Idle";
    [ReadOnly] [SerializeField] private MeshRenderer _meshRenderer;
    
    // ... (phần còn lại của class)
    
    public override void FrameUpdate(int currentFrame)
    {
        // Cập nhật vị trí vertex dựa trên frame hiện tại
    }
    
    public void CrossFade(string stateName, float crossFadeDuration, bool loop = false)
    {
        // Thực hiện cross fade giữa hai animation
    }
}
```

**Trách nhiệm**:
- Cập nhật vị trí vertex dựa trên texture VAT
- Xử lý cross fade giữa các animation
- Quản lý shader và material

#### AnimMeshUVGenerator

```csharp
public class AnimMeshUVGenerator : MonoBehaviour
{
    public void GenerateUV(Mesh mesh)
    {
        // Tạo UV đặc biệt cho mesh
    }
}
```

**Trách nhiệm**:
- Tạo UV đặc biệt cho mesh để sử dụng với VAT

### 3. Physics Module

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
    
    // ... (phần còn lại của class)
}
```

**Trách nhiệm**:
- Cung cấp các phương thức để kiểm tra va chạm
- Xử lý các phép tính toán vật lý

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
}
```

**Trách nhiệm**:
- Cập nhật vị trí và kích thước collider theo animation
- Đồng bộ hóa collider với VAT

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

**Trách nhiệm**:
- Quản lý tất cả các collider trong hệ thống
- Cung cấp các phương thức để thêm, xóa và truy cập collider

### 4. Cache Module

#### VertexCacheData

```csharp
public class VertexCacheData : ScriptableObject
{
    public Texture2D VAT;
    public List<StateInfo> StateInfors;
    
    // ... (phần còn lại của class)
    
    public StateInfo GetStateInfo(string stateName)
    {
        // Lấy thông tin trạng thái từ tên
    }
    
    public void GetFrameRangeAndBoundingBox(string stateName, out Vector2 frameRange, out BoundingBox boundingBox)
    {
        // Lấy thông tin frame range và bounding box
    }
}
```

**Trách nhiệm**:
- Lưu trữ texture VAT
- Quản lý thông tin về các trạng thái animation
- Cung cấp dữ liệu cho VertexUpdate

#### ColliderCacheData

```csharp
public class ColliderCacheData : ScriptableObject
{
    public List<StateInfo> StateInfors;
    
    // ... (phần còn lại của class)
    
    public StateInfo GetStateInfo(string stateName)
    {
        // Lấy thông tin trạng thái từ tên
    }
}
```

**Trách nhiệm**:
- Lưu trữ thông tin về collider theo từng frame
- Cung cấp dữ liệu cho ColliderUpdate

#### CylinderCacheData

```csharp
public class CylinderCacheData : MonoBehaviour
{
    public bool                           ActiveGizmos = false;
    public VAT_Utilities.ColliderCustomCacheData[] cacheData;
    
    // ... (phần còn lại của class)
    
    public void InitData(int cacheCount)
    {
        // Khởi tạo dữ liệu cache
    }
}
```

**Trách nhiệm**:
- Lưu trữ thông tin về các collider hình trụ
- Đăng ký với VAT_ColliderManager

### 5. Utility Module

#### VAT_Utilities

```csharp
public static partial class VAT_Utilities
{
    public unsafe struct ColliderInfo
    {
        // ... (định nghĩa struct)
    }
    
    [Serializable]
    public struct Cylinder
    {
        // ... (định nghĩa struct)
    }
    
    // ... (phần còn lại của class)
}
```

**Trách nhiệm**:
- Cung cấp các cấu trúc dữ liệu và tiện ích chung
- Hỗ trợ các module khác

## Luồng dữ liệu

### 1. Khởi tạo

```
Editor -> Bake Animation -> VertexCacheData
                         -> ColliderCacheData
                         -> StateInforsCache
```

1. Trong Editor, animation được bake thành các texture VAT
2. Thông tin về animation, collider, và trạng thái được lưu vào các ScriptableObject

### 2. Runtime

```
AnimationController -> UpdateAnim() -> VertexUpdate -> FrameUpdate() -> Shader
                                    -> ColliderUpdate -> FrameUpdate() -> VAT_ColliderManager
```

1. AnimationController cập nhật frame hiện tại
2. VertexUpdate cập nhật vị trí vertex dựa trên frame hiện tại
3. ColliderUpdate cập nhật vị trí và kích thước collider
4. Shader sample texture VAT để lấy vị trí vertex
5. VAT_ColliderManager cập nhật thông tin collider trong hệ thống

### 3. Chuyển đổi Animation

```
AnimationController -> ChangeState() -> VertexUpdate -> CrossFade() -> Shader
                                     -> ColliderUpdate -> ChangeState()
```

1. AnimationController nhận lệnh chuyển đổi trạng thái
2. VertexUpdate thực hiện cross fade giữa hai animation
3. ColliderUpdate chuyển đổi trạng thái collider
4. Shader blend giữa hai animation

### 4. Phát hiện va chạm

```
Game Logic -> VAT_Physics -> RayCast() -> VAT_ColliderManager -> HitInfo
```

1. Game Logic gọi phương thức RayCast
2. VAT_Physics tạo job để kiểm tra va chạm
3. Job được thực hiện song song trên nhiều luồng
4. Kết quả va chạm được trả về

## Mở rộng hệ thống

Hệ thống VAT được thiết kế để dễ dàng mở rộng với các tính năng mới:

### 1. BlendTree

```csharp
public class BlendTreeUpdate : BaseAnimationUpdate
{
    [SerializeField] private List<BlendAnimation> _blendAnimations;
    [SerializeField] private BlendParameter _blendParameter;
    
    // ... (phần còn lại của class)
    
    public override void FrameUpdate(int currentFrame)
    {
        // Blend giữa các animation dựa trên tham số
    }
}
```

### 2. RootMotion

```csharp
public class RootMotionUpdate : BaseAnimationUpdate
{
    [SerializeField] private RootMotionCacheData _rootMotionCacheData;
    
    // ... (phần còn lại của class)
    
    public override void FrameUpdate(int currentFrame)
    {
        // Cập nhật vị trí và hướng của nhân vật dựa trên root motion
    }
}
```

### 3. Ragdoll

```csharp
public class RagdollSystem : MonoBehaviour
{
    [SerializeField] private AnimationController _animationController;
    private NativeArray<Particle> _particles;
    private NativeArray<IConstraint> _constraints;
    
    // ... (phần còn lại của class)
    
    public void EnableRagdoll(Vector3 impactForce, Vector3 impactPoint)
    {
        // Chuyển từ animation sang ragdoll
    }
    
    private void Update()
    {
        // Cập nhật ragdoll
    }
}
```

## Kết luận

Kiến trúc hệ thống VAT được thiết kế theo module, với các thành phần tách biệt nhưng có sự tương tác chặt chẽ với nhau. Điều này giúp dễ dàng bảo trì, mở rộng và tối ưu hóa hệ thống. Các module chính bao gồm Animation, Rendering, Physics, Cache và Utility, mỗi module đảm nhận một trách nhiệm cụ thể trong hệ thống. 
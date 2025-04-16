# Tối ưu hóa hiệu năng trong VAT

Hệ thống Vertex Animation Texture (VAT) được thiết kế với mục tiêu tối ưu hiệu năng để có thể render nhiều nhân vật có animation cùng lúc trên thiết bị di động cấu hình thấp. Tài liệu này mô tả các phương pháp tối ưu hiệu năng đã và sẽ áp dụng.

## Hiện trạng

Hiện tại, hệ thống VAT có thể:
- Render lên đến 1000 bot với 30 FPS trên thiết bị di động
- Giảm dung lượng từ 2MB xuống còn 400KB cho mỗi bot với 5 animation
- Hỗ trợ GPU Instancing để giảm số lượng draw call

## Các phương pháp tối ưu hiện tại

### 1. Chuyển gánh nặng từ CPU sang GPU

#### Vấn đề
SkinnedMeshRenderer truyền thống yêu cầu CPU tính toán skinning, gây áp lực lên CPU và giới hạn số lượng nhân vật.

#### Giải pháp
- Sử dụng VAT để lưu trữ vị trí vertex theo từng frame
- Để GPU xử lý việc sample và tính toán vị trí vertex
- Giảm tải cho CPU, cho phép xử lý nhiều nhân vật hơn

### 2. GPU Instancing

#### Vấn đề
Mỗi nhân vật yêu cầu một draw call riêng, gây áp lực lên CPU và GPU.

#### Giải pháp
```csharp
_animationLoopMaterial.enableInstancing = true;
_crossFadeMaterial.enableInstancing = true;
```

- Sử dụng GPU Instancing để render nhiều nhân vật cùng loại với một draw call
- Giảm đáng kể số lượng draw call
- Tăng hiệu năng khi render nhiều nhân vật cùng loại

### 3. MaterialPropertyBlock

#### Vấn đề
Thay đổi trực tiếp material properties sẽ tạo ra nhiều instance của material, tốn bộ nhớ và giảm hiệu năng.

#### Giải pháp
```csharp
_meshRenderer.GetPropertyBlock(_materialPropertyBlock);
_materialPropertyBlock.SetFloat(ShaderIDLib.Frame, frameValue);
_meshRenderer.SetPropertyBlock(_materialPropertyBlock);
```

- Sử dụng MaterialPropertyBlock để cập nhật shader properties mà không tạo material mới
- Giảm số lượng material instance
- Tối ưu bộ nhớ và hiệu năng

### 4. Job System và Burst Compile

#### Vấn đề
Các tính toán vật lý và va chạm có thể tốn nhiều tài nguyên CPU.

#### Giải pháp
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

- Sử dụng Unity Job System để thực hiện các tính toán song song trên nhiều luồng
- Áp dụng Burst Compile để tối ưu hóa code
- Tăng hiệu năng phát hiện va chạm và tính toán vật lý

### 5. Tối ưu dung lượng

#### Vấn đề
Texture VAT chiếm nhiều dung lượng (ban đầu là 2MB, hiện tại là 400KB cho 1 bot với 5 animation).

#### Giải pháp
```csharp
// Chuyển texture sang mảng byte
byte[] textureBytes = texture.EncodeToPNG();

// Khi cần sử dụng, chuyển từ mảng byte về texture
Texture2D newTexture = new Texture2D(2, 2);
newTexture.LoadImage(textureBytes);
```

- Chuyển texture sang mảng byte để giảm dung lượng
- Khi bắt đầu vào game (trong Awake), chuyển từ mảng byte về texture
- Giảm dung lượng từ 2MB xuống còn 400KB

## Kế hoạch tối ưu trong tương lai

### 1. Nén Delta

#### Vấn đề
Texture VAT lưu trữ toàn bộ vị trí vertex tại mỗi frame, dẫn đến dung lượng lớn.

#### Giải pháp
```csharp
// Thay vì lưu vị trí tuyệt đối
float3 position = GetVertexPosition(vertexIndex, frameIndex);

// Lưu sự thay đổi so với frame trước đó
float3 delta = GetVertexPosition(vertexIndex, frameIndex) - GetVertexPosition(vertexIndex, frameIndex - 1);
```

- Chỉ lưu trữ sự thay đổi (delta) giữa các frame thay vì lưu toàn bộ vị trí
- Frame đầu tiên lưu vị trí tuyệt đối, các frame sau lưu delta
- Có thể giảm đáng kể dung lượng dữ liệu (mục tiêu dưới 100KB)

#### Thách thức
- Cần thay đổi shader để tính toán vị trí từ delta
- Có thể ảnh hưởng đến hiệu năng GPU

### 2. Tối ưu hóa Shader

#### Vấn đề
Shader hiện tại có thể được tối ưu hơn nữa để giảm chi phí tính toán.

#### Giải pháp
```hlsl
// Trước khi tối ưu
float3 pos = tex2Dlod(_VAT, float4(VAT_UV, 0, 0)).xyz;
float3 boundingRange = boundingBoxMax.xyz - boundingBoxMin.xyz;
pos = boundingRange * pos;
pos = pos + boundingBoxMin;

// Sau khi tối ưu
float3 pos = lerp(boundingBoxMin.xyz, boundingBoxMax.xyz, tex2Dlod(_VAT, float4(VAT_UV, 0, 0)).xyz);
```

- Giảm số lượng texture sample
- Sử dụng các kỹ thuật tối ưu như early-out
- Tối ưu hóa các phép tính toán

### 3. Lazy Loading

#### Vấn đề
Load tất cả animation vào bộ nhớ cùng lúc có thể tốn nhiều tài nguyên.

#### Giải pháp
```csharp
public void LoadAnimation(string animationName)
{
    if (!_loadedAnimations.ContainsKey(animationName))
    {
        // Load animation từ asset bundle hoặc resources
        var animationData = Resources.Load<AnimationData>(animationName);
        _loadedAnimations.Add(animationName, animationData);
    }
}

public void UnloadAnimation(string animationName)
{
    if (_loadedAnimations.ContainsKey(animationName) && animationName != _currentAnimation)
    {
        // Giải phóng bộ nhớ
        Resources.UnloadAsset(_loadedAnimations[animationName]);
        _loadedAnimations.Remove(animationName);
    }
}
```

- Chỉ load các animation cần thiết vào bộ nhớ
- Giải phóng bộ nhớ khi không cần thiết
- Quản lý bộ nhớ hiệu quả hơn

### 4. Culling và LOD

#### Vấn đề
Render tất cả nhân vật với cùng độ phức tạp, bất kể khoảng cách.

#### Giải pháp
```csharp
public void UpdateLOD(float distanceFromCamera)
{
    if (distanceFromCamera > _highLODDistance)
    {
        // Sử dụng LOD thấp hoặc tắt một số tính năng
        _currentLOD = 2;
    }
    else if (distanceFromCamera > _mediumLODDistance)
    {
        _currentLOD = 1;
    }
    else
    {
        _currentLOD = 0;
    }
    
    // Cập nhật shader với LOD mới
    _materialPropertyBlock.SetFloat(ShaderIDLib.LOD, _currentLOD);
}
```

- Triển khai hệ thống LOD để giảm chi phí rendering khi nhân vật ở xa
- Sử dụng occlusion culling để không render nhân vật không nhìn thấy
- Cân bằng giữa chất lượng hình ảnh và hiệu năng

#### Thách thức
- LOD cho VAT sẽ làm tăng dung lượng vì mỗi LOD cần một VAT riêng
- Cần tìm giải pháp để cân bằng giữa hiệu năng và dung lượng

## Đo lường và phân tích hiệu năng

### Công cụ

- Unity Profiler: Phân tích hiệu năng CPU và GPU
- Memory Profiler: Theo dõi sử dụng bộ nhớ
- Frame Debugger: Phân tích quá trình rendering

### Metrics

- **FPS**: Mục tiêu duy trì 30 FPS trên thiết bị mục tiêu
- **Draw Calls**: Giảm xuống mức tối thiểu
- **Memory Usage**: Giảm dung lượng texture VAT xuống dưới 100KB
- **CPU Usage**: Giảm tải cho CPU
- **GPU Usage**: Tối ưu shader để giảm tải cho GPU

## Kết luận

Tối ưu hóa hiệu năng là một quá trình liên tục. Hệ thống VAT đã đạt được những cải thiện đáng kể về hiệu năng so với SkinnedMeshRenderer truyền thống, nhưng vẫn còn nhiều cơ hội để tối ưu hơn nữa. Các phương pháp như nén delta, tối ưu hóa shader, và lazy loading sẽ giúp giảm dung lượng và tăng hiệu năng, đặc biệt trên thiết bị di động cấu hình thấp. 
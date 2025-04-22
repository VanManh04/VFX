# Shader và Rendering trong VAT

Hệ thống Vertex Animation Texture (VAT) sử dụng các shader đặc biệt để xử lý animation dựa trên texture. Các shader này đóng vai trò quan trọng trong việc chuyển đổi dữ liệu từ texture VAT thành animation mượt mà trên mesh.

## Các Shader chính

### 1. UnlitVertexAnimation

Shader chính được sử dụng để render animation từ texture VAT.

```hlsl
Shader "Horus/UnlitVertexAnimation"
{
    Properties
    {
        [_BaseMap]_BaseMap("Base Map (RGB)", 2D) = "white" {}
        _VAT("VAT", 2D) = "white" {}
        _BoundingBoxMin("Bounding Box Min", Vector) = (0, 0, 0, 0)
        _BoundingBoxMax("Bounding Box Max", Vector) = (1, 1, 1, 1)
        _TimingData("Timing Data", Vector) = (0, 0, 0, 0)
        _Loop("Loop", Float) = 0
        _Frame("Frame", Float) = 0
    }
    
    // ... (phần còn lại của shader)
}
```

**Chức năng**:
- Sample vị trí vertex từ texture VAT dựa trên UV đặc biệt
- Tính toán vị trí cuối cùng của vertex dựa trên bounding box
- Hỗ trợ animation loop

**Cách hoạt động**:
1. Nhận UV đặc biệt từ mesh (UV3)
2. Sử dụng UV.x để xác định vị trí vertex trên texture
3. Sử dụng _Frame (được cập nhật động) làm UV.y để xác định frame hiện tại
4. Sample texture VAT để lấy vị trí vertex
5. Áp dụng bounding box để tính toán vị trí cuối cùng

### 2. UnlitVertexCrossFade

Shader được sử dụng để thực hiện cross fade giữa hai animation.

```hlsl
Shader "Horus/UnlitVertexCrossFade"
{
    Properties
    {
        [_BaseMap]_BaseMap("Base Map (RGB)", 2D) = "white" {}
        _VAT("VAT", 2D) = "white" {}
        _BoundingBoxMin("Bounding Box Min", Vector) = (0, 0, 0, 0)
        _BoundingBoxMax("Bounding Box Max", Vector) = (1, 1, 1, 1)
        _PreviousBoundingBoxMin("Previous Bounding Box Min", Vector) = (0, 0, 0, 0)
        _PreviousBoundingBoxMax("Previous Bounding Box Max", Vector) = (1, 1, 1, 1)
        _LerpTiming("Lerp Timing", Float) = 0
        _Frame("Frame", Float) = 0
        _PreviousFrame("Previous Frame", Float) = 0
    }
    
    // ... (phần còn lại của shader)
}
```

**Chức năng**:
- Blend giữa hai vị trí vertex từ hai frame animation khác nhau
- Tạo hiệu ứng chuyển đổi mượt mà giữa các animation

**Cách hoạt động**:
1. Sample vị trí vertex từ animation hiện tại
2. Sample vị trí vertex từ animation trước đó
3. Sử dụng _LerpTiming để blend giữa hai vị trí
4. Áp dụng bounding box tương ứng cho mỗi vị trí

### 3. UnlitVertexAnimationNoLoop

Phiên bản của UnlitVertexAnimation không hỗ trợ loop, dùng cho các animation chỉ chạy một lần.

### 4. UnlitVertexBlendTree

Shader hỗ trợ blend giữa nhiều animation, tương tự như BlendTree trong Animator.

```hlsl
Shader "Horus/UnlitVertexBlendTree"
{
    Properties
    {
        [_BaseMap]_BaseMap("Base Map (RGB)", 2D) = "white" {}
        _VAT("VAT", 2D) = "white" {}
        _BoundingBoxMin1("Bounding Box Min 1", Vector) = (0, 0, 0, 0)
        _BoundingBoxMax1("Bounding Box Max 1", Vector) = (1, 1, 1, 1)
        _BoundingBoxMin2("Bounding Box Min 2", Vector) = (0, 0, 0, 0)
        _BoundingBoxMax2("Bounding Box Max 2", Vector) = (1, 1, 1, 1)
        _BlendFactor("Blend Factor", Range(0, 1)) = 0
        _Frame1("Frame 1", Float) = 0
        _Frame2("Frame 2", Float) = 0
    }
    
    // ... (phần còn lại của shader)
}
```

**Chức năng**:
- Blend giữa hai hoặc nhiều animation dựa trên các tham số
- Hỗ trợ các loại blend khác nhau (1D, 2D)

## Quy trình Rendering

### 1. Khởi tạo

```csharp
public override void Init()
{
    _materialPropertyBlock = new MaterialPropertyBlock();
    _cancellationTokenSource = new CancellationTokenSource();
    _crossFadeShader = _globalShader.UnlitVertexCrossfade;
    _animationShader = _globalShader.UnlitVertexAnimation;

    CacheData.InitTextures();
    _VAT = CacheData.VAT;
    _animMeshUVGenerator.GenerateUV(_skinMesh.sharedMesh);

    // ... (phần còn lại của hàm)
    
    _meshFilter.sharedMesh = _skinMesh.sharedMesh;
    _meshRenderer.sharedMaterial = _animationLoopMaterial;
    _skinMesh.enabled = false;
    _animator.enabled = false;
    _animationRoot.SetActive(false);
}
```

**Các bước**:
1. Khởi tạo MaterialPropertyBlock để cập nhật shader properties
2. Lấy texture VAT từ CacheData
3. Tạo UV đặc biệt cho mesh
4. Thiết lập material với shader phù hợp
5. Vô hiệu hóa SkinnedMeshRenderer và Animator gốc

### 2. Cập nhật Frame

```csharp
public override void FrameUpdate(int currentFrame)
{
    CurrentFrame = currentFrame;
    if (_isCrossFade)
    {
        _meshRenderer.GetPropertyBlock(_materialPropertyBlock);
        _materialPropertyBlock.SetFloat(ShaderIDLib.LerpTiming,
            LerpTiming(_currentTimingData.x, _crossFadeDuration));
        _materialPropertyBlock.SetFloat(ShaderIDLib.PreviousFrame,
            GetFrame(_previousTimingData.x, _previousTimingData.y, _previousTimingData.z, _previousFrameRange));
        _materialPropertyBlock.SetFloat(ShaderIDLib.Frame,
            GetFrame(_currentTimingData.x, _currentTimingData.y, _currentTimingData.z, _frameRange));
        _meshRenderer.SetPropertyBlock(_materialPropertyBlock);
    }

    if (_isAnimation)
    {
        _meshRenderer.GetPropertyBlock(_materialPropertyBlock);
        _materialPropertyBlock.SetFloat(ShaderIDLib.Frame,
            GetFrame(_currentTimingData.x, _currentTimingData.y, _currentTimingData.z, _frameRange));
        _meshRenderer.SetPropertyBlock(_materialPropertyBlock);
    }
}
```

**Các bước**:
1. Cập nhật frame hiện tại
2. Nếu đang trong quá trình cross fade:
   - Cập nhật LerpTiming dựa trên thời gian
   - Cập nhật frame cho animation hiện tại và trước đó
3. Nếu đang chạy animation bình thường:
   - Chỉ cập nhật frame cho animation hiện tại

### 3. Chuyển đổi Animation

```csharp
public void CrossFade(string stateName, float crossFadeDuration, bool loop = false)
{
    CrossFadeAsync(stateName, crossFadeDuration, loop, _cancellationTokenSource);
}

public async void CrossFadeAsync(string stateName, float crossFadeDuration, bool loop,
    CancellationTokenSource cancellationTokenSource)
{
    _isCrossFade = true;
    _isAnimation = false;
    _isLoop = true;
    
    _crossFadeDuration = crossFadeDuration;

    _meshRenderer.sharedMaterial = _crossFadeMaterial;
    _meshRenderer.GetPropertyBlock(_materialPropertyBlock);
    _previousTimingData = _currentTimingData;
    var previousStateName = string.IsNullOrWhiteSpace(_currentStateInfo.stateName) ? stateName : _currentStateInfo.stateName;
    CacheData.GetFrameRangeAndBoundingBox(previousStateName, out _previousFrameRange, out var previousBoundingBox);
    _currentStateInfo = CacheData.GetStateInfo(stateName);
    CacheData.GetFrameRangeAndBoundingBox(stateName, out _frameRange, out var boundingBox);
    _currentFrameRate = _currentStateInfo.animationInfo.frameRate;
    _currentTimingData = new Vector4(Time.time, _currentStateInfo.animationInfo.clipLength, _currentFrameRate, _currentLOD);
    _materialPropertyBlock.SetVector(ShaderIDLib.PreviousBoundingBoxMin, previousBoundingBox.min);
    _materialPropertyBlock.SetVector(ShaderIDLib.PreviousBoundingBoxMax, previousBoundingBox.max);
    _materialPropertyBlock.SetVector(ShaderIDLib.BoundingBoxMin, boundingBox.min);
    _materialPropertyBlock.SetVector(ShaderIDLib.BoundingBoxMax, boundingBox.max);

    _meshRenderer.SetPropertyBlock(_materialPropertyBlock);

    try
    {
        await UniTask.Delay(TimeSpan.FromSeconds(crossFadeDuration),
            cancellationToken: cancellationTokenSource.Token);
    }
    catch (OperationCanceledException e)
    {
        return;
    }

    _isCrossFade = false;
    _isAnimation = true;
    _isLoop = loop;
    
    _meshRenderer.sharedMaterial = _animationLoopMaterial;
    _meshRenderer.GetPropertyBlock(_materialPropertyBlock);
    _materialPropertyBlock.SetFloat(ShaderIDLib.Loop, loop ? 1.0f : 0.0f);
    _materialPropertyBlock.SetVector(ShaderIDLib.BoundingBoxMin, boundingBox.min);
    _materialPropertyBlock.SetVector(ShaderIDLib.BoundingBoxMax, boundingBox.max);
    _meshRenderer.SetPropertyBlock(_materialPropertyBlock);
}
```

**Các bước**:
1. Chuyển sang shader CrossFade
2. Lưu thông tin timing và bounding box của animation trước đó
3. Lấy thông tin của animation mới
4. Cập nhật các tham số shader
5. Đợi cho đến khi quá trình cross fade hoàn thành
6. Chuyển về shader animation thông thường

## Tối ưu hóa Rendering

### 1. GPU Instancing

Hệ thống VAT sử dụng GPU Instancing để render nhiều nhân vật cùng lúc với hiệu năng cao.

```csharp
_animationLoopMaterial.enableInstancing = true;
_crossFadeMaterial.enableInstancing = true;
```

**Lợi ích**:
- Giảm đáng kể số lượng draw call
- Tăng hiệu năng khi render nhiều nhân vật cùng loại

### 2. MaterialPropertyBlock

Sử dụng MaterialPropertyBlock thay vì thay đổi trực tiếp material để tránh tạo nhiều instance của material.

```csharp
_meshRenderer.GetPropertyBlock(_materialPropertyBlock);
_materialPropertyBlock.SetFloat(ShaderIDLib.Frame, frameValue);
_meshRenderer.SetPropertyBlock(_materialPropertyBlock);
```

**Lợi ích**:
- Giảm số lượng material instance
- Tối ưu bộ nhớ và hiệu năng

## Kế hoạch phát triển

### 1. BlendTree trong Shader

Phát triển shader BlendTree để hỗ trợ blend giữa nhiều animation dựa trên các tham số.

**Cách tiếp cận**:
- Sample nhiều vị trí vertex từ các animation khác nhau
- Blend các vị trí dựa trên trọng số
- Hỗ trợ các loại blend khác nhau (1D, 2D)

### 2. Tối ưu hóa Shader

Tối ưu shader để giảm thiểu tính toán và tăng hiệu năng.

**Phương pháp**:
- Giảm số lượng texture sample
- Sử dụng các kỹ thuật tối ưu như early-out
- Tối ưu hóa các phép tính toán

### 3. LOD cho VAT

Phát triển hệ thống LOD cho VAT để giảm chi phí rendering khi nhân vật ở xa.

**Thách thức**:
- LOD cho VAT sẽ làm tăng dung lượng vì mỗi LOD cần một VAT riêng
- Cần tìm giải pháp để cân bằng giữa hiệu năng và dung lượng 
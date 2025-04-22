# Triển khai BlendTree cho hệ thống VAT

## Tổng quan

BlendTree là một tính năng quan trọng trong hệ thống animation, cho phép pha trộn mượt mà giữa nhiều animation khác nhau dựa trên các tham số. Tài liệu này mô tả phương pháp triển khai BlendTree cho hệ thống Vertex Animation Texture (VAT) hiện tại.

## Yêu cầu

- **Loại BlendTree**: Hỗ trợ BlendTree 1D và 2D
- **Số lượng animation blend cùng lúc**: 2 animation
- **Tham số điều khiển**: Biến float, tùy theo cách đặt tên, cập nhật bằng code
- **Hiệu năng**: Duy trì 30 FPS với 1000 bot trên thiết bị di động
- **Tích hợp**: Đọc dữ liệu từ Animator của Unity và chuyển thành dữ liệu cache

## Phân tích shader CrossFade hiện tại

Shader UnlitVertexCrossFade hiện tại hoạt động như sau:

- Shader này cho phép blend giữa hai animation (trạng thái trước đó và trạng thái hiện tại)
- Sử dụng một texture VAT duy nhất và sample ở hai vị trí khác nhau (frame trước và frame hiện tại)
- Sử dụng tham số `_LerpTiming` để kiểm soát mức độ blend giữa hai animation
- Mỗi animation có bounding box riêng (`_BoundingBoxMin`/`_BoundingBoxMax` và `_PreviousBoundingBoxMin`/`_PreviousBoundingBoxMax`)
- Quá trình blend diễn ra trong một khoảng thời gian (`_crossFadeDuration`)

```hlsl
// Đoạn code chính của shader CrossFade
float3 positionPrevious = tex2Dlod(_VAT, float4(VAT_UV_Previous, 0, 0)).xyz;
float3 positionCurrent = tex2Dlod(_VAT, float4(VAT_UV_Current, 0, 0)).xyz;

// Áp dụng bounding box
float3 boundingRangePrevious = _PreviousBoundingBoxMax.xyz - _PreviousBoundingBoxMin.xyz;
positionPrevious = boundingRangePrevious * positionPrevious;
positionPrevious = positionPrevious + _PreviousBoundingBoxMin.xyz;

float3 boundingRangeCurrent = _BoundingBoxMax.xyz - _BoundingBoxMin.xyz;
positionCurrent = boundingRangeCurrent * positionCurrent;
positionCurrent = positionCurrent + _BoundingBoxMin.xyz;

// Blend giữa hai vị trí
float3 finalPosition = lerp(positionPrevious, positionCurrent, _LerpTiming);
```

## Cách tiếp cận BlendTree

### 1. Sử dụng một texture VAT duy nhất

Thay vì sử dụng nhiều texture, chúng ta sẽ tiếp tục sử dụng một texture VAT duy nhất chứa tất cả animation của bot. Shader BlendTree sẽ sample từ các vị trí khác nhau trong texture này, tương tự như cách CrossFade hiện tại hoạt động.

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

Trong fragment shader:

```hlsl
// Sample từ animation 1
float2 uv1 = float2(vertexUV.x, _Frame1 / _VAT_TexelSize.w);
float3 position1 = tex2Dlod(_VAT, float4(uv1, 0, 0)).xyz;
position1 = lerp(_BoundingBoxMin1.xyz, _BoundingBoxMax1.xyz, position1);

// Sample từ animation 2
float2 uv2 = float2(vertexUV.x, _Frame2 / _VAT_TexelSize.w);
float3 position2 = tex2Dlod(_VAT, float4(uv2, 0, 0)).xyz;
position2 = lerp(_BoundingBoxMin2.xyz, _BoundingBoxMax2.xyz, position2);

// Blend giữa hai vị trí
float3 finalPosition = lerp(position1, position2, _BlendFactor);
```

### 2. Cấu trúc dữ liệu cho BlendTree

Chúng ta sẽ tích hợp BlendTree vào StateInforsData hiện tại:

```csharp
[System.Serializable]
public class BlendTreeData
{
    public enum BlendTreeType { OneD, TwoD }
    
    public BlendTreeType type;
    public string parameterName;
    public BlendTreeNode[] nodes;
    
    [System.Serializable]
    public class BlendTreeNode
    {
        public string stateName;  // Tên trạng thái animation
        public float threshold;   // Ngưỡng cho 1D BlendTree
        public Vector2 position;  // Vị trí cho 2D BlendTree
    }
}

// Mở rộng StateInfo
public class StateInfo
{
    // Các trường hiện tại
    public string stateName;
    public AnimationInfo animationInfo;
    
    // Thêm trường mới
    public bool isBlendTree;
    public BlendTreeData blendTreeData;
}
```

### 3. Xử lý animation có độ dài khác nhau

Chúng ta sẽ sử dụng phương pháp chuẩn hóa thời gian:

```csharp
// Chuyển đổi thời gian thực sang thời gian chuẩn hóa (0-1) cho mỗi animation
float normalizedTime1 = (Time.time - startTime) / animation1.length;
float frame1 = normalizedTime1 * animation1.frameCount;

float normalizedTime2 = (Time.time - startTime) / animation2.length;
float frame2 = normalizedTime2 * animation2.frameCount;

// Cập nhật shader
_materialPropertyBlock.SetFloat(ShaderIDLib.Frame1, frame1);
_materialPropertyBlock.SetFloat(ShaderIDLib.Frame2, frame2);
```

Lưu ý: Chúng ta sẽ không blend hai animation có sử dụng animation event để tránh phức tạp trong việc đồng bộ hóa events.

### 4. Triển khai BlendTree 1D

BlendTree 1D blend giữa các animation dựa trên một tham số duy nhất:

```csharp
public void UpdateBlendTree1D(float parameter)
{
    // Tìm hai node gần nhất với giá trị tham số
    BlendTreeNode node1 = null;
    BlendTreeNode node2 = null;
    float blend = 0;
    
    for (int i = 0; i < blendTreeData.nodes.Length - 1; i++)
    {
        if (parameter >= blendTreeData.nodes[i].threshold && 
            parameter <= blendTreeData.nodes[i + 1].threshold)
        {
            node1 = blendTreeData.nodes[i];
            node2 = blendTreeData.nodes[i + 1];
            
            // Tính toán hệ số blend
            float range = node2.threshold - node1.threshold;
            blend = range > 0 ? (parameter - node1.threshold) / range : 0;
            break;
        }
    }
    
    if (node1 != null && node2 != null)
    {
        // Cập nhật shader với hai animation và hệ số blend
        UpdateBlendShader(node1.stateName, node2.stateName, blend);
    }
}
```

### 5. Triển khai BlendTree 2D

BlendTree 2D blend giữa các animation dựa trên hai tham số (thường là vectơ 2D):

```csharp
public void UpdateBlendTree2D(Vector2 parameter)
{
    // Tìm ba node gần nhất tạo thành tam giác chứa điểm tham số
    // (Sử dụng thuật toán Delaunay triangulation hoặc đơn giản hóa)
    
    // Đơn giản hóa: Tìm hai node gần nhất
    BlendTreeNode node1 = null;
    BlendTreeNode node2 = null;
    float minDist1 = float.MaxValue;
    float minDist2 = float.MaxValue;
    
    foreach (var node in blendTreeData.nodes)
    {
        float dist = Vector2.Distance(parameter, node.position);
        
        if (dist < minDist1)
        {
            minDist2 = minDist1;
            node2 = node1;
            
            minDist1 = dist;
            node1 = node;
        }
        else if (dist < minDist2)
        {
            minDist2 = dist;
            node2 = node;
        }
    }
    
    if (node1 != null && node2 != null)
    {
        // Tính toán hệ số blend dựa trên khoảng cách
        float totalDist = minDist1 + minDist2;
        float blend = totalDist > 0 ? minDist2 / totalDist : 0;
        
        // Cập nhật shader với hai animation và hệ số blend
        UpdateBlendShader(node1.stateName, node2.stateName, blend);
    }
}
```

### 6. Đọc dữ liệu từ Animator của Unity

```csharp
public void ExtractBlendTreeData(AnimatorController controller)
{
    foreach (var layer in controller.layers)
    {
        foreach (var state in layer.stateMachine.states)
        {
            var motion = state.state.motion;
            if (motion is BlendTree blendTree)
            {
                // Phân tích BlendTree và tạo BlendTreeData
                BlendTreeData data = new BlendTreeData();
                data.parameterName = blendTree.blendParameter;
                
                if (blendTree.blendType == UnityEngine.AnimatorControllerParameterType.Float)
                    data.type = BlendTreeType.OneD;
                else if (blendTree.blendType == UnityEngine.AnimatorControllerParameterType.Vector2)
                    data.type = BlendTreeType.TwoD;
                
                // Phân tích các child motion
                data.nodes = new BlendTreeNode[blendTree.children.Length];
                for (int i = 0; i < blendTree.children.Length; i++)
                {
                    var child = blendTree.children[i];
                    data.nodes[i] = new BlendTreeNode();
                    data.nodes[i].stateName = GetAnimationName(child.motion);
                    data.nodes[i].threshold = child.threshold;
                    data.nodes[i].position = child.position;
                }
                
                // Lưu BlendTreeData vào StateInforsData
                StateInfo stateInfo = new StateInfo();
                stateInfo.stateName = state.state.name;
                stateInfo.isBlendTree = true;
                stateInfo.blendTreeData = data;
                
                // Thêm vào StateInforsData
                stateInforsData.AddStateInfo(stateInfo);
            }
        }
    }
}
```

### 7. Kiểm thử hiệu năng

```csharp
public class BlendTreePerformanceTest : MonoBehaviour
{
    public int botCount = 1000;
    public GameObject botPrefab;
    
    private List<GameObject> bots = new List<GameObject>();
    private float[] fpsHistory = new float[100];
    private int fpsIndex = 0;
    
    void Start()
    {
        // Tạo bots
        for (int i = 0; i < botCount; i++)
        {
            var bot = Instantiate(botPrefab);
            bots.Add(bot);
        }
    }
    
    void Update()
    {
        // Đo FPS
        fpsHistory[fpsIndex] = 1.0f / Time.deltaTime;
        fpsIndex = (fpsIndex + 1) % fpsHistory.Length;
        
        // Cập nhật tham số BlendTree
        foreach (var bot in bots)
        {
            var controller = bot.GetComponent<AnimationController>();
            controller.SetBlendParameter("Speed", Random.Range(0f, 1f));
        }
    }
    
    void OnGUI()
    {
        // Hiển thị FPS trung bình
        float avgFps = 0;
        for (int i = 0; i < fpsHistory.Length; i++)
            avgFps += fpsHistory[i];
        avgFps /= fpsHistory.Length;
        
        GUI.Label(new Rect(10, 10, 200, 20), $"Bots: {botCount}, FPS: {avgFps:F1}");
    }
}
```

## Lộ trình triển khai

1. **Phase 1: Thiết kế và cấu trúc dữ liệu (1-2 tuần)**
   - Mở rộng StateInforsData để hỗ trợ BlendTree
   - Tạo công cụ để đọc BlendTree từ Animator của Unity

2. **Phase 2: Shader và rendering (1-2 tuần)**
   - Phát triển shader UnlitVertexBlendTree
   - Triển khai logic để sample từ các vị trí khác nhau trong texture VAT
   - Tối ưu hóa shader cho hiệu năng

3. **Phase 3: Runtime và AnimationController (1-2 tuần)**
   - Mở rộng AnimationController để hỗ trợ BlendTree
   - Triển khai logic để cập nhật tham số BlendTree
   - Xử lý các trường hợp đặc biệt (animation độ dài khác nhau)

4. **Phase 4: Tích hợp với nén Delta (1 tuần)**
   - Đảm bảo BlendTree hoạt động với texture đã nén Delta
   - Tối ưu hóa quá trình giải nén và cache

5. **Phase 5: Kiểm thử và tối ưu hóa (1-2 tuần)**
   - Phát triển test cases
   - Triển khai công cụ kiểm thử hiệu năng
   - Tối ưu hóa dựa trên kết quả kiểm thử

Tổng thời gian dự kiến: 5-9 tuần tùy thuộc vào độ phức tạp và các vấn đề phát sinh.

## Kết luận

Việc triển khai BlendTree cho hệ thống VAT là khả thi với cách tiếp cận sử dụng một texture VAT duy nhất và tích hợp vào StateInforsData. Bằng cách tận dụng cơ chế blend đã có trong shader CrossFade và mở rộng để hỗ trợ BlendTree 1D và 2D, chúng ta có thể tạo ra một hệ thống animation linh hoạt và hiệu quả.

Các thách thức chính bao gồm việc xử lý animation có độ dài khác nhau và đảm bảo hiệu năng tốt khi render nhiều bot. Tuy nhiên, với kế hoạch triển khai phù hợp, chúng ta có thể giải quyết các thách thức này và đạt được mục tiêu đề ra. 
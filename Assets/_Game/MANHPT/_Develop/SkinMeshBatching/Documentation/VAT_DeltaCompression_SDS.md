# Software Design Specification: Tối ưu Dung lượng Texture VAT

## 1. Tổng quan

### 1.1 Mục tiêu
Giảm dung lượng texture VAT từ 400KB xuống còn dưới 100KB cho mỗi bot với 5 animation, đồng thời duy trì chất lượng animation và hiệu năng.

### 1.2 Phạm vi
- Tối ưu hóa dung lượng VertexCacheData
- Triển khai Delta Compression với RGB24 cho keyframes và RGB565 cho delta frames
- Tích hợp Run-Length Encoding (RLE) cho delta frames
- Giải nén dữ liệu trong Awake thành texture bình thường và sử dụng shader hiện tại

### 1.3 Các bên liên quan
- Đội phát triển VAT
- Đội tối ưu hiệu năng
- Đội nghệ thuật (kiểm tra chất lượng animation)

## 2. Kiến trúc

### 2.1 Cấu trúc dữ liệu

```csharp
// Cấu trúc dữ liệu cho Delta Compression
[Serializable]
public class DeltaCompressedData
{
    public byte[] keyframeData;      // Dữ liệu RGB24 cho keyframe
    public byte[] deltaFrameData;    // Dữ liệu RGB565 cho delta frames
    public int totalFrames;          // Tổng số frame
    public int vertexCount;          // Số lượng vertex
}

// Mở rộng VertexCacheData
public class VertexCacheData : ScriptableObject
{
    [SerializeField] public List<StateInfo> StateInfors;
    
    [Serializable]
    public class StateInfo
    {
        [SerializeField] public string stateName;
        [SerializeField] public VAT_Utilities.AnimationInfo animationInfo;
        [SerializeField] public ModelBoundingBox boundingBox;
        [SerializeField] public int frameCount;
        [SerializeField] public DeltaCompressedData compressedData;
        [SerializeField] public bool isCompressed;
        
        [NonSerialized] public Texture2D texture;
    }
}
```

### 2.2 Luồng dữ liệu

1. **Quá trình nén (Editor)**:
   - Bake animation để lấy dữ liệu vertex
   - Lưu frame đầu tiên làm keyframe dưới dạng RGB24
   - Tính toán delta frames (sự thay đổi so với frame trước đó) và lưu dưới dạng RGB565
   - Áp dụng RLE cho delta frames
   - Lưu dữ liệu nén vào VertexCacheData

2. **Quá trình giải nén (Runtime - Awake)**:
   - Giải nén dữ liệu từ DeltaCompressedData
   - Tạo texture từ dữ liệu đã giải nén
   - Sử dụng texture với shader hiện tại

## 3. Thiết kế chi tiết

### 3.1 Delta Compression

#### 3.1.1 Chọn keyframe
- Chỉ frame đầu tiên (frame 0) là keyframe
- Tất cả các frame còn lại đều là delta frame
- Delta là sự thay đổi so với frame trước đó

#### 3.1.2 Định dạng dữ liệu
- Keyframe: RGB24 (8 bit/kênh, 24 bit/pixel)
- Delta frames: RGB565 (5 bit R, 6 bit G, 5 bit B, 16 bit/pixel)

#### 3.1.3 Thuật toán nén
```
Thuật toán Nén Delta:
1. Lưu frame đầu tiên làm keyframe với định dạng RGB24
2. Với mỗi frame tiếp theo:
   a. Tính delta = currentFrame - previousFrame
   b. Chuẩn hóa delta vào khoảng [0,1]
   c. Chuyển đổi sang RGB565
   d. Áp dụng RLE nếu có nhiều giá trị lặp lại
```

### 3.2 Run-Length Encoding (RLE)

#### 3.2.1 Thuật toán RLE
```
Thuật toán RLE:
1. Khởi tạo mảng kết quả
2. Duyệt qua mảng đầu vào:
   a. Đếm số lần lặp lại của giá trị hiện tại
   b. Nếu số lần lặp lại > ngưỡng (ví dụ: 3):
      i. Lưu cặp (số lần lặp, giá trị)
   c. Nếu không:
      i. Lưu các giá trị riêng lẻ
```

#### 3.2.2 Định dạng dữ liệu RLE
```
Định dạng RLE:
[Marker Byte][Count][Value] hoặc [Value]

Marker Byte: 0xFF (chỉ ra rằng đây là một chuỗi lặp lại)
Count: Số lần lặp lại (1 byte)
Value: Giá trị lặp lại (2 bytes cho RGB565)
```

### 3.3 Giải nén trong Runtime

#### 3.3.1 Giải nén dữ liệu
```csharp
// Giải nén dữ liệu trong Awake
public void DecompressData()
{
    foreach (var stateInfo in StateInfors)
    {
        if (stateInfo.isCompressed && stateInfo.compressedData != null)
        {
            // Giải nén dữ liệu
            var decompressedData = DecompressVertexData(stateInfo.compressedData);
            
            // Tạo texture từ dữ liệu đã giải nén
            stateInfo.texture = CreateTextureFromDecompressedData(decompressedData, 
                stateInfo.compressedData.vertexCount, 
                stateInfo.compressedData.totalFrames);
        }
    }
}
```

## 4. Triển khai

### 4.1 Các lớp và phương thức chính

#### 4.1.1 DeltaCompressor
```csharp
public class DeltaCompressor
{
    // Nén dữ liệu vertex
    public DeltaCompressedData Compress(List<FrameInfo> frames);
    
    // Giải nén dữ liệu vertex
    public List<Vector3[]> Decompress(DeltaCompressedData compressedData);
    
    // Áp dụng RLE
    private byte[] ApplyRLE(byte[] data);
    
    // Giải nén RLE
    private byte[] DecompressRLE(byte[] compressedData);
    
    // Chuyển đổi RGB24 sang RGB565
    private ushort ConvertToRGB565(Color color);
    
    // Chuyển đổi RGB565 sang RGB24
    private Color ConvertFromRGB565(ushort value);
}
```

#### 4.1.2 VertexCacheDataExtensions
```csharp
public static class VertexCacheDataExtensions
{
    // Nén VertexCacheData
    public static void CompressData(this VertexCacheData cacheData);
    
    // Giải nén VertexCacheData
    public static void DecompressData(this VertexCacheData cacheData);
    
    // Tạo texture từ dữ liệu đã giải nén
    public static Texture2D CreateTextureFromDecompressedData(Vector3[] decompressedData, int vertexCount, int frameCount);
}
```

### 4.2 Quy trình triển khai

1. **Giai đoạn 1: Cài đặt cơ bản**
   - Triển khai DeltaCompressor
   - Mở rộng VertexCacheData để hỗ trợ dữ liệu nén
   - Cập nhật Editor để hỗ trợ nén dữ liệu

2. **Giai đoạn 2: Runtime**
   - Triển khai giải nén trong Awake
   - Tạo texture từ dữ liệu đã giải nén

3. **Giai đoạn 3: Tối ưu và kiểm thử**
   - Tối ưu thuật toán nén
   - Kiểm thử hiệu năng và chất lượng
   - Điều chỉnh tham số để cân bằng giữa dung lượng và chất lượng

## 5. UML Diagram

### 5.1 Class Diagram

```
+------------------------+       +------------------------+
|   VertexCacheData      |       |   DeltaCompressor     |
+------------------------+       +------------------------+
| - StateInfors          |       | + Compress()          |
+------------------------+       | + Decompress()        |
| + CompressData()       |       | - ApplyRLE()          |
| + DecompressData()     |       | - DecompressRLE()     |
+------------------------+       | - ConvertToRGB565()   |
           |                     | - ConvertFromRGB565() |
           |                     +------------------------+
           v
+------------------------+
|      StateInfo         |
+------------------------+
| - stateName            |
| - animationInfo        |
| - boundingBox          |
| - frameCount           |
| - compressedData       |
| - isCompressed         |
| - texture              |
+------------------------+
           |
           |
           v
+------------------------+
|  DeltaCompressedData   |
+------------------------+
| - keyframeData         |
| - deltaFrameData       |
| - totalFrames          |
| - vertexCount          |
+------------------------+
```

### 5.2 Sequence Diagram

```
+--------+    +----------------+    +---------------+    +----------------+
| Editor |    | VertexCacheData|    | DeltaCompressor|    | StateInfo     |
+--------+    +----------------+    +---------------+    +----------------+
    |                 |                    |                     |
    | 1. BakeAnimation|                    |                     |
    |---------------->|                    |                     |
    |                 |                    |                     |
    |                 | 2. CompressData()  |                     |
    |                 |------------------->|                     |
    |                 |                    |                     |
    |                 |                    | 3. Compress()      |
    |                 |                    |-------------------->|
    |                 |                    |                     |
    |                 |                    | 4. Return          |
    |                 |                    |<--------------------|
    |                 |                    |                     |
    |                 | 5. Return          |                     |
    |                 |<-------------------|                     |
    |                 |                    |                     |
    | 6. SaveAsset    |                    |                     |
    |---------------->|                    |                     |
    |                 |                    |                     |
```

```
+---------+    +----------------+    +---------------+    +----------------+
| Runtime |    | VertexCacheData|    | DeltaCompressor|    | StateInfo     |
+---------+    +----------------+    +---------------+    +----------------+
    |                 |                    |                     |
    | 1. Awake        |                    |                     |
    |---------------->|                    |                     |
    |                 |                    |                     |
    |                 | 2. DecompressData()|                     |
    |                 |------------------->|                     |
    |                 |                    |                     |
    |                 |                    | 3. Decompress()    |
    |                 |                    |-------------------->|
    |                 |                    |                     |
    |                 |                    | 4. Return          |
    |                 |                    |<--------------------|
    |                 |                    |                     |
    |                 | 5. CreateTexture   |                     |
    |                 |-------------------->                     |
    |                 |                    |                     |
    | 6. Use Texture  |                    |                     |
    |<----------------|                    |                     |
    |                 |                    |                     |
```

## 6. Procedural Code

### 6.1 Nén dữ liệu

```csharp
public DeltaCompressedData Compress(List<FrameInfo> frames)
{
    if (frames == null || frames.Count == 0)
        return null;
    
    int vertexCount = frames[0].vertices.Length;
    int frameCount = frames.Count;
    
    // Chuẩn bị dữ liệu
    var keyframeData = new List<byte>();
    var deltaFrameData = new List<byte>();
    
    // Lưu keyframe (frame đầu tiên)
    for (int v = 0; v < vertexCount; v++)
    {
        Vector3 vertex = frames[0].vertices[v];
        
        // Chuẩn hóa vertex vào khoảng [0,1]
        Vector3 normalizedVertex = NormalizeVertex(vertex);
        
        // Chuyển đổi sang RGB24
        keyframeData.Add((byte)(normalizedVertex.x * 255));
        keyframeData.Add((byte)(normalizedVertex.y * 255));
        keyframeData.Add((byte)(normalizedVertex.z * 255));
    }
    
    // Tính toán và lưu delta frames
    for (int f = 1; f < frameCount; f++)
    {
        var deltasForFrame = new List<byte>();
        
        for (int v = 0; v < vertexCount; v++)
        {
            // Tính delta
            Vector3 delta = frames[f].vertices[v] - frames[f-1].vertices[v];
            
            // Chuẩn hóa delta vào khoảng [0,1]
            Vector3 normalizedDelta = NormalizeDelta(delta);
            
            // Chuyển đổi sang RGB565
            ushort rgb565 = ConvertToRGB565(normalizedDelta);
            
            // Thêm vào danh sách
            deltasForFrame.Add((byte)(rgb565 >> 8));
            deltasForFrame.Add((byte)(rgb565 & 0xFF));
        }
        
        // Áp dụng RLE cho delta frame
        byte[] compressedDeltas = ApplyRLE(deltasForFrame.ToArray());
        
        // Thêm vào dữ liệu delta frames
        deltaFrameData.AddRange(compressedDeltas);
    }
    
    // Tạo và trả về dữ liệu nén
    return new DeltaCompressedData
    {
        keyframeData = keyframeData.ToArray(),
        deltaFrameData = deltaFrameData.ToArray(),
        totalFrames = frameCount,
        vertexCount = vertexCount
    };
}
```

### 6.2 Giải nén dữ liệu

```csharp
public List<Vector3[]> Decompress(DeltaCompressedData compressedData)
{
    if (compressedData == null)
        return null;
    
    int vertexCount = compressedData.vertexCount;
    int frameCount = compressedData.totalFrames;
    
    var result = new List<Vector3[]>();
    
    // Giải nén keyframe
    var keyframe = new Vector3[vertexCount];
    for (int v = 0; v < vertexCount; v++)
    {
        int index = v * 3;
        float x = compressedData.keyframeData[index] / 255.0f;
        float y = compressedData.keyframeData[index + 1] / 255.0f;
        float z = compressedData.keyframeData[index + 2] / 255.0f;
        
        keyframe[v] = DenormalizeVertex(new Vector3(x, y, z));
    }
    
    result.Add(keyframe);
    
    // Giải nén RLE
    byte[] decompressedDeltaData = DecompressRLE(compressedData.deltaFrameData);
    
    // Giải nén delta frames
    Vector3[] previousFrame = keyframe;
    
    for (int f = 1; f < frameCount; f++)
    {
        var currentFrame = new Vector3[vertexCount];
        
        for (int v = 0; v < vertexCount; v++)
        {
            int index = (f - 1) * vertexCount * 2 + v * 2;
            
            // Đọc RGB565
            ushort rgb565 = (ushort)((decompressedDeltaData[index] << 8) | decompressedDeltaData[index + 1]);
            
            // Chuyển đổi từ RGB565
            Vector3 normalizedDelta = ConvertFromRGB565(rgb565);
            
            // Khôi phục delta
            Vector3 delta = DenormalizeDelta(normalizedDelta);
            
            // Tính vị trí vertex
            currentFrame[v] = previousFrame[v] + delta;
        }
        
        result.Add(currentFrame);
        previousFrame = currentFrame;
    }
    
    return result;
}
```

### 6.3 Áp dụng RLE

```csharp
private byte[] ApplyRLE(byte[] data)
{
    if (data == null || data.Length == 0)
        return data;
    
    var result = new List<byte>();
    int i = 0;
    
    while (i < data.Length)
    {
        byte currentByte = data[i];
        int count = 1;
        
        // Đếm số lần lặp lại
        while (i + count < data.Length && data[i + count] == currentByte && count < 255)
        {
            count++;
        }
        
        // Nếu lặp lại nhiều lần, sử dụng RLE
        if (count >= 3)
        {
            result.Add(0xFF); // Marker
            result.Add((byte)count);
            result.Add(currentByte);
            i += count;
        }
        else
        {
            // Nếu không, lưu giá trị như bình thường
            result.Add(currentByte);
            i++;
        }
    }
    
    return result.ToArray();
}
```

### 6.4 Giải nén RLE

```csharp
private byte[] DecompressRLE(byte[] compressedData)
{
    if (compressedData == null || compressedData.Length == 0)
        return compressedData;
    
    var result = new List<byte>();
    int i = 0;
    
    while (i < compressedData.Length)
    {
        byte currentByte = compressedData[i];
        
        // Kiểm tra marker
        if (currentByte == 0xFF && i + 2 < compressedData.Length)
        {
            byte count = compressedData[i + 1];
            byte value = compressedData[i + 2];
            
            // Lặp lại giá trị
            for (int j = 0; j < count; j++)
            {
                result.Add(value);
            }
            
            i += 3;
        }
        else
        {
            // Giá trị bình thường
            result.Add(currentByte);
            i++;
        }
    }
    
    return result.ToArray();
}
```

## 7. Test Cases

### 7.1 Unit Tests

#### 7.1.1 Test RGB565 Conversion

```csharp
[Test]
public void TestRGB565Conversion()
{
    // Arrange
    Vector3 original = new Vector3(0.5f, 0.25f, 0.75f);
    
    // Act
    ushort rgb565 = ConvertToRGB565(original);
    Vector3 converted = ConvertFromRGB565(rgb565);
    
    // Assert
    Assert.AreEqual(original.x, converted.x, 0.05f); // Chấp nhận sai số 5%
    Assert.AreEqual(original.y, converted.y, 0.05f);
    Assert.AreEqual(original.z, converted.z, 0.05f);
}
```

> **Lưu ý**: Xem thêm 10 test case bổ sung cho phần này trong tài liệu [VAT_DeltaCompression_AdditionalTests.md](VAT_DeltaCompression_AdditionalTests.md#711-test-rgb565-conversion-bổ-sung).

#### 7.1.2 Test RLE Compression

```csharp
[Test]
public void TestRLECompression()
{
    // Arrange
    byte[] original = new byte[] { 1, 1, 1, 1, 2, 3, 4, 5, 5, 5, 5, 5 };
    
    // Act
    byte[] compressed = ApplyRLE(original);
    byte[] decompressed = DecompressRLE(compressed);
    
    // Assert
    Assert.AreEqual(original.Length, decompressed.Length);
    for (int i = 0; i < original.Length; i++)
    {
        Assert.AreEqual(original[i], decompressed[i]);
    }
}
```

> **Lưu ý**: Xem thêm 10 test case bổ sung cho phần này trong tài liệu [VAT_DeltaCompression_AdditionalTests.md](VAT_DeltaCompression_AdditionalTests.md#712-test-rle-compression-bổ-sung).

#### 7.1.3 Test Delta Compression

```csharp
[Test]
public void TestDeltaCompression()
{
    // Arrange
    var frames = new List<FrameInfo>();
    
    // Frame 1
    var frame1 = new FrameInfo();
    frame1.vertices = new Vector3[] 
    {
        new Vector3(0, 0, 0),
        new Vector3(1, 0, 0),
        new Vector3(0, 1, 0)
    };
    frames.Add(frame1);
    
    // Frame 2
    var frame2 = new FrameInfo();
    frame2.vertices = new Vector3[] 
    {
        new Vector3(0.1f, 0.1f, 0.1f),
        new Vector3(1.1f, 0.1f, 0.1f),
        new Vector3(0.1f, 1.1f, 0.1f)
    };
    frames.Add(frame2);
    
    // Act
    var compressor = new DeltaCompressor();
    var compressed = compressor.Compress(frames);
    var decompressed = compressor.Decompress(compressed);
    
    // Assert
    Assert.AreEqual(frames.Count, decompressed.Count);
    
    for (int f = 0; f < frames.Count; f++)
    {
        for (int v = 0; v < frames[f].vertices.Length; v++)
        {
            Assert.AreEqual(frames[f].vertices[v].x, decompressed[f][v].x, 0.05f);
            Assert.AreEqual(frames[f].vertices[v].y, decompressed[f][v].y, 0.05f);
            Assert.AreEqual(frames[f].vertices[v].z, decompressed[f][v].z, 0.05f);
        }
    }
}
```

> **Lưu ý**: Xem thêm 10 test case bổ sung cho phần này trong tài liệu [VAT_DeltaCompression_AdditionalTests.md](VAT_DeltaCompression_AdditionalTests.md#713-test-delta-compression-bổ-sung).

### 7.2 Performance Tests

#### 7.2.1 Test Compression Ratio

```csharp
[Test]
public void TestCompressionRatio()
{
    // Arrange
    var frames = GenerateTestFrames(30, 1000); // 30 frames, 1000 vertices
    
    // Act
    var compressor = new DeltaCompressor();
    var compressed = compressor.Compress(frames);
    
    // Calculate sizes
    int originalSize = frames.Count * frames[0].vertices.Length * 3 * 4; // float = 4 bytes
    int compressedSize = compressed.keyframeData.Length + compressed.deltaFrameData.Length;
    
    float ratio = (float)compressedSize / originalSize;
    
    // Assert
    Assert.Less(ratio, 0.3f); // Expect at least 70% reduction
}
```

#### 7.2.2 Test Compression Time

```csharp
[Test]
public void TestCompressionTime()
{
    // Arrange
    var frames = GenerateTestFrames(30, 1000); // 30 frames, 1000 vertices
    var compressor = new DeltaCompressor();
    
    // Act
    var stopwatch = new Stopwatch();
    stopwatch.Start();
    
    var compressed = compressor.Compress(frames);
    
    stopwatch.Stop();
    
    // Assert
    Assert.Less(stopwatch.ElapsedMilliseconds, 500); // Should compress in less than 500ms
}
```

#### 7.2.3 Test Decompression Time

```csharp
[Test]
public void TestDecompressionTime()
{
    // Arrange
    var frames = GenerateTestFrames(30, 1000); // 30 frames, 1000 vertices
    var compressor = new DeltaCompressor();
    var compressed = compressor.Compress(frames);
    
    // Act
    var stopwatch = new Stopwatch();
    stopwatch.Start();
    
    var decompressed = compressor.Decompress(compressed);
    
    stopwatch.Stop();
    
    // Assert
    Assert.Less(stopwatch.ElapsedMilliseconds, 100); // Should decompress in less than 100ms
}
```

## 8. Rủi ro và giảm thiểu

| Rủi ro | Mức độ | Tác động | Giảm thiểu |
|--------|--------|----------|------------|
| Giảm chất lượng animation | Trung bình | Cao | Điều chỉnh tham số nén, tăng độ chính xác của RGB565 |
| Tăng thời gian khởi tạo | Trung bình | Trung bình | Tối ưu thuật toán giải nén, sử dụng lazy loading |
| Không tương thích với phần cứng cũ | Thấp | Cao | Cung cấp fallback cho phần cứng không hỗ trợ |
| Lỗi khi giải nén | Trung bình | Cao | Kiểm thử kỹ lưỡng, xử lý ngoại lệ |
| Tăng thời gian tải | Trung bình | Trung bình | Giải nén bất đồng bộ, hiển thị loading screen |

## 9. Kết luận

Tính năng tối ưu dung lượng texture sử dụng Delta Compression với RGB24 cho keyframe và RGB565 cho delta frames, kết hợp với RLE, sẽ giúp giảm đáng kể dung lượng texture VAT. Dữ liệu sẽ được giải nén trong Awake thành texture bình thường và sử dụng shader hiện tại, giúp duy trì hiệu năng trong runtime.

Với thiết kế này, chúng ta có thể đạt được mục tiêu giảm dung lượng từ 400KB xuống dưới 100KB cho mỗi bot với 5 animation, đồng thời duy trì chất lượng animation và hiệu năng. 
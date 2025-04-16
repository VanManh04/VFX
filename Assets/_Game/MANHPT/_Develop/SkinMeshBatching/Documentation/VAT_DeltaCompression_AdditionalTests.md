# Test Cases Bổ Sung cho VAT Delta Compression

## 7.1.1 Test RGB565 Conversion (Bổ sung)

```csharp
[Test]
public void TestRGB565Conversion_ZeroValues()
{
    // Arrange
    Vector3 original = Vector3.zero;
    
    // Act
    ushort rgb565 = ConvertToRGB565(original);
    Vector3 converted = ConvertFromRGB565(rgb565);
    
    // Assert
    Assert.AreEqual(original.x, converted.x, 0.01f);
    Assert.AreEqual(original.y, converted.y, 0.01f);
    Assert.AreEqual(original.z, converted.z, 0.01f);
}

[Test]
public void TestRGB565Conversion_OneValues()
{
    // Arrange
    Vector3 original = Vector3.one;
    
    // Act
    ushort rgb565 = ConvertToRGB565(original);
    Vector3 converted = ConvertFromRGB565(rgb565);
    
    // Assert
    Assert.AreEqual(original.x, converted.x, 0.05f);
    Assert.AreEqual(original.y, converted.y, 0.05f);
    Assert.AreEqual(original.z, converted.z, 0.05f);
}

[Test]
public void TestRGB565Conversion_NegativeValues()
{
    // Arrange
    Vector3 original = new Vector3(-0.3f, -0.5f, -0.7f);
    
    // Act
    ushort rgb565 = ConvertToRGB565(original);
    Vector3 converted = ConvertFromRGB565(rgb565);
    
    // Assert
    Assert.AreEqual(original.x, converted.x, 0.05f);
    Assert.AreEqual(original.y, converted.y, 0.05f);
    Assert.AreEqual(original.z, converted.z, 0.05f);
}

[Test]
public void TestRGB565Conversion_MixedValues()
{
    // Arrange
    Vector3 original = new Vector3(-0.2f, 0.4f, -0.6f);
    
    // Act
    ushort rgb565 = ConvertToRGB565(original);
    Vector3 converted = ConvertFromRGB565(rgb565);
    
    // Assert
    Assert.AreEqual(original.x, converted.x, 0.05f);
    Assert.AreEqual(original.y, converted.y, 0.05f);
    Assert.AreEqual(original.z, converted.z, 0.05f);
}

[Test]
public void TestRGB565Conversion_SmallValues()
{
    // Arrange
    Vector3 original = new Vector3(0.01f, 0.02f, 0.03f);
    
    // Act
    ushort rgb565 = ConvertToRGB565(original);
    Vector3 converted = ConvertFromRGB565(rgb565);
    
    // Assert
    Assert.AreEqual(original.x, converted.x, 0.05f);
    Assert.AreEqual(original.y, converted.y, 0.05f);
    Assert.AreEqual(original.z, converted.z, 0.05f);
}

[Test]
public void TestRGB565Conversion_LargeValues()
{
    // Arrange
    Vector3 original = new Vector3(0.98f, 0.99f, 0.97f);
    
    // Act
    ushort rgb565 = ConvertToRGB565(original);
    Vector3 converted = ConvertFromRGB565(rgb565);
    
    // Assert
    Assert.AreEqual(original.x, converted.x, 0.05f);
    Assert.AreEqual(original.y, converted.y, 0.05f);
    Assert.AreEqual(original.z, converted.z, 0.05f);
}

[Test]
public void TestRGB565Conversion_RandomValues()
{
    // Arrange
    System.Random random = new System.Random(42); // Seed for reproducibility
    Vector3 original = new Vector3(
        (float)random.NextDouble(),
        (float)random.NextDouble(),
        (float)random.NextDouble()
    );
    
    // Act
    ushort rgb565 = ConvertToRGB565(original);
    Vector3 converted = ConvertFromRGB565(rgb565);
    
    // Assert
    Assert.AreEqual(original.x, converted.x, 0.05f);
    Assert.AreEqual(original.y, converted.y, 0.05f);
    Assert.AreEqual(original.z, converted.z, 0.05f);
}

[Test]
public void TestRGB565Conversion_ExtremeValues()
{
    // Arrange
    Vector3 original = new Vector3(0.0001f, 0.9999f, 0.5f);
    
    // Act
    ushort rgb565 = ConvertToRGB565(original);
    Vector3 converted = ConvertFromRGB565(rgb565);
    
    // Assert
    Assert.AreEqual(original.x, converted.x, 0.05f);
    Assert.AreEqual(original.y, converted.y, 0.05f);
    Assert.AreEqual(original.z, converted.z, 0.05f);
}

[Test]
public void TestRGB565Conversion_MultipleConversions()
{
    // Arrange
    Vector3 original = new Vector3(0.33f, 0.66f, 0.44f);
    
    // Act - Convert multiple times to test stability
    ushort rgb565_1 = ConvertToRGB565(original);
    Vector3 converted_1 = ConvertFromRGB565(rgb565_1);
    ushort rgb565_2 = ConvertToRGB565(converted_1);
    Vector3 converted_2 = ConvertFromRGB565(rgb565_2);
    
    // Assert
    Assert.AreEqual(rgb565_1, rgb565_2); // Should be identical
    Assert.AreEqual(converted_1.x, converted_2.x, 0.001f);
    Assert.AreEqual(converted_1.y, converted_2.y, 0.001f);
    Assert.AreEqual(converted_1.z, converted_2.z, 0.001f);
}
```

## 7.1.2 Test RLE Compression (Bổ sung)

```csharp
[Test]
public void TestRLECompression_EmptyArray()
{
    // Arrange
    byte[] original = new byte[0];
    
    // Act
    byte[] compressed = ApplyRLE(original);
    byte[] decompressed = DecompressRLE(compressed);
    
    // Assert
    Assert.AreEqual(0, decompressed.Length);
}

[Test]
public void TestRLECompression_SingleValue()
{
    // Arrange
    byte[] original = new byte[] { 42 };
    
    // Act
    byte[] compressed = ApplyRLE(original);
    byte[] decompressed = DecompressRLE(compressed);
    
    // Assert
    Assert.AreEqual(original.Length, decompressed.Length);
    Assert.AreEqual(original[0], decompressed[0]);
}

[Test]
public void TestRLECompression_NoRepeats()
{
    // Arrange
    byte[] original = new byte[] { 1, 2, 3, 4, 5 };
    
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

[Test]
public void TestRLECompression_AllRepeats()
{
    // Arrange
    byte[] original = new byte[] { 7, 7, 7, 7, 7, 7, 7, 7, 7, 7 };
    
    // Act
    byte[] compressed = ApplyRLE(original);
    byte[] decompressed = DecompressRLE(compressed);
    
    // Assert
    Assert.AreEqual(original.Length, decompressed.Length);
    for (int i = 0; i < original.Length; i++)
    {
        Assert.AreEqual(original[i], decompressed[i]);
    }
    // Check compression ratio
    Assert.Less(compressed.Length, original.Length);
}

[Test]
public void TestRLECompression_MixedPattern()
{
    // Arrange
    byte[] original = new byte[] { 1, 2, 3, 3, 3, 4, 5, 5, 5, 5, 5, 6 };
    
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

[Test]
public void TestRLECompression_AlternatingPattern()
{
    // Arrange
    byte[] original = new byte[] { 1, 2, 1, 2, 1, 2, 1, 2, 1, 2 };
    
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

[Test]
public void TestRLECompression_LongRepeats()
{
    // Arrange
    byte[] original = new byte[300];
    for (int i = 0; i < original.Length; i++)
    {
        original[i] = 123;
    }
    
    // Act
    byte[] compressed = ApplyRLE(original);
    byte[] decompressed = DecompressRLE(compressed);
    
    // Assert
    Assert.AreEqual(original.Length, decompressed.Length);
    for (int i = 0; i < original.Length; i++)
    {
        Assert.AreEqual(original[i], decompressed[i]);
    }
    // Check significant compression
    Assert.Less(compressed.Length, original.Length / 10);
}

[Test]
public void TestRLECompression_RepeatsAtBoundaries()
{
    // Arrange
    byte[] original = new byte[] { 5, 5, 5, 1, 2, 3, 4, 9, 9, 9, 9 };
    
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

[Test]
public void TestRLECompression_MarkerByteInData()
{
    // Arrange
    byte[] original = new byte[] { 0xFF, 1, 2, 3, 0xFF, 0xFF, 0xFF };
    
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

[Test]
public void TestRLECompression_RandomData()
{
    // Arrange
    System.Random random = new System.Random(42);
    byte[] original = new byte[1000];
    random.NextBytes(original);
    
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

## 7.1.3 Test Delta Compression (Bổ sung)

```csharp
[Test]
public void TestDeltaCompression_SingleFrame()
{
    // Arrange
    var frames = new List<FrameInfo>();
    
    // Only one frame
    var frame = new FrameInfo();
    frame.vertices = new Vector3[] 
    {
        new Vector3(0, 0, 0),
        new Vector3(1, 0, 0),
        new Vector3(0, 1, 0)
    };
    frames.Add(frame);
    
    // Act
    var compressor = new DeltaCompressor();
    var compressed = compressor.Compress(frames);
    var decompressed = compressor.Decompress(compressed);
    
    // Assert
    Assert.AreEqual(frames.Count, decompressed.Count);
    
    for (int v = 0; v < frames[0].vertices.Length; v++)
    {
        Assert.AreEqual(frames[0].vertices[v].x, decompressed[0][v].x, 0.05f);
        Assert.AreEqual(frames[0].vertices[v].y, decompressed[0][v].y, 0.05f);
        Assert.AreEqual(frames[0].vertices[v].z, decompressed[0][v].z, 0.05f);
    }
}

[Test]
public void TestDeltaCompression_NoMovement()
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
    
    // Frame 2 - identical to frame 1
    var frame2 = new FrameInfo();
    frame2.vertices = new Vector3[] 
    {
        new Vector3(0, 0, 0),
        new Vector3(1, 0, 0),
        new Vector3(0, 1, 0)
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
    
    // Check that delta frames are highly compressed
    Assert.Less(compressed.deltaFrameData.Length, 10); // Should be very small
}

[Test]
public void TestDeltaCompression_LargeMovement()
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
    
    // Frame 2 - large movement
    var frame2 = new FrameInfo();
    frame2.vertices = new Vector3[] 
    {
        new Vector3(10, 10, 10),
        new Vector3(11, 10, 10),
        new Vector3(10, 11, 10)
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
            Assert.AreEqual(frames[f].vertices[v].x, decompressed[f][v].x, 0.1f); // Larger tolerance for large movements
            Assert.AreEqual(frames[f].vertices[v].y, decompressed[f][v].y, 0.1f);
            Assert.AreEqual(frames[f].vertices[v].z, decompressed[f][v].z, 0.1f);
        }
    }
}

[Test]
public void TestDeltaCompression_MultipleFrames()
{
    // Arrange
    var frames = new List<FrameInfo>();
    
    // Create 10 frames with gradual movement
    for (int f = 0; f < 10; f++)
    {
        var frame = new FrameInfo();
        frame.vertices = new Vector3[] 
        {
            new Vector3(0 + f * 0.1f, 0 + f * 0.05f, 0 + f * 0.02f),
            new Vector3(1 + f * 0.1f, 0 + f * 0.05f, 0 + f * 0.02f),
            new Vector3(0 + f * 0.1f, 1 + f * 0.05f, 0 + f * 0.02f)
        };
        frames.Add(frame);
    }
    
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

[Test]
public void TestDeltaCompression_ManyVertices()
{
    // Arrange
    var frames = new List<FrameInfo>();
    
    // Create 2 frames with many vertices
    for (int f = 0; f < 2; f++)
    {
        var frame = new FrameInfo();
        var vertices = new Vector3[1000];
        
        for (int v = 0; v < 1000; v++)
        {
            vertices[v] = new Vector3(v * 0.01f + f * 0.1f, v * 0.005f + f * 0.05f, v * 0.002f + f * 0.02f);
        }
        
        frame.vertices = vertices;
        frames.Add(frame);
    }
    
    // Act
    var compressor = new DeltaCompressor();
    var compressed = compressor.Compress(frames);
    var decompressed = compressor.Decompress(compressed);
    
    // Assert
    Assert.AreEqual(frames.Count, decompressed.Count);
    
    // Check a sample of vertices
    for (int f = 0; f < frames.Count; f++)
    {
        for (int v = 0; v < frames[f].vertices.Length; v += 100) // Check every 100th vertex
        {
            Assert.AreEqual(frames[f].vertices[v].x, decompressed[f][v].x, 0.05f);
            Assert.AreEqual(frames[f].vertices[v].y, decompressed[f][v].y, 0.05f);
            Assert.AreEqual(frames[f].vertices[v].z, decompressed[f][v].z, 0.05f);
        }
    }
}

[Test]
public void TestDeltaCompression_RandomMovement()
{
    // Arrange
    var frames = new List<FrameInfo>();
    System.Random random = new System.Random(42);
    
    // Frame 1
    var frame1 = new FrameInfo();
    frame1.vertices = new Vector3[] 
    {
        new Vector3(0, 0, 0),
        new Vector3(1, 0, 0),
        new Vector3(0, 1, 0),
        new Vector3(1, 1, 0),
        new Vector3(0, 0, 1)
    };
    frames.Add(frame1);
    
    // Frame 2 - random movement
    var frame2 = new FrameInfo();
    frame2.vertices = new Vector3[5];
    for (int v = 0; v < 5; v++)
    {
        frame2.vertices[v] = frame1.vertices[v] + new Vector3(
            (float)random.NextDouble() * 0.2f - 0.1f,
            (float)random.NextDouble() * 0.2f - 0.1f,
            (float)random.NextDouble() * 0.2f - 0.1f
        );
    }
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

[Test]
public void TestDeltaCompression_NegativeCoordinates()
{
    // Arrange
    var frames = new List<FrameInfo>();
    
    // Frame 1
    var frame1 = new FrameInfo();
    frame1.vertices = new Vector3[] 
    {
        new Vector3(-1, -2, -3),
        new Vector3(-4, -5, -6),
        new Vector3(-7, -8, -9)
    };
    frames.Add(frame1);
    
    // Frame 2
    var frame2 = new FrameInfo();
    frame2.vertices = new Vector3[] 
    {
        new Vector3(-1.1f, -2.1f, -3.1f),
        new Vector3(-4.1f, -5.1f, -6.1f),
        new Vector3(-7.1f, -8.1f, -9.1f)
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

[Test]
public void TestDeltaCompression_MixedDirectionMovement()
{
    // Arrange
    var frames = new List<FrameInfo>();
    
    // Frame 1
    var frame1 = new FrameInfo();
    frame1.vertices = new Vector3[] 
    {
        new Vector3(0, 0, 0),
        new Vector3(1, 1, 1),
        new Vector3(2, 2, 2)
    };
    frames.Add(frame1);
    
    // Frame 2 - some vertices move up, some down
    var frame2 = new FrameInfo();
    frame2.vertices = new Vector3[] 
    {
        new Vector3(0.1f, 0.1f, 0.1f),    // Move up
        new Vector3(0.9f, 0.9f, 0.9f),    // Move down
        new Vector3(2.1f, 2.1f, 2.1f)     // Move up
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

[Test]
public void TestDeltaCompression_ZeroVertices()
{
    // Arrange
    var frames = new List<FrameInfo>();
    
    // Frame with no vertices
    var frame = new FrameInfo();
    frame.vertices = new Vector3[0];
    frames.Add(frame);
    
    // Act
    var compressor = new DeltaCompressor();
    var compressed = compressor.Compress(frames);
    var decompressed = compressor.Decompress(compressed);
    
    // Assert
    Assert.AreEqual(frames.Count, decompressed.Count);
    Assert.AreEqual(0, decompressed[0].Length);
}

[Test]
public void TestDeltaCompression_CompressionRatio()
{
    // Arrange
    var frames = new List<FrameInfo>();
    
    // Create 30 frames with small movements
    for (int f = 0; f < 30; f++)
    {
        var frame = new FrameInfo();
        frame.vertices = new Vector3[100];
        
        for (int v = 0; v < 100; v++)
        {
            // Small incremental changes
            frame.vertices[v] = new Vector3(
                v * 0.01f + f * 0.001f,
                v * 0.01f + f * 0.001f,
                v * 0.01f + f * 0.001f
            );
        }
        
        frames.Add(frame);
    }
    
    // Act
    var compressor = new DeltaCompressor();
    var compressed = compressor.Compress(frames);
    
    // Calculate sizes
    int originalSize = frames.Count * frames[0].vertices.Length * 3 * 4; // float = 4 bytes
    int compressedSize = compressed.keyframeData.Length + compressed.deltaFrameData.Length;
    
    float ratio = (float)compressedSize / originalSize;
    
    // Assert
    Assert.Less(ratio, 0.4f); // Expect at least 60% reduction
    
    // Also verify decompression works
    var decompressed = compressor.Decompress(compressed);
    Assert.AreEqual(frames.Count, decompressed.Count);
}
``` 
# Ragdoll với Verlet Integration và Data-Oriented Design

Một trong những tính năng quan trọng cần phát triển cho hệ thống Vertex Animation Texture (VAT) là tích hợp ragdoll dựa trên Verlet Integration và Data-Oriented Design (DOD). Phần này mô tả chi tiết về kiến trúc, cách tiếp cận và kế hoạch triển khai.

## Giới thiệu về Verlet Integration

Verlet Integration là một phương pháp số học để tích phân phương trình chuyển động trong mô phỏng vật lý. So với phương pháp Euler truyền thống, Verlet Integration có một số ưu điểm:

1. **Ổn định hơn**: Ít bị ảnh hưởng bởi các vấn đề số học
2. **Bảo toàn năng lượng tốt hơn**: Giúp mô phỏng trông tự nhiên hơn
3. **Dễ dàng áp dụng các ràng buộc (constraints)**: Phù hợp cho mô phỏng ragdoll

### Công thức cơ bản

```
x(t+dt) = 2*x(t) - x(t-dt) + a(t)*dt^2
```

Trong đó:
- `x(t+dt)`: Vị trí mới
- `x(t)`: Vị trí hiện tại
- `x(t-dt)`: Vị trí trước đó
- `a(t)`: Gia tốc
- `dt`: Bước thời gian

## Kiến trúc DOD cho Ragdoll

### 1. Kiến trúc tổng thể

Dựa vào phương pháp Verlet integration trong tài liệu của Jakobsen, chúng ta sẽ tạo một hệ thống ragdoll tối ưu:

```
RagdollSystem
├── RagdollManager (MonoBehaviour, quản lý và khởi tạo)
├── RagdollJobSystem (Job scheduling)
└── RagdollData (NativeArrays/NativeContainers)
    ├── Particles
    ├── Constraints
    └── Bodies
```

### 2. Cấu trúc dữ liệu tuân theo DOD

Thay vì sử dụng cấu trúc dữ liệu phân cấp truyền thống, chúng ta sẽ sử dụng cấu trúc dữ liệu phẳng phù hợp với DOD:

```csharp
// Structure of Arrays (SoA) thay vì Array of Structures (AoS)
public struct RagdollData
{
    // Particles
    public NativeArray<float3> CurrentPositions;
    public NativeArray<float3> PreviousPositions;
    public NativeArray<float> InverseMasses;
    public NativeArray<int> BodyIndices;
    
    // Constraints
    public NativeArray<int> ConstraintParticleA;
    public NativeArray<int> ConstraintParticleB;
    public NativeArray<float> ConstraintRestLengths;
    public NativeArray<byte> ConstraintTypes;
    
    // Bodies
    public NativeArray<int> BodyParticleStartIndices;
    public NativeArray<int> BodyParticleCounts;
    public NativeArray<int> BodyConstraintStartIndices;
    public NativeArray<int> BodyConstraintCounts;
    public NativeArray<int> BodyModelIndices;
    public NativeArray<byte> BodyStates;
    public NativeArray<RagdollLOD> BodyLODs;
}
```

Cấu trúc này tối ưu cho cache locality và memory bandwidth, đặc biệt quan trọng trên thiết bị di động.

### 3. Mô hình Stick Figure cho LOD

Chúng ta sẽ sử dụng mô hình stick figure đơn giản hóa để tối ưu hiệu năng, đặc biệt là cho các ragdoll ở xa:

```csharp
public enum RagdollLOD
{
    High,    // Mô hình đầy đủ: ~20-30 particles, ~30-40 constraints
    Medium,  // Mô hình đơn giản hóa: ~10-15 particles, ~15-20 constraints
    Low,     // Stick figure: ~6-8 particles, ~5-7 constraints
    Minimal  // Super simplified: ~4 particles, ~3 constraints
}
```

#### Cấu trúc LOD cho Stick Figure

```
// LOD High: ~20-30 particles
// [Head, Neck, Chest, Abdomen, Pelvis, L/R Shoulder, L/R Elbow, L/R Hand, L/R Hip, L/R Knee, L/R Foot, etc.]

// LOD Medium: ~10-15 particles
// [Head, Chest, Pelvis, L/R Shoulder, L/R Hand, L/R Hip, L/R Foot]

// LOD Low (Stick Figure): ~6-8 particles
// [Head, Chest, Pelvis, L/R Hand, L/R Foot]

// LOD Minimal: ~4 particles
// [Head, Chest, L/R Foot]
```

## Tối ưu hóa hiệu năng

### 1. Memory Pooling và Reuse

```csharp
public class RagdollManager : MonoBehaviour
{
    private RagdollData _ragdollData;
    private NativeList<int> _freeParticleIndices;
    private NativeList<int> _freeConstraintIndices;
    private NativeList<int> _freeBodyIndices;
    
    private void InitializeMemoryPools()
    {
        // Khởi tạo pools với kích thước phù hợp
        int maxConcurrentRagdolls = 20; // Điều chỉnh theo nhu cầu game
        
        // Tính toán kích thước tối đa cần thiết
        int maxParticles = maxConcurrentRagdolls * 30; // 30 particles per high LOD ragdoll
        int maxConstraints = maxConcurrentRagdolls * 40; // 40 constraints per high LOD ragdoll
        
        // Khởi tạo native containers
        _ragdollData = new RagdollData
        {
            CurrentPositions = new NativeArray<float3>(maxParticles, Allocator.Persistent),
            PreviousPositions = new NativeArray<float3>(maxParticles, Allocator.Persistent),
            InverseMasses = new NativeArray<float>(maxParticles, Allocator.Persistent),
            BodyIndices = new NativeArray<int>(maxParticles, Allocator.Persistent),
            
            // Khởi tạo các mảng khác tương tự
        };
        
        // Khởi tạo free lists để quản lý memory
        _freeParticleIndices = new NativeList<int>(maxParticles, Allocator.Persistent);
        _freeConstraintIndices = new NativeList<int>(maxConstraints, Allocator.Persistent);
        _freeBodyIndices = new NativeList<int>(maxConcurrentRagdolls, Allocator.Persistent);
        
        // Điền các free lists
        for (int i = maxParticles - 1; i >= 0; i--)
            _freeParticleIndices.Add(i);
        // Tương tự cho constraints và bodies
    }
    
    public int CreateRagdoll(Vector3[] jointPositions, Vector3 position, Quaternion rotation, RagdollLOD lod = RagdollLOD.High)
    {
        // Lấy body index từ free list
        int bodyIndex = _freeBodyIndices.Length > 0 ? _freeBodyIndices[_freeBodyIndices.Length - 1] : -1;
        if (bodyIndex < 0)
            return -1; // Không còn slot trống
            
        _freeBodyIndices.RemoveAt(_freeBodyIndices.Length - 1);
        
        // Tạo ragdoll với LOD phù hợp
        CreateRagdollWithLOD(bodyIndex, jointPositions, position, rotation, lod);
        
        return bodyIndex;
    }
    
    public void ReleaseRagdoll(int bodyIndex)
    {
        // Trả lại các indices vào free lists
        int particleStart = _ragdollData.BodyParticleStartIndices[bodyIndex];
        int particleCount = _ragdollData.BodyParticleCounts[bodyIndex];
        int constraintStart = _ragdollData.BodyConstraintStartIndices[bodyIndex];
        int constraintCount = _ragdollData.BodyConstraintCounts[bodyIndex];
        
        for (int i = 0; i < particleCount; i++)
            _freeParticleIndices.Add(particleStart + i);
            
        for (int i = 0; i < constraintCount; i++)
            _freeConstraintIndices.Add(constraintStart + i);
            
        _freeBodyIndices.Add(bodyIndex);
        
        // Đánh dấu body là không hoạt động
        _ragdollData.BodyStates[bodyIndex] = 0; // Inactive
    }
}
```

### 2. LOD Management

```csharp
public void UpdateLOD(float distanceFromCamera)
{
    for (int i = 0; i < _ragdollData.BodyStates.Length; i++)
    {
        if (_ragdollData.BodyStates[i] != 1) // Not active
            continue;
            
        // Tính khoảng cách từ camera đến ragdoll
        int particleStart = _ragdollData.BodyParticleStartIndices[i];
        float3 position = _ragdollData.CurrentPositions[particleStart]; // Lấy vị trí particle đầu tiên
        float distance = Vector3.Distance(Camera.main.transform.position, position);
        
        // Xác định LOD dựa trên khoảng cách
        RagdollLOD newLOD;
        if (distance < _highLODDistance)
            newLOD = RagdollLOD.High;
        else if (distance < _mediumLODDistance)
            newLOD = RagdollLOD.Medium;
        else if (distance < _lowLODDistance)
            newLOD = RagdollLOD.Low;
        else
            newLOD = RagdollLOD.Minimal;
            
        // Nếu LOD thay đổi, cập nhật ragdoll
        if (_ragdollData.BodyLODs[i] != newLOD)
            SwitchBodyLOD(i, newLOD);
    }
}

private void SwitchBodyLOD(int bodyIndex, RagdollLOD newLOD)
{
    // Lưu trữ vị trí của các particle quan trọng (head, chest, etc.)
    var keyPositions = ExtractKeyPositions(bodyIndex);
    
    // Giải phóng particles và constraints hiện tại
    ReleaseBodyResources(bodyIndex);
    
    // Tạo cấu trúc particle và constraint mới theo LOD
    ReconfigureBodyForLOD(bodyIndex, newLOD, keyPositions);
    
    // Cập nhật LOD
    _ragdollData.BodyLODs[bodyIndex] = newLOD;
}
```

### 3. Spatial Partitioning tối ưu

```csharp
[BurstCompile]
struct SpatialHashingJob : IJobParallelFor
{
    [ReadOnly] public NativeArray<float3> Positions;
    [WriteOnly] public NativeMultiHashMap<int, int>.ParallelWriter SpatialHash;
    public float CellSize;
    
    public void Execute(int index)
    {
        float3 pos = Positions[index];
        int hashKey = ComputeHashKey(pos, CellSize);
        SpatialHash.Add(hashKey, index);
    }
    
    private int ComputeHashKey(float3 position, float cellSize)
    {
        int x = (int)(position.x / cellSize);
        int y = (int)(position.y / cellSize);
        int z = (int)(position.z / cellSize);
        return (x * 73856093) ^ (y * 19349663) ^ (z * 83492791);
    }
}
```

### 4. Tối ưu hóa Constraint Solving

```csharp
[BurstCompile]
struct SolveConstraintsByTypeJob : IJob
{
    public NativeArray<float3> CurrentPositions;
    public NativeArray<float> InverseMasses;
    [ReadOnly] public NativeArray<int> ConstraintParticleA;
    [ReadOnly] public NativeArray<int> ConstraintParticleB;
    [ReadOnly] public NativeArray<float> ConstraintRestLengths;
    [ReadOnly] public NativeArray<byte> ConstraintTypes;
    [ReadOnly] public NativeArray<int> ConstraintIndices;
    public int IterationCount;
    
    public void Execute()
    {
        for (int iteration = 0; iteration < IterationCount; iteration++)
        {
            foreach (int constraintIndex in ConstraintIndices)
            {
                byte type = ConstraintTypes[constraintIndex];
                
                // Giải quyết constraint dựa trên loại
                switch (type)
                {
                    case 0: // Distance constraint
                        SolveDistanceConstraint(constraintIndex);
                        break;
                    case 1: // Angle constraint
                        SolveAngleConstraint(constraintIndex);
                        break;
                    // Các loại constraint khác
                }
            }
        }
    }
    
    private void SolveDistanceConstraint(int constraintIndex)
    {
        int particleA = ConstraintParticleA[constraintIndex];
        int particleB = ConstraintParticleB[constraintIndex];
        float restLength = ConstraintRestLengths[constraintIndex];
        
        float3 posA = CurrentPositions[particleA];
        float3 posB = CurrentPositions[particleB];
        
        float3 delta = posB - posA;
        float currentDistance = math.length(delta);
        
        if (currentDistance > 0.0001f)
        {
            float3 correction = delta * (1 - restLength / currentDistance);
            
            float invMassA = InverseMasses[particleA];
            float invMassB = InverseMasses[particleB];
            float totalInvMass = invMassA + invMassB;
            
            if (totalInvMass > 0)
            {
                float3 correctionA = correction * (invMassA / totalInvMass);
                float3 correctionB = correction * (-invMassB / totalInvMass);
                
                CurrentPositions[particleA] += correctionA;
                CurrentPositions[particleB] += correctionB;
            }
        }
    }
    
    // Các phương thức giải quyết constraint khác
}
```

### 5. Sleep Detection

```csharp
[BurstCompile]
struct SleepDetectionJob : IJobParallelFor
{
    [ReadOnly] public NativeArray<float3> CurrentPositions;
    [ReadOnly] public NativeArray<float3> PreviousPositions;
    [ReadOnly] public NativeArray<int> BodyParticleStartIndices;
    [ReadOnly] public NativeArray<int> BodyParticleCounts;
    public NativeArray<float> BodyKineticEnergies;
    public NativeArray<byte> BodyStates;
    public float DeltaTime;
    public float SleepThreshold;
    public int SleepCounterThreshold;
    public NativeArray<int> SleepCounters;
    
    public void Execute(int bodyIndex)
    {
        if (BodyStates[bodyIndex] != 1) // Not active
            return;
            
        int particleStart = BodyParticleStartIndices[bodyIndex];
        int particleCount = BodyParticleCounts[bodyIndex];
        
        float energy = 0;
        for (int i = 0; i < particleCount; i++)
        {
            int particleIndex = particleStart + i;
            float3 current = CurrentPositions[particleIndex];
            float3 previous = PreviousPositions[particleIndex];
            
            float3 velocity = (current - previous) / DeltaTime;
            energy += math.lengthsq(velocity);
        }
        
        energy /= particleCount;
        BodyKineticEnergies[bodyIndex] = energy;
        
        // Nếu năng lượng thấp, tăng sleep counter
        if (energy < SleepThreshold)
        {
            SleepCounters[bodyIndex]++;
            
            // Nếu đã ổn định trong nhiều frame liên tiếp, chuyển sang trạng thái sleep
            if (SleepCounters[bodyIndex] >= SleepCounterThreshold)
            {
                BodyStates[bodyIndex] = 2; // Sleep state
            }
        }
        else
        {
            // Reset sleep counter nếu có chuyển động
            SleepCounters[bodyIndex] = 0;
        }
    }
}
```

## Tích hợp với VAT

### 1. Chuyển đổi từ VAT sang Ragdoll

```csharp
public void EnableRagdoll(Vector3 impactForce, Vector3 impactPoint)
{
    // 1. Lấy vị trí hiện tại của các khớp từ VAT
    var jointPositions = _vatController.GetCurrentJointPositions();
    
    // 2. Xác định LOD dựa trên khoảng cách từ camera
    float distance = Vector3.Distance(Camera.main.transform.position, transform.position);
    RagdollLOD initialLOD = DetermineLODFromDistance(distance);
    
    // 3. Khởi tạo ragdoll với các vị trí này
    int bodyIndex = _ragdollManager.CreateRagdoll(jointPositions, transform.position, transform.rotation, initialLOD);
    
    // 4. Áp dụng lực tác động
    _ragdollManager.ApplyForce(bodyIndex, impactForce, impactPoint);
    
    // 5. Vô hiệu hóa VAT renderer và bật ragdoll renderer
    _vatController.enabled = false;
    _ragdollRenderer.enabled = true;
    
    // 6. Lưu trữ tham chiếu để có thể quay lại animation nếu cần
    _activeRagdollBodyIndex = bodyIndex;
}
```

### 2. Cập nhật Visual Mesh từ Ragdoll

```csharp
[BurstCompile]
public struct UpdateMeshFromRagdollJob : IJobParallelFor
{
    [ReadOnly] public NativeArray<float3> ParticlePositions;
    [ReadOnly] public NativeArray<int> ParticleToVertexMap;
    public NativeArray<float3> VertexPositions;
    
    public void Execute(int index)
    {
        int particleIndex = ParticleToVertexMap[index];
        if (particleIndex >= 0)
        {
            VertexPositions[index] = ParticlePositions[particleIndex];
        }
    }
}
```

## Công cụ Editor

Để dễ dàng thiết lập ragdoll, chúng ta sẽ tạo một editor tool:

```csharp
#if UNITY_EDITOR
[CustomEditor(typeof(RagdollSetup))]
public class RagdollSetupEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        
        RagdollSetup ragdollSetup = (RagdollSetup)target;
        
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Ragdoll Setup Tools", EditorStyles.boldLabel);
        
        if (GUILayout.Button("Auto-Generate Particles"))
        {
            ragdollSetup.AutoGenerateParticles();
        }
        
        if (GUILayout.Button("Auto-Generate Constraints"))
        {
            ragdollSetup.AutoGenerateConstraints();
        }
        
        if (GUILayout.Button("Generate LOD Variants"))
        {
            ragdollSetup.GenerateLODVariants();
        }
        
        if (GUILayout.Button("Test Ragdoll"))
        {
            ragdollSetup.TestRagdoll();
        }
    }
}
#endif
```

## Quy trình mô phỏng

### 1. Khởi tạo

```csharp
public void InitializeRagdoll(Vector3[] jointPositions, Vector3 position, Quaternion rotation, RagdollLOD lod)
{
    // 1. Tạo particles tại các vị trí khớp
    CreateParticlesForLOD(jointPositions, position, rotation, lod);
    
    // 2. Tạo constraints giữa các particles
    CreateConstraintsForLOD(lod);
    
    // 3. Thiết lập các tham số vật lý
    SetupPhysicsParameters(lod);
}
```

### 2. Cập nhật mỗi frame

```csharp
public void UpdateRagdolls(float deltaTime)
{
    // 1. Tích phân vị trí (Verlet Integration)
    VerletIntegrationJob verletJob = new VerletIntegrationJob
    {
        CurrentPositions = _ragdollData.CurrentPositions,
        PreviousPositions = _ragdollData.PreviousPositions,
        InverseMasses = _ragdollData.InverseMasses,
        BodyStates = _ragdollData.BodyStates,
        Gravity = _gravity,
        DeltaTime = deltaTime
    };
    JobHandle verletHandle = verletJob.Schedule(_ragdollData.CurrentPositions.Length, 64);
    
    // 2. Cập nhật spatial hash
    NativeMultiHashMap<int, int> spatialHash = new NativeMultiHashMap<int, int>(_ragdollData.CurrentPositions.Length, Allocator.TempJob);
    SpatialHashingJob hashingJob = new SpatialHashingJob
    {
        Positions = _ragdollData.CurrentPositions,
        SpatialHash = spatialHash.AsParallelWriter(),
        CellSize = _collisionCellSize
    };
    JobHandle hashingHandle = hashingJob.Schedule(_ragdollData.CurrentPositions.Length, 64, verletHandle);
    
    // 3. Phát hiện và xử lý va chạm
    CollisionDetectionJob collisionJob = new CollisionDetectionJob
    {
        CurrentPositions = _ragdollData.CurrentPositions,
        InverseMasses = _ragdollData.InverseMasses,
        BodyStates = _ragdollData.BodyStates,
        SpatialHash = spatialHash,
        StaticColliders = _staticColliders,
        CellSize = _collisionCellSize
    };
    JobHandle collisionHandle = collisionJob.Schedule(hashingHandle);
    
    // 4. Giải quyết các ràng buộc
    for (int i = 0; i < _activeBodyCount; i++)
    {
        if (_ragdollData.BodyStates[i] != 1) // Not active
            continue;
            
        RagdollLOD lod = _ragdollData.BodyLODs[i];
        int iterationCount = GetConstraintIterationsForLOD(lod);
        
        int constraintStart = _ragdollData.BodyConstraintStartIndices[i];
        int constraintCount = _ragdollData.BodyConstraintCounts[i];
        
        NativeArray<int> constraintIndices = new NativeArray<int>(constraintCount, Allocator.TempJob);
        for (int j = 0; j < constraintCount; j++)
            constraintIndices[j] = constraintStart + j;
            
        SolveConstraintsByTypeJob constraintJob = new SolveConstraintsByTypeJob
        {
            CurrentPositions = _ragdollData.CurrentPositions,
            InverseMasses = _ragdollData.InverseMasses,
            ConstraintParticleA = _ragdollData.ConstraintParticleA,
            ConstraintParticleB = _ragdollData.ConstraintParticleB,
            ConstraintRestLengths = _ragdollData.ConstraintRestLengths,
            ConstraintTypes = _ragdollData.ConstraintTypes,
            ConstraintIndices = constraintIndices,
            IterationCount = iterationCount
        };
        
        collisionHandle = constraintJob.Schedule(collisionHandle);
        constraintIndices.Dispose(collisionHandle);
    }
    
    // 5. Phát hiện sleep state
    SleepDetectionJob sleepJob = new SleepDetectionJob
    {
        CurrentPositions = _ragdollData.CurrentPositions,
        PreviousPositions = _ragdollData.PreviousPositions,
        BodyParticleStartIndices = _ragdollData.BodyParticleStartIndices,
        BodyParticleCounts = _ragdollData.BodyParticleCounts,
        BodyKineticEnergies = _bodyKineticEnergies,
        BodyStates = _ragdollData.BodyStates,
        DeltaTime = deltaTime,
        SleepThreshold = _sleepThreshold,
        SleepCounterThreshold = _sleepCounterThreshold,
        SleepCounters = _sleepCounters
    };
    JobHandle sleepHandle = sleepJob.Schedule(_activeBodyCount, 1, collisionHandle);
    
    // 6. Cập nhật mesh
    UpdateMeshesFromRagdolls(sleepHandle);
    
    // Đảm bảo tất cả jobs hoàn thành
    sleepHandle.Complete();
    
    // Giải phóng bộ nhớ tạm thời
    spatialHash.Dispose();
}
```

## Kế hoạch triển khai

### Giai đoạn 1: Prototype

1. Triển khai cấu trúc dữ liệu DOD cơ bản
2. Triển khai Verlet Integration và constraint solving
3. Tạo mô hình stick figure đơn giản
4. Thử nghiệm với một số ít ragdoll

### Giai đoạn 2: Tích hợp với VAT

1. Tạo cơ chế chuyển đổi từ animation VAT sang ragdoll
2. Triển khai hệ thống LOD cho stick figure
3. Tích hợp với hệ thống collider tùy chỉnh
4. Tối ưu hóa memory pooling và reuse

### Giai đoạn 3: Tối ưu hóa

1. Triển khai spatial partitioning
2. Tối ưu hóa constraint solving
3. Triển khai sleep detection
4. Profiling và tối ưu hóa bottlenecks

### Giai đoạn 4: Hoàn thiện

1. Tạo editor tools để dễ dàng thiết lập ragdoll
2. Tối ưu hóa memory bandwidth với SoA
3. Tạo API dễ sử dụng
4. Tạo documentation đầy đủ

## Tài liệu tham khảo

- [Verlet Integration](https://en.wikipedia.org/wiki/Verlet_integration)
- [Advanced Character Physics by Thomas Jakobsen](https://www.cs.cmu.edu/afs/cs/academic/class/15462-s13/www/lecture_slides/Jakobsen.pdf)
- [Position Based Dynamics](https://matthias-research.github.io/pages/publications/posBasedDyn.pdf)
- [Unity Job System](https://docs.unity3d.com/Manual/JobSystem.html)
- [Unity Burst Compiler](https://docs.unity3d.com/Packages/com.unity.burst@1.6/manual/index.html)
- [Data-Oriented Design for Unity](https://unity.com/how-to/programming-optimized-3d-games-memory-oriented-design) 
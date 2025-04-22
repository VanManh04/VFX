using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace SplineMesh
{
    public class MeshDeformer : IDisposable
    {
        private NativeArray<float3> originalVertices;
        private NativeArray<float3> deformedVertices;
        private int                 vertexCount;
        private bool                isInitialized = false;

        /// <summary>
        /// Khởi tạo MeshDeformer với một mesh
        /// </summary>
        public MeshDeformer(Mesh mesh)
        {
            vertexCount = mesh.vertexCount;
        }

        /// <summary>
        /// Khởi tạo buffers và sao chép dữ liệu ban đầu từ mesh
        /// </summary>
        public unsafe void Initialize(Mesh mesh)
        {
            if (isInitialized)
            {
                Dispose();
            }

            // Khởi tạo NativeArrays để lưu trữ đỉnh
            originalVertices = new NativeArray<float3>(vertexCount, Allocator.Persistent);
            deformedVertices = new NativeArray<float3>(vertexCount, Allocator.Persistent);

            // Lấy vertices từ mesh
            Vector3[] vertices = mesh.vertices;

            // Copy dữ liệu sử dụng unsafe pointer để tối ưu hiệu năng
            fixed (Vector3* verticesPtr = vertices)
            {
                UnsafeUtility.MemCpy(
                    originalVertices.GetUnsafePtr(),
                    verticesPtr,
                    vertexCount * UnsafeUtility.SizeOf<Vector3>()
                );
            }

            // Copy dữ liệu ban đầu sang deformedVertices để bắt đầu
            UnsafeUtility.MemCpy(
                deformedVertices.GetUnsafePtr(),
                originalVertices.GetUnsafeReadOnlyPtr(),
                vertexCount * UnsafeUtility.SizeOf<float3>()
            );

            isInitialized = true;
        }

        /// <summary>
        /// Biến dạng mesh dọc theo spline
        /// </summary>
        public void DeformAlongSpline(
            NativeArray<float3> splinePoints,
            NativeArray<float3> splineTangents,
            NativeArray<float3> splineNormals,
            float               dropParameter,
            float               compressionFactor,
            float               length,
            float               scale)
        {
            if (!isInitialized)
            {
                Debug.LogError("MeshDeformer is not initialized. Call Initialize() first.");
                return;
            }

            // Tạo job biến dạng mesh
            DeformMeshAlongSplineJob deformJob = new DeformMeshAlongSplineJob
            {
                originalVertices  = originalVertices,
                splinePoints      = splinePoints,
                splineTangents    = splineTangents,
                splineNormals     = splineNormals,
                dropParameter     = dropParameter,
                compressionFactor = compressionFactor,
                length            = length,
                scale             = scale,
                deformedVertices  = deformedVertices
            };

            // Lên lịch và thực thi job
            JobHandle jobHandle = deformJob.Schedule(vertexCount, 64);
            jobHandle.Complete();
        }

        /// <summary>
        /// Áp dụng nén mesh theo hướng xác định
        /// </summary>
        public unsafe void ApplyCompression(float compressionAmount, Vector3 compressionDirection)
        {
            if (!isInitialized)
            {
                Debug.LogError("MeshDeformer is not initialized. Call Initialize() first.");
                return;
            }

            // Tạm thời lưu trữ vertices đã biến dạng trước khi nén
            NativeArray<float3> tempVertices = new NativeArray<float3>(vertexCount, Allocator.TempJob);
            UnsafeUtility.MemCpy(
                tempVertices.GetUnsafePtr(),
                deformedVertices.GetUnsafeReadOnlyPtr(),
                vertexCount * UnsafeUtility.SizeOf<float3>()
            );

            // Tạo job nén mesh
            CompressMeshJob compressJob = new CompressMeshJob
            {
                inputVertices     = tempVertices,
                compressionAmount = compressionAmount,
                compressionDirection =
                    new float3(compressionDirection.x, compressionDirection.y, compressionDirection.z),
                outputVertices = deformedVertices
            };

            // Lên lịch và thực thi job
            JobHandle jobHandle = compressJob.Schedule(vertexCount, 64);
            jobHandle.Complete();

            // Giải phóng tài nguyên tạm thời
            tempVertices.Dispose();
        }

        /// <summary>
        /// Áp dụng các thay đổi vào mesh
        /// </summary>
        public unsafe void ApplyDeformation(Mesh mesh)
        {
            if (!isInitialized)
            {
                Debug.LogError("MeshDeformer is not initialized. Call Initialize() first.");
                return;
            }

            // Tối ưu hóa với unsafe pointers
            Vector3[] vertices = new Vector3[vertexCount];
            fixed (Vector3* verticesPtr = vertices)
            {
                UnsafeUtility.MemCpy(
                    verticesPtr,
                    deformedVertices.GetUnsafeReadOnlyPtr(),
                    vertexCount * UnsafeUtility.SizeOf<Vector3>()
                );
            }

            // Áp dụng vertices mới vào mesh
            mesh.vertices = vertices;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
        }

        /// <summary>
        /// Phiên bản tối ưu hơn, truy cập trực tiếp vào Mesh API
        /// </summary>
        public unsafe void FastApplyDeformation(Mesh mesh)
        {
            if (!isInitialized)
            {
                Debug.LogError("MeshDeformer is not initialized. Call Initialize() first.");
                return;
            }

            // Tạo buffer tạm thời để cập nhật mesh
            var verticesBuffer = new NativeArray<Vector3>(vertexCount, Allocator.Temp);

            // Copy dữ liệu từ deformedVertices sang verticesBuffer
            UnsafeUtility.MemCpy(
                verticesBuffer.GetUnsafePtr(),
                deformedVertices.GetUnsafeReadOnlyPtr(),
                vertexCount * UnsafeUtility.SizeOf<Vector3>()
            );

            // Cập nhật mesh một cách hiệu quả
            mesh.SetVertices(verticesBuffer);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            // Giải phóng tài nguyên tạm thời
            verticesBuffer.Dispose();
        }

        /// <summary>
        /// Reset mesh về hình dạng ban đầu
        /// </summary>
        public unsafe void ResetMesh(Mesh mesh)
        {
            if (!isInitialized)
            {
                Debug.LogError("MeshDeformer is not initialized. Call Initialize() first.");
                return;
            }

            // Copy vertices gốc vào deformed vertices
            UnsafeUtility.MemCpy(
                deformedVertices.GetUnsafePtr(),
                originalVertices.GetUnsafeReadOnlyPtr(),
                vertexCount * UnsafeUtility.SizeOf<float3>()
            );

            // Áp dụng vào mesh
            ApplyDeformation(mesh);
        }

        /// <summary>
        /// Giải phóng tài nguyên native
        /// </summary>
        public void Dispose()
        {
            if (originalVertices.IsCreated)
            {
                originalVertices.Dispose();
            }

            if (deformedVertices.IsCreated)
            {
                deformedVertices.Dispose();
            }

            isInitialized = false;
        }

        #region Jobs

        /// <summary>
        /// Job biến dạng mesh dọc theo spline
        /// </summary>
        [BurstCompile]
        public struct DeformMeshAlongSplineJob : IJobParallelFor
        {
            [ReadOnly] public NativeArray<float3> originalVertices;
            [ReadOnly] public NativeArray<float3> splinePoints;
            [ReadOnly] public NativeArray<float3> splineTangents;
            [ReadOnly] public NativeArray<float3> splineNormals;
            [ReadOnly] public float               dropParameter;     // t từ 0->1
            [ReadOnly] public float               compressionFactor; // Hệ số nén (1.0 = không nén)
            [ReadOnly] public float               length;
            [ReadOnly] public float               scale;

            [NativeDisableParallelForRestriction] public NativeArray<float3> deformedVertices;

            public unsafe void Execute(int index)
            {
                // Sử dụng con trỏ để truy cập nhanh hơn
                float3* origPtr    = (float3*)originalVertices.GetUnsafeReadOnlyPtr();
                float3* splinePtr  = (float3*)splinePoints.GetUnsafeReadOnlyPtr();
                float3* tangentPtr = (float3*)splineTangents.GetUnsafeReadOnlyPtr();
                float3* normalPtr  = (float3*)splineNormals.GetUnsafeReadOnlyPtr();
                float3* defPtr     = (float3*)deformedVertices.GetUnsafePtr();

                // Lấy vị trí ban đầu của đỉnh
                float3 vertex = origPtr[index];

                // Tìm điểm gần nhất trên spline dựa vào tham số dropParameter

                int localIndex = math.min((int)(dropParameter * (splinePoints.Length - 1)), splinePoints.Length - 1);

                float3 localPoint    = splinePtr[localIndex];
                float3 localTangent  = tangentPtr[localIndex];
                float3 localNormal   = normalPtr[localIndex];
                float3 localBinormal = math.cross(localNormal, localTangent);

                float3x3 localTransformMatrix = new float3x3(
                    localBinormal,
                    localNormal,
                    localTangent
                );

                var vertexLocal = localPoint + math.mul(localTransformMatrix, vertex);

                int splineIndex = 0;

                for (var i = 0; i < splinePoints.Length; i++)
                {
                    if (i == splineIndex)
                        continue;

                    float distance = math.distance(vertexLocal, splinePtr[i]);
                    if (distance < math.distance(vertexLocal, splinePtr[splineIndex]))
                    {
                        splineIndex = i;
                    }
                }

                float3 splinePoint = splinePtr[splineIndex];
                float3 tangent     = tangentPtr[splineIndex];
                float3 normal      = normalPtr[splineIndex];
                float3 binormal    = math.cross(normal, tangent);

                // Tạo ma trận biến đổi từ không gian local sang không gian spline
                float3x3 transformMatrix = new float3x3(
                    binormal,
                    normal,
                    tangent
                );

                // Áp dụng hiệu ứng nén dọc theo trục di chuyển

                var compressionMultiplier = math.lerp(length, compressionFactor, dropParameter);
                vertex.z *= compressionMultiplier * scale;
                var expandMultiplier = math.sqrt(1 / compressionMultiplier);
                vertex.x *= expandMultiplier * scale;
                vertex.y *= expandMultiplier * scale;


                // Biến đổi đỉnh vào không gian spline
                float3 transformedVertex = math.mul(transformMatrix, vertex);

                // Đặt đỉnh vào vị trí mới dọc theo spline
                defPtr[index] = splinePoint + transformedVertex;
            }
        }

        /// <summary>
        /// Job nén mesh theo hướng xác định
        /// </summary>
        [BurstCompile]
        public struct CompressMeshJob : IJobParallelFor
        {
            [ReadOnly] public NativeArray<float3> inputVertices;
            [ReadOnly] public float               compressionAmount;    // 0.0 = không nén, 1.0 = nén tối đa
            [ReadOnly] public float3              compressionDirection; // Hướng nén

            [NativeDisableParallelForRestriction] public NativeArray<float3> outputVertices;

            public unsafe void Execute(int index)
            {
                float3* inPtr  = (float3*)inputVertices.GetUnsafeReadOnlyPtr();
                float3* outPtr = (float3*)outputVertices.GetUnsafePtr();

                float3 vertex              = inPtr[index];
                float3 normalizedDirection = math.normalize(compressionDirection);

                // Tính toán hệ số nén dựa vào vị trí trên mesh
                float distanceInCompressionDirection = math.dot(vertex, normalizedDirection);
                float compressionFactor = 1.0f - (compressionAmount * math.saturate(distanceInCompressionDirection));

                // Áp dụng biến dạng
                float3 offset = normalizedDirection * distanceInCompressionDirection * (1.0f - compressionFactor);
                outPtr[index] = vertex - offset;
            }
        }

        #endregion
    }
}
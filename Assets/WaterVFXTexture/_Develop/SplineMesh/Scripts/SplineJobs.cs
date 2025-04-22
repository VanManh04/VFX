using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace SplineMesh 
{
    public static class SplineJobs
    {
        /// <summary>
        /// Job tính toán các điểm trên spline dựa vào công thức Catmull-Rom
        /// </summary>
        [BurstCompile]
        public struct CalculateSplinePointsJob : IJobParallelFor
        {
            [ReadOnly] public NativeArray<float3> controlPoints;
            [ReadOnly] public float tension;
            [ReadOnly] public NativeArray<float> tValues;
            [ReadOnly] public bool closedLoop;
            
            [NativeDisableParallelForRestriction]
            public NativeArray<float3> results;
            
            public unsafe void Execute(int index)
            {
                float t = tValues[index];
                
                // Sử dụng con trỏ trực tiếp để truy cập dữ liệu nhanh hơn
                float3* controlPointsPtr = (float3*)controlPoints.GetUnsafeReadOnlyPtr();
                float3* resultsPtr = (float3*)results.GetUnsafePtr();
                
                int numPoints = controlPoints.Length;
                
                // Xác định các điểm điều khiển cần thiết cho đoạn spline hiện tại
                if (closedLoop)
                {
                    float segmentT = t * numPoints;
                    int segment = (int)math.floor(segmentT);
                    float localT = segmentT - segment;
                    
                    segment %= numPoints;
                    
                    int p0 = (segment - 1 + numPoints) % numPoints;
                    int p1 = segment;
                    int p2 = (segment + 1) % numPoints;
                    int p3 = (segment + 2) % numPoints;
                    
                    resultsPtr[index] = CalculateCatmullRomPoint(
                        controlPointsPtr[p0],
                        controlPointsPtr[p1],
                        controlPointsPtr[p2],
                        controlPointsPtr[p3],
                        localT,
                        tension
                    );
                }
                else
                {
                    // Với spline mở, chúng ta cần ít nhất 4 điểm để tính
                    if (numPoints < 4)
                    {
                        resultsPtr[index] = float3.zero;
                        return;
                    }
                    
                    // Tính điểm theo công thức Catmull-Rom thông thường
                    float segmentT = t * (numPoints - 3); // Số đoạn thực sự = numPoints - 3
                    int segment = (int)math.floor(segmentT);
                    float localT = segmentT - segment;
                    
                    if(segment >= numPoints - 3) return;
                    
                    // Lấy 4 điểm điều khiển cần thiết
                    float3 p0 = controlPointsPtr[segment];
                    float3 p1 = controlPointsPtr[segment + 1];
                    float3 p2 = controlPointsPtr[segment + 2];
                    float3 p3 = controlPointsPtr[segment + 3];
                    
                    resultsPtr[index] = CalculateCatmullRomPoint(
                        p0, p1, p2, p3,
                        localT,
                        tension
                    );
                }
            }
            
            private float3 CalculateCatmullRomPoint(float3 p0, float3 p1, float3 p2, float3 p3, float t, float tension)
            {
                // Công thức Catmull-Rom chính xác
                float t2 = t * t;
                float t3 = t2 * t;
                
                // Hệ số α (alpha) trong công thức Catmull-Rom, thường từ 0 đến 1
                // tension = 0 cho Catmull-Rom tiêu chuẩn, 0.5 cho Centripetal, 1.0 cho Chordal
                float alpha = 1f - tension; // Điều chỉnh theo tension parameter
                
                // Ma trận cơ sở Catmull-Rom
                float3 b1 = alpha * (p2 - p0) / 2f;
                float3 b2 = alpha * (p3 - p1) / 2f;
                
                // Hệ số Hermite
                float3 h1 = 2f * t3 - 3f * t2 + 1f;          // Hệ số cho P1
                float3 h2 = -2f * t3 + 3f * t2;              // Hệ số cho P2
                float3 h3 = t3 - 2f * t2 + t;                // Hệ số cho vận tốc tại P1
                float3 h4 = t3 - t2;                         // Hệ số cho vận tốc tại P2
                
                // Phép nội suy Hermite
                return (h1 * p1) + (h2 * p2) + (h3 * b1) + (h4 * b2);
            }
        }

        /// <summary>
        /// Job tính toán các vector tiếp tuyến tại mỗi điểm trên spline
        /// </summary>
        [BurstCompile]
        public struct CalculateSplineTangentsJob : IJobParallelFor
        {
            [ReadOnly] public NativeArray<float3> splinePoints;
            
            [NativeDisableParallelForRestriction]
            public NativeArray<float3> tangents;
            
            public unsafe void Execute(int index)
            {
                // Sử dụng con trỏ để truy cập nhanh hơn
                float3* pointsPtr = (float3*)splinePoints.GetUnsafeReadOnlyPtr();
                float3* tangentsPtr = (float3*)tangents.GetUnsafePtr();
                
                int numPoints = splinePoints.Length;
                
                if (index == 0)
                {
                    // Điểm đầu tiên - sử dụng sai phân tiến
                    tangentsPtr[index] = math.normalize(pointsPtr[index + 1] - pointsPtr[index]);
                }
                else if (index == numPoints - 1)
                {
                    // Điểm cuối cùng - sử dụng sai phân lùi
                    tangentsPtr[index] = math.normalize(pointsPtr[index] - pointsPtr[index - 1]);
                }
                else
                {
                    // Các điểm giữa - sử dụng sai phân trung tâm
                    tangentsPtr[index] = math.normalize(pointsPtr[index + 1] - pointsPtr[index - 1]);
                }
            }
        }

        /// <summary>
        /// Job tính toán các vector pháp tuyến tại mỗi điểm trên spline
        /// </summary>
        [BurstCompile]
        public struct CalculateSplineNormalsJob : IJobParallelFor
        {
            [ReadOnly] public NativeArray<float3> tangents;
            [ReadOnly] public float3 upVector;
            
            [NativeDisableParallelForRestriction]
            public NativeArray<float3> normals;
            
            public unsafe void Execute(int index)
            {
                float3* tangentsPtr = (float3*)tangents.GetUnsafeReadOnlyPtr();
                float3* normalsPtr = (float3*)normals.GetUnsafePtr();
                
                float3 tangent = tangentsPtr[index];
                float3 up = upVector;
                
                // Xử lý trường hợp tangent gần với up quá mức
                if (math.abs(math.dot(tangent, up)) > 0.999f)
                {
                    up = new float3(1, 0, 0);
                }
                
                // Tính normal (vuông góc với tangent)
                float3 normal = math.normalize(math.cross(math.cross(tangent, up), tangent));
                normalsPtr[index] = normal;
            }
        }

        /// <summary>
        /// Struct chứa dữ liệu spline được tính toán
        /// </summary>
        public struct SplineData
        {
            public NativeArray<float3> points;
            public NativeArray<float3> tangents;
            public NativeArray<float3> normals;
            public NativeArray<float> tValues;

            public void Dispose()
            {
                if (points.IsCreated) points.Dispose();
                if (tangents.IsCreated) tangents.Dispose();
                if (normals.IsCreated) normals.Dispose();
                if (tValues.IsCreated) tValues.Dispose();
            }
        }

        /// <summary>
        /// Tính toán dữ liệu spline sử dụng JobSystem và Burst Compiler
        /// </summary>
        public static SplineData CalculateSpline(NativeArray<float3> controlPoints, float tension, bool closedLoop, int resolution, Allocator allocator)
        {
            SplineData splineData = new SplineData
            {
                points = new NativeArray<float3>(closedLoop ? resolution : resolution - 3, allocator),
                tangents = new NativeArray<float3>(resolution, allocator),
                normals = new NativeArray<float3>(resolution, allocator),
                tValues = new NativeArray<float>(resolution, allocator)
            };

            // Khởi tạo giá trị t từ 0 đến 1
            for (int i = 0; i < resolution; i++)
            {
                splineData.tValues[i] = (float)i / (resolution - 1);
            }

            // Tính các điểm trên spline
            var pointsJob = new CalculateSplinePointsJob
            {
                controlPoints = controlPoints,
                tension = tension,
                tValues = splineData.tValues,
                closedLoop = closedLoop,
                results = splineData.points
            };

            // Tính các vector tiếp tuyến
            var tangentsJob = new CalculateSplineTangentsJob
            {
                splinePoints = splineData.points,
                tangents = splineData.tangents
            };

            // Tính các vector pháp tuyến
            var normalsJob = new CalculateSplineNormalsJob
            {
                tangents = splineData.tangents,
                upVector = new float3(0, 1, 0),
                normals = splineData.normals
            };

            // Thực thi các job theo thứ tự
            JobHandle pointsHandle = pointsJob.Schedule(resolution, 64);
            JobHandle tangentsHandle = tangentsJob.Schedule(resolution, 64, pointsHandle);
            JobHandle normalsHandle = normalsJob.Schedule(resolution, 64, tangentsHandle);
            
            // Chờ hoàn thành tất cả các job
            normalsHandle.Complete();

            return splineData;
        }
    }
} 
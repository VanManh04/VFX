using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace SplineMesh
{
    public class SplineController : MonoBehaviour
    {
        [SerializeField] private SplineData splineData;
        [SerializeField] private int splineResolution = 100;
        [SerializeField] private bool autoUpdateSpline = true;
        
        private SplineJobs.SplineData cachedSplineData;
        private NativeArray<float3> controlPointsArray;
        private bool isDirty = true;

        public int SplineResolution => splineResolution;
        
        // Events
        public event Action OnSplineChanged;
        
        private void Awake()
        {
            if (splineData == null)
            {
                // Tạo SplineData trống
                splineData = ScriptableObject.CreateInstance<SplineData>();
                
                // Không tự động tạo các điểm mặc định nữa
            }
            
            // Tạo native array từ control points
            CreateControlPointsArray();
        }
        
        private void OnEnable()
        {
            UpdateSplineData();
        }
        
        private void OnDisable()
        {
            DisposeNativeArrays();
        }
        
        private void OnDestroy()
        {
            DisposeNativeArrays();
        }
        
        #region Public Methods
        
        /// <summary>
        /// Lấy dữ liệu spline đã được tính toán
        /// </summary>
        public SplineJobs.SplineData GetSplineData(int resolution = -1)
        {
            // Nếu yêu cầu độ phân giải khác và đang có cache thì giải phóng cache cũ
            if (resolution > 0 && resolution != splineResolution && cachedSplineData.points.IsCreated)
            {
                cachedSplineData.Dispose();
                splineResolution = resolution;
                isDirty = true;
            }
            
            // Cập nhật spline nếu cần
            if (isDirty || !cachedSplineData.points.IsCreated)
            {
                UpdateSplineData();
            }
            
            return cachedSplineData;
        }
        
        /// <summary>
        /// Lấy vị trí tại tham số t trên spline (0 ≤ t ≤ 1)
        /// </summary>
        public Vector3 GetPointAt(float t)
        {
            if (isDirty || !cachedSplineData.points.IsCreated)
            {
                UpdateSplineData();
            }
            
            t = Mathf.Clamp01(t);
            int index = Mathf.FloorToInt(t * (splineResolution - 3));
            index = Mathf.Clamp(index, 0, splineResolution - 4);
            return cachedSplineData.points[index];
        }
        
        /// <summary>
        /// Lấy vector tiếp tuyến tại tham số t (0 ≤ t ≤ 1)
        /// </summary>
        public Vector3 GetTangentAt(float t)
        {
            if (isDirty || !cachedSplineData.points.IsCreated)
            {
                UpdateSplineData();
            }
            
            t = Mathf.Clamp01(t);
            int index = Mathf.FloorToInt(t * (splineResolution - 1));
            return cachedSplineData.tangents[index];
        }
        
        /// <summary>
        /// Lấy vector pháp tuyến tại tham số t (0 ≤ t ≤ 1)
        /// </summary>
        public Vector3 GetNormalAt(float t)
        {
            if (isDirty || !cachedSplineData.points.IsCreated)
            {
                UpdateSplineData();
            }
            
            t = Mathf.Clamp01(t);
            int index = Mathf.FloorToInt(t * (splineResolution - 1));
            return cachedSplineData.normals[index];
        }
        
        /// <summary>
        /// Lấy vector binormal tại tham số t (0 ≤ t ≤ 1)
        /// </summary>
        public Vector3 GetBinormalAt(float t)
        {
            Vector3 tangent = GetTangentAt(t);
            Vector3 normal = GetNormalAt(t);
            return Vector3.Cross(normal, tangent);
        }
        
        /// <summary>
        /// Tạo ma trận biến đổi (transform) tại tham số t (0 ≤ t ≤ 1)
        /// </summary>
        public Matrix4x4 GetTransformAt(float t)
        {
            Vector3 position = GetPointAt(t);
            Vector3 tangent = GetTangentAt(t);
            Vector3 normal = GetNormalAt(t);
            Vector3 binormal = Vector3.Cross(normal, tangent);
            
            Matrix4x4 matrix = Matrix4x4.identity;
            matrix.SetColumn(0, new Vector4(binormal.x, binormal.y, binormal.z, 0));
            matrix.SetColumn(1, new Vector4(normal.x, normal.y, normal.z, 0));
            matrix.SetColumn(2, new Vector4(tangent.x, tangent.y, tangent.z, 0));
            matrix.SetColumn(3, new Vector4(position.x, position.y, position.z, 1));
            
            return matrix;
        }
        
        /// <summary>
        /// Tạo transform cho một GameObejct tại tham số t (0 ≤ t ≤ 1)
        /// </summary>
        public void SetTransformAt(Transform target, float t)
        {
            Vector3 position = GetPointAt(t);
            Vector3 tangent = GetTangentAt(t);
            Vector3 normal = GetNormalAt(t);
            Vector3 binormal = Vector3.Cross(normal, tangent);
            
            target.position = position;
            target.rotation = Quaternion.LookRotation(tangent, normal);
        }
        
        /// <summary>
        /// Thêm một điểm điều khiển mới
        /// </summary>
        public void AddControlPoint(Vector3 point)
        {
            splineData.AddControlPoint(point);
            MarkDirty();
        }
        
        /// <summary>
        /// Chèn điểm điều khiển tại vị trí chỉ định
        /// </summary>
        public void InsertControlPoint(int index, Vector3 point)
        {
            if (index < 0 || index > splineData.ControlPointCount)
                return;
                
            splineData.InsertControlPoint(index, point);
            MarkDirty();
        }
        
        /// <summary>
        /// Cập nhật vị trí điểm điều khiển
        /// </summary>
        public void UpdateControlPoint(int index, Vector3 point)
        {
            if (index < 0 || index >= splineData.ControlPointCount)
                return;
                
            splineData.UpdateControlPoint(index, point);
            MarkDirty();
        }
        
        /// <summary>
        /// Xóa một điểm điều khiển
        /// </summary>
        public void RemoveControlPoint(int index)
        {
            if (index < 0 || index >= splineData.ControlPointCount)
                return;
                
            splineData.RemoveControlPoint(index);
            MarkDirty();
        }
        
        /// <summary>
        /// Danh dấu spline cần cập nhật lại
        /// </summary>
        public void MarkDirty()
        {
            isDirty = true;
            
            if (autoUpdateSpline)
            {
                UpdateSplineData();
            }
        }
        
        /// <summary>
        /// Lấy SplineData
        /// </summary>
        public SplineData GetRawSplineData()
        {
            return splineData;
        }
        
        /// <summary>
        /// Thiết lập SplineData mới
        /// </summary>
        public void SetSplineData(SplineData data)
        {
            if (data == null) 
                return;
                
            splineData = data;
            MarkDirty();
        }
        
        /// <summary>
        /// Lấy số lượng điểm điều khiển
        /// </summary>
        public int GetControlPointCount()
        {
            return splineData.ControlPointCount;
        }
        
        /// <summary>
        /// Lấy điểm điều khiển tại vị trí index
        /// </summary>
        public Vector3 GetControlPoint(int index)
        {
            return splineData.GetControlPoint(index);
        }
        
        /// <summary>
        /// Lấy tất cả các điểm điều khiển
        /// </summary>
        public List<Vector3> GetAllControlPoints()
        {
            return splineData.GetAllControlPoints();
        }
        
        #endregion
        
        #region Private Methods
        
        private void CreateControlPointsArray()
        {
            // Giải phóng mảng cũ nếu có
            if (controlPointsArray.IsCreated)
            {
                controlPointsArray.Dispose();
            }
            
            // Tạo native array mới
            List<Vector3> points = splineData.GetAllControlPoints();
            controlPointsArray = new NativeArray<float3>(points.Count, Allocator.Persistent);
            
            // Chuyển đổi từ Vector3 sang float3
            for (int i = 0; i < points.Count; i++)
            {
                controlPointsArray[i] = new float3(points[i].x, points[i].y, points[i].z);
            }
        }
        
        private void UpdateSplineData()
        {
            // Cập nhật array control points nếu cần
            if (!controlPointsArray.IsCreated || controlPointsArray.Length != splineData.ControlPointCount)
            {
                CreateControlPointsArray();
            }
            else
            {
                // Cập nhật giá trị control points
                List<Vector3> points = splineData.GetAllControlPoints();
                for (int i = 0; i < points.Count; i++)
                {
                    controlPointsArray[i] = new float3(points[i].x, points[i].y, points[i].z);
                }
            }
            
            // Giải phóng dữ liệu spline cũ nếu có
            if (cachedSplineData.points.IsCreated)
            {
                cachedSplineData.Dispose();
            }
            
            // Tính toán dữ liệu spline mới
            cachedSplineData = SplineJobs.CalculateSpline(
                controlPointsArray,
                splineData.Tension,
                splineData.ClosedLoop,
                splineResolution,
                Allocator.Persistent
            );
            
            isDirty = false;
            
            // Phát event thông báo spline đã thay đổi
            OnSplineChanged?.Invoke();
        }
        
        private void DisposeNativeArrays()
        {
            if (controlPointsArray.IsCreated)
            {
                controlPointsArray.Dispose();
            }
            
            if (cachedSplineData.points.IsCreated)
            {
                cachedSplineData.Dispose();
            }
        }
        
        #endregion
    }
} 
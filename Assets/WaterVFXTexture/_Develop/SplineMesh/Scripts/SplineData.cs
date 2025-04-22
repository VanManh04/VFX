using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace SplineMesh
{
    [CreateAssetMenu(fileName = "New Spline Data", menuName = "SplineMesh/Spline Data")]
    public class SplineData : ScriptableObject
    {
        [SerializeField] private List<Vector3> controlPoints = new();
        [Range(0f, 1f)]
        [SerializeField] private float tension = 0.5f;
        [SerializeField] private bool closedLoop;
        
        public int ControlPointCount => controlPoints.Count;
        public float Tension => tension;
        public bool ClosedLoop => closedLoop;
        
        /// <summary>
        /// Lấy điểm điều khiển tại vị trí chỉ định
        /// </summary>
        public Vector3 GetControlPoint(int index)
        {
            if (index < 0 || index >= controlPoints.Count)
                return Vector3.zero;
                
            return controlPoints[index];
        }
        
        /// <summary>
        /// Thêm điểm điều khiển vào cuối spline
        /// </summary>
        public void AddControlPoint(Vector3 point)
        {
            controlPoints.Add(point);
        }
        
        /// <summary>
        /// Thêm điểm điều khiển tại vị trí chỉ định
        /// </summary>
        public void InsertControlPoint(int index, Vector3 point)
        {
            if (index < 0 || index > controlPoints.Count)
                return;
                
            controlPoints.Insert(index, point);
        }
        
        /// <summary>
        /// Cập nhật điểm điều khiển tại vị trí chỉ định
        /// </summary>
        public void UpdateControlPoint(int index, Vector3 point)
        {
            if (index < 0 || index >= controlPoints.Count)
                return;
                
            controlPoints[index] = point;
        }
        
        /// <summary>
        /// Xóa điểm điều khiển tại vị trí chỉ định
        /// </summary>
        public void RemoveControlPoint(int index)
        {
            if (index < 0 || index >= controlPoints.Count)
                return;
                
            controlPoints.RemoveAt(index);
        }
        
        /// <summary>
        /// Xóa tất cả điểm điều khiển
        /// </summary>
        public void ClearControlPoints()
        {
            controlPoints.Clear();
        }
        
        /// <summary>
        /// Lấy danh sách tất cả điểm điều khiển
        /// </summary>
        public List<Vector3> GetAllControlPoints()
        {
            return new List<Vector3>(controlPoints);
        }
        
        /// <summary>
        /// Thiết lập độ căng của spline (0-1)
        /// </summary>
        public void SetTension(float newTension)
        {
            tension = Mathf.Clamp01(newTension);
        }
        
        /// <summary>
        /// Thiết lập trạng thái vòng kín
        /// </summary>
        public void SetClosedLoop(bool closed)
        {
            closedLoop = closed;
        }
        
        /// <summary>
        /// Chuyển điểm điều khiển từ không gian world sang local
        /// </summary>
        public void TransformControlPoints(Transform transform)
        {
            for (int i = 0; i < controlPoints.Count; i++)
            {
                controlPoints[i] = transform.InverseTransformPoint(controlPoints[i]);
            }
        }
        
        /// <summary>
        /// Tạo Spline Data từ danh sách các điểm
        /// </summary>
        public static SplineData CreateFromPoints(List<Vector3> points, float tension = 0.5f, bool closedLoop = false)
        {
            SplineData data = CreateInstance<SplineData>();
            data.controlPoints = new List<Vector3>(points);
            data.tension = tension;
            data.closedLoop = closedLoop;
            return data;
        }
    }
} 
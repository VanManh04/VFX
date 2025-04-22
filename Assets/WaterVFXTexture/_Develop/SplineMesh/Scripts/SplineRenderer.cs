using UnityEngine;

namespace SplineMesh
{
    [ExecuteInEditMode]
    [RequireComponent(typeof(SplineController))]
    public class SplineRenderer : MonoBehaviour
    {
        [Header("Cài đặt hiển thị")]
        [SerializeField] private bool showSpline = true;
        [SerializeField] private bool showControlPoints = true;
        [SerializeField] private bool showTangents = false;
        [SerializeField] private bool showNormals = false;
        [SerializeField] private bool showFrames = false;
        
        [Header("Màu sắc")]
        [SerializeField] private Color splineColor = Color.white;
        [SerializeField] private Color controlPointColor = Color.red;
        [SerializeField] private Color tangentColor = Color.green;
        [SerializeField] private Color normalColor = Color.blue;
        [SerializeField] private Color binormalColor = Color.yellow;
        
        [Header("Kích thước")]
        [SerializeField] private float controlPointSize = 0.1f;
        [SerializeField] private float vectorScale = 0.5f;
        [SerializeField] private int frameCount = 10;
        
        private SplineController splineController;
        
        private void OnEnable()
        {
            splineController = GetComponent<SplineController>();
            splineController.OnSplineChanged += OnSplineChanged;
        }
        
        private void OnDisable()
        {
            if (splineController != null)
            {
                splineController.OnSplineChanged -= OnSplineChanged;
            }
        }
        
        private void OnSplineChanged()
        {
            // Chỉ dùng trong Editor
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                UnityEditor.EditorApplication.QueuePlayerLoopUpdate();
                UnityEditor.SceneView.RepaintAll();
            }
#endif
        }
        
        private void OnDrawGizmos()
        {
            if (splineController == null)
                return;
                
            // Vẽ điểm điều khiển
            if (showControlPoints)
            {
                Gizmos.color = controlPointColor;
                
                for (int i = 0; i < splineController.GetControlPointCount(); i++)
                {
                    Vector3 point = splineController.GetControlPoint(i);
                    Gizmos.DrawSphere(point, controlPointSize);
                }
            }
            
            // Vẽ spline
            if (showSpline)
            {
                Gizmos.color = splineColor;
                
                for(var i = 0; i < splineController.SplineResolution - 1; i++)
                {
                    float t1 = (float)i / (splineController.SplineResolution - 1);
                    float t2 = (float)(i + 1) / (splineController.SplineResolution - 1);
                    Vector3 position1 = splineController.GetPointAt(t1);
                    Vector3 position2 = splineController.GetPointAt(t2);
                    
                    Gizmos.DrawLine(position1, position2);
                }
            }
            
            // Vẽ các vector tangent, normal và các frame
            if (showTangents || showNormals || showFrames)
            {
                var splineData = splineController.GetSplineData();
                if (!splineData.points.IsCreated)
                    return;
                
                int step = splineData.points.Length / frameCount;
                step = Mathf.Max(1, step);
                
                for (int i = 0; i < splineData.points.Length; i += step)
                {
                    float t = (float)i / (splineData.points.Length - 1);
                    Vector3 position = splineData.points[i];
                    
                    // Vẽ tangent
                    if (showTangents)
                    {
                        Gizmos.color = tangentColor;
                        Vector3 tangent = splineData.tangents[i];
                        Gizmos.DrawRay(position, tangent * vectorScale);
                    }
                    
                    // Vẽ normal
                    if (showNormals)
                    {
                        Gizmos.color = normalColor;
                        Vector3 normal = splineData.normals[i];
                        Gizmos.DrawRay(position, normal * vectorScale);
                    }
                    
                    // Vẽ frame (tangent, normal, binormal)
                    if (showFrames)
                    {
                        Vector3 tangent = splineData.tangents[i];
                        Vector3 normal = splineData.normals[i];
                        Vector3 binormal = Vector3.Cross(normal, tangent);
                        
                        Matrix4x4 matrix = splineController.GetTransformAt(t);
                        Vector3 right = matrix.GetColumn(0);
                        Vector3 up = matrix.GetColumn(1);
                        Vector3 forward = matrix.GetColumn(2);
                        
                        Gizmos.color = tangentColor;
                        Gizmos.DrawRay(position, forward * vectorScale);
                        
                        Gizmos.color = normalColor;
                        Gizmos.DrawRay(position, up * vectorScale);
                        
                        Gizmos.color = binormalColor;
                        Gizmos.DrawRay(position, right * vectorScale);
                    }
                }
            }
        }
        
        #region Public API
        
        /// <summary>
        /// Bật/tắt hiển thị spline
        /// </summary>
        public void SetSplineVisibility(bool visible)
        {
            showSpline = visible;
        }
        
        /// <summary>
        /// Bật/tắt hiển thị điểm điều khiển
        /// </summary>
        public void SetControlPointVisibility(bool visible)
        {
            showControlPoints = visible;
        }
        
        /// <summary>
        /// Bật/tắt hiển thị các vector tiếp tuyến
        /// </summary>
        public void SetTangentVisibility(bool visible)
        {
            showTangents = visible;
        }
        
        /// <summary>
        /// Bật/tắt hiển thị các vector pháp tuyến
        /// </summary>
        public void SetNormalVisibility(bool visible)
        {
            showNormals = visible;
        }
        
        /// <summary>
        /// Bật/tắt hiển thị frames
        /// </summary>
        public void SetFrameVisibility(bool visible)
        {
            showFrames = visible;
        }
        
        #endregion
    }
} 
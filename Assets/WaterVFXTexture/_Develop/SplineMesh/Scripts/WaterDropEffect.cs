using UnityEngine;

namespace SplineMesh
{
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class WaterDropEffect : MonoBehaviour
    {
        [Header("Cài đặt Spline")]
        [SerializeField] private SplineController splineController;
        [SerializeField] private float speed = 0.5f;
        [SerializeField] private bool autoMove = true;
        [SerializeField] private bool loopAnimation = true;
        [SerializeField] private AnimationCurve speedCurve = AnimationCurve.Linear(0, 1, 1, 1);
        
        [Header("Hiệu ứng biến dạng")]
        [SerializeField] private bool deformMesh = true;
        [Range(0.000000001f, 1f)]
        [SerializeField] private float compressionFactor = 0.7f;
        [SerializeField] private float endCompressionStop = 0.5f;
        [SerializeField] private float length             = 6f;
        [SerializeField] private float scale              = 10f;
        
        [Header("Thông số Debug")]
        [SerializeField] private bool showPath = true;
        [SerializeField] private bool showProgress = true;
        
        // Tham số di chuyển
        private float dropParameter = 0f; // t từ 0->1
        private bool isAnimating = false;
        private Mesh originalMesh;
        private MeshFilter meshFilter;
        private MeshDeformer meshDeformer;
        
        private void Awake()
        {
            meshFilter = GetComponent<MeshFilter>();
            
            // Lưu mesh gốc để có thể reset
            if (meshFilter.sharedMesh != null)
            {
                originalMesh = Instantiate(meshFilter.sharedMesh);
                meshFilter.mesh = Instantiate(originalMesh);
            }
            
            if (splineController == null)
            {
                splineController = FindObjectOfType<SplineController>();
                if (splineController == null)
                {
                    Debug.LogError("Không tìm thấy SplineController! Vui lòng kéo thả một SplineController vào Inspector.");
                    enabled = false;
                    return;
                }
            }
        }
        
        private void Start()
        {
            // Khởi tạo MeshDeformer nếu cần
            if (deformMesh)
            {
                meshDeformer = new MeshDeformer(meshFilter.mesh);
                meshDeformer.Initialize(meshFilter.mesh);
            }
            
            // Đặt vị trí ban đầu tại đầu spline
            PlaceAtParameter(0);
            
            if (autoMove)
            {
                StartAnimation();
            }
        }
        
        private void Update()
        {
            if (!isAnimating)
                return;
                
            // Cập nhật tham số t dựa theo thời gian và cấu hình
            float speedMultiplier = speedCurve.Evaluate(dropParameter);
            dropParameter += (Time.deltaTime * speed * speedMultiplier);
            
            // Kiểm tra kết thúc
            if (dropParameter >= 1)
            {
                if (loopAnimation)
                {
                    dropParameter = 0; // Reset về đầu spline
                }
                else
                {
                    dropParameter = 1; // Dừng ở cuối spline
                    isAnimating = false;
                }
            }
            
            // Cập nhật vị trí và biến dạng
            PlaceAtParameter(dropParameter);
        }
        
        private void OnDestroy()
        {
            // Giải phóng tài nguyên
            if (meshDeformer != null)
            {
                meshDeformer.Dispose();
            }
        }
        
        private void OnDrawGizmos()
        {
            if (splineController == null || !showPath) 
                return;
                
            // Hiển thị đường spline
            Gizmos.color = Color.cyan;
            var splineData = splineController.GetSplineData();
            if (splineData.points.IsCreated)
            {
                for (int i = 0; i < splineData.points.Length - 1; i++)
                {
                    Gizmos.DrawLine(splineData.points[i], splineData.points[i + 1]);
                }
            }
            
            // Hiển thị vị trí hiện tại
            if (Application.isPlaying && showProgress)
            {
                Gizmos.color = Color.yellow;
                Vector3 currentPosition = splineController.GetPointAt(dropParameter);
                Gizmos.DrawSphere(currentPosition, 0.1f);
            }
        }
        
        private float Remap(float value, float from1, float to1, float from2, float to2)
        {
            return from2 + (value - from1) * (to2 - from2) / (to1 - from1);
        }
        
        /// <summary>
        /// Đặt vị trí và xoay giọt nước theo tham số t trên spline (0-1)
        /// </summary>
        public void PlaceAtParameter(float t)
        {
            if (splineController == null) 
                return;
                
            // Giới hạn tham số t trong khoảng 0-1
            dropParameter = Mathf.Clamp01(t);
            
            // Biến dạng mesh nếu được bật
            if (deformMesh && meshDeformer != null)
            {
                var splineData = splineController.GetSplineData();
                
                // Biến dạng mesh theo spline
                meshDeformer.DeformAlongSpline(
                    splineData.points,
                    splineData.tangents,
                    splineData.normals,
                    dropParameter,
                    compressionFactor,
                    length,
                    scale
                );
                
                // Áp dụng biến dạng vào mesh
                meshDeformer.ApplyDeformation(meshFilter.mesh);
            }
        }
        
        /// <summary>
        /// Bắt đầu animation
        /// </summary>
        public void StartAnimation()
        {
            isAnimating = true;
        }
        
        /// <summary>
        /// Dừng animation
        /// </summary>
        public void StopAnimation()
        {
            isAnimating = false;
        }
        
        /// <summary>
        /// Đặt lại animation về vị trí ban đầu
        /// </summary>
        public void ResetAnimation()
        {
            dropParameter = 0f;
            PlaceAtParameter(0f);
        }
        
        /// <summary>
        /// Khôi phục mesh về trạng thái ban đầu
        /// </summary>
        public void ResetMesh()
        {
            if (deformMesh && meshDeformer != null)
            {
                meshDeformer.ResetMesh(meshFilter.mesh);
            }
        }
        
        /// <summary>
        /// Lấy tham số t hiện tại
        /// </summary>
        public float GetCurrentParameter()
        {
            return dropParameter;
        }
        
        /// <summary>
        /// Thiết lập SplineController mới
        /// </summary>
        public void SetSplineController(SplineController controller)
        {
            if (controller != null)
            {
                splineController = controller;
                ResetAnimation();
            }
        }
    }
} 
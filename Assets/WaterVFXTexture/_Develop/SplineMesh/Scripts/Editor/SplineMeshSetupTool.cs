using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

namespace SplineMesh.Editor
{
    public class SplineMeshSetupTool : EditorWindow
    {
        private enum DemoType
        {
            Simple,
            Complex,
            Custom
        }

        // Cài đặt tạo scene demo
        private DemoType demoType = DemoType.Simple;
        private GameObject waterDropPrefab;
        private int controlPointCount = 8;
        private float splineLength = 10f;
        private float splineHeight = 2f;
        private float splineCurvature = 1f;
        private bool createSplineInCurvedShape = true;
        private bool createGround = true;

        // Màu sắc
        private Color splineColor = Color.cyan;
        private Color waterColor = new Color(0.2f, 0.6f, 1f, 0.8f);

        // Trạng thái
        private bool isSetupComplete = false;
        private GameObject splineRoot;
        private GameObject waterDropObject;

        [MenuItem("Tools/SplineMesh/Setup Demo Scene")]
        public static void ShowWindow()
        {
            GetWindow<SplineMeshSetupTool>("SplineMesh Demo Setup");
        }

        private void OnGUI()
        {
            GUILayout.Label("SplineMesh Demo Setup Tool", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            demoType = (DemoType)EditorGUILayout.EnumPopup("Demo Type", demoType);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            EditorGUILayout.Space();

            DrawSettingsUI();

            EditorGUILayout.Space();

            if (GUILayout.Button("Create Demo"))
            {
                CreateDemoScene();
            }

            // Hiển thị nút reset nếu đã setup
            if (isSetupComplete)
            {
                EditorGUILayout.Space();
                if (GUILayout.Button("Reset Demo"))
                {
                    ResetDemoScene();
                }
            }
        }

        private void DrawSettingsUI()
        {
            EditorGUILayout.LabelField("Spline Settings", EditorStyles.boldLabel);

            // Tùy chỉnh cài đặt dựa trên loại demo
            switch (demoType)
            {
                case DemoType.Simple:
                    controlPointCount = 4;
                    splineLength = 10f;
                    splineHeight = 2f;
                    splineCurvature = 1f;
                    createSplineInCurvedShape = true;
                    EditorGUILayout.HelpBox("Simple demo: 4 control points tạo đường cong đơn giản với 1 giọt nước.", MessageType.Info);
                    break;

                case DemoType.Complex:
                    controlPointCount = 8;
                    splineLength = 15f;
                    splineHeight = 3f;
                    splineCurvature = 2f;
                    createSplineInCurvedShape = true;
                    EditorGUILayout.HelpBox("Complex demo: 8 control points tạo đường cong phức tạp với các hiệu ứng biến dạng.", MessageType.Info);
                    break;

                case DemoType.Custom:
                    controlPointCount = EditorGUILayout.IntSlider("Control Point Count", controlPointCount, 4, 16);
                    splineLength = EditorGUILayout.Slider("Spline Length", splineLength, 5f, 30f);
                    splineHeight = EditorGUILayout.Slider("Spline Height", splineHeight, 0f, 10f);
                    splineCurvature = EditorGUILayout.Slider("Curvature", splineCurvature, 0.1f, 5f);
                    createSplineInCurvedShape = EditorGUILayout.Toggle("Curved Shape", createSplineInCurvedShape);
                    break;
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Object Settings", EditorStyles.boldLabel);
            
            // Lựa chọn water prefab
            EditorGUI.BeginChangeCheck();
            waterDropPrefab = EditorGUILayout.ObjectField("Water Drop Prefab", waterDropPrefab, typeof(GameObject), false) as GameObject;
            if (EditorGUI.EndChangeCheck() && waterDropPrefab != null)
            {
                // Kiểm tra prefab có mesh không
                if (waterDropPrefab.GetComponent<MeshFilter>() == null)
                {
                    EditorUtility.DisplayDialog("Invalid Prefab", "The selected prefab must have a MeshFilter component.", "OK");
                    waterDropPrefab = null;
                }
            }

            createGround = EditorGUILayout.Toggle("Create Ground", createGround);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Colors", EditorStyles.boldLabel);
            splineColor = EditorGUILayout.ColorField("Spline Color", splineColor);
            waterColor = EditorGUILayout.ColorField("Water Color", waterColor);
        }

        private void CreateDemoScene()
        {
            // Kiểm tra đã có giọt nước chưa
            if (waterDropPrefab == null)
            {
                // Tìm model mặc định
                string[] guids = AssetDatabase.FindAssets("t:prefab water_drop");
                if (guids.Length > 0)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                    waterDropPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                }
                
                // Nếu không tìm thấy, tạo primitive sphere
                if (waterDropPrefab == null)
                {
                    if (!EditorUtility.DisplayDialog("No Water Drop", 
                        "No water drop prefab selected. Do you want to create a simple sphere as a water drop?", 
                        "Create Sphere", "Cancel"))
                    {
                        return;
                    }
                }
            }

            // Tạo root cho scene demo
            splineRoot = new GameObject("SplineMesh_Demo");

            // Tạo SplineController và SplineData
            GameObject splineObject = new GameObject("Spline");
            splineObject.transform.SetParent(splineRoot.transform);
            
            SplineController splineController = splineObject.AddComponent<SplineController>();
            SplineRenderer splineRenderer = splineObject.AddComponent<SplineRenderer>();
            
            // Tạo điểm điều khiển cho spline
            CreateSplineControlPoints(splineController);
            
            // Tạo giọt nước
            CreateWaterDrop(splineController);
            
            // Tạo mặt đất nếu cần
            if (createGround)
            {
                CreateGround(splineRoot.transform);
            }
            
            // Thêm camera và lighting nếu cần
            SetupCameraAndLighting();

            // Đánh dấu đã setup
            isSetupComplete = true;
            
            Debug.Log("SplineMesh demo created successfully!");
            
            // Chọn root trong hierarchy
            Selection.activeGameObject = splineRoot;
        }
        
        private void CreateSplineControlPoints(SplineController splineController)
        {
            // Tạo SplineData trống và không tự động thêm điểm điều khiển
            SplineData splineData = ScriptableObject.CreateInstance<SplineData>();
            
            // Áp dụng SplineData vào controller
            splineController.SetSplineData(splineData);
            
            // Hiển thị hướng dẫn cho người dùng
            Debug.Log("SplineMesh: Created empty spline. Please add control points using the SplineController inspector.");
            EditorUtility.DisplayDialog(
                "SplineMesh Setup", 
                "Đã tạo Spline trống. Bạn cần thêm ít nhất 4 điểm điều khiển bằng cách:\n\n" +
                "1. Chọn đối tượng 'Spline' trong Hierarchy\n" +
                "2. Trong Inspector, mở rộng phần 'Control Points'\n" +
                "3. Nhấn nút 'Add Point' để thêm các điểm điều khiển\n" +
                "4. Sử dụng handles trong Scene View để điều chỉnh vị trí điểm", 
                "OK");
        }
        
        private void CreateWaterDrop(SplineController splineController)
        {
            // Tạo giọt nước từ prefab hoặc sphere
            if (waterDropPrefab != null)
            {
                waterDropObject = (GameObject)PrefabUtility.InstantiatePrefab(waterDropPrefab);
                waterDropObject.transform.SetParent(splineRoot.transform);
                waterDropObject.name = "WaterDrop";
            }
            else
            {
                // Tạo sphere cơ bản
                waterDropObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                waterDropObject.transform.SetParent(splineRoot.transform);
                waterDropObject.name = "WaterDrop";
                waterDropObject.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
            }
            
            // Thêm component WaterDropEffect
            WaterDropEffect waterEffect = waterDropObject.AddComponent<WaterDropEffect>();
            waterEffect.SetSplineController(splineController);
            
            // Thiết lập material cho giọt nước - Sử dụng URP/Lit thay vì Standard
            Renderer renderer = waterDropObject.GetComponent<Renderer>();
            if (renderer != null)
            {
                Material waterMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                waterMaterial.SetFloat("_Smoothness", 0.9f);
                waterMaterial.SetFloat("_Metallic", 0.0f);
                waterMaterial.color = waterColor;
                
                if (waterColor.a < 1.0f)
                {
                    // Thiết lập rendering mode là transparent cho URP
                    waterMaterial.SetFloat("_Surface", 1); // 0 = Opaque, 1 = Transparent
                    waterMaterial.SetFloat("_Blend", 0); // 0 = Alpha, 1 = Premultiply, 2 = Additive, 3 = Multiply
                    waterMaterial.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    waterMaterial.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    waterMaterial.SetFloat("_ZWrite", 0);
                    waterMaterial.SetFloat("_AlphaClip", 0);
                    waterMaterial.EnableKeyword("_ALPHAPREMULTIPLY_ON");
                    waterMaterial.renderQueue = 3000;
                }
                
                renderer.material = waterMaterial;
            }
        }
        
        private void CreateGround(Transform parent)
        {
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.transform.SetParent(parent);
            ground.name = "Ground";
            ground.transform.localScale = new Vector3(splineLength / 10f + 0.5f, 1, splineCurvature + 1.0f);
            ground.transform.position = new Vector3(0, -0.01f, 0);
            
            // Thiết lập material cho mặt đất - Sử dụng URP/Lit thay vì Standard
            Renderer renderer = ground.GetComponent<Renderer>();
            if (renderer != null)
            {
                Material groundMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                groundMaterial.SetFloat("_Smoothness", 0.1f);
                groundMaterial.SetFloat("_Metallic", 0.0f);
                groundMaterial.color = new Color(0.3f, 0.3f, 0.3f);
                renderer.material = groundMaterial;
            }
        }
        
        private void SetupCameraAndLighting()
        {
            // Tạo directional light nếu chưa có
            if (GameObject.FindObjectOfType<Light>() == null)
            {
                GameObject lightObj = new GameObject("Demo_DirectionalLight");
                lightObj.transform.SetParent(splineRoot.transform);
                Light light = lightObj.AddComponent<Light>();
                light.type = LightType.Directional;
                light.intensity = 1.0f;
                light.color = Color.white;
                lightObj.transform.rotation = Quaternion.Euler(50, 30, 0);
            }
            
            // Thiết lập camera nếu cần
            Camera mainCamera = Camera.main;
            if (mainCamera != null)
            {
                mainCamera.transform.position = new Vector3(0, splineHeight + 2f, -splineLength / 2f);
                mainCamera.transform.LookAt(new Vector3(0, splineHeight / 2f, 0));
            }
        }
        
        private void ResetDemoScene()
        {
            // Xóa các đối tượng đã tạo
            if (splineRoot != null)
            {
                DestroyImmediate(splineRoot);
            }
            
            isSetupComplete = false;
        }
    }
} 
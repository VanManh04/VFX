using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace SplineMesh.Editor
{
    [CustomEditor(typeof(SplineController))]
    public class SplineControllerEditor : UnityEditor.Editor
    {
        private SplineController splineController;
        private List<Vector3> controlPoints = new List<Vector3>();
        private int selectedPointIndex = -1;
        private bool showControlPointsList = true;
        
        private SerializedProperty splineDataProperty;
        private SerializedProperty splineResolutionProperty;
        private SerializedProperty autoUpdateSplineProperty;
        
        private void OnEnable()
        {
            splineController = (SplineController)target;
            
            // Lấy các SerializedProperty
            splineDataProperty = serializedObject.FindProperty("splineData");
            splineResolutionProperty = serializedObject.FindProperty("splineResolution");
            autoUpdateSplineProperty = serializedObject.FindProperty("autoUpdateSpline");
            
            // Lấy các điểm điều khiển nếu có SplineData
            RefreshControlPoints();
        }
        
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            
            // Hiển thị các thuộc tính cơ bản
            EditorGUILayout.PropertyField(splineDataProperty);
            EditorGUILayout.PropertyField(splineResolutionProperty);
            EditorGUILayout.PropertyField(autoUpdateSplineProperty);
            
            serializedObject.ApplyModifiedProperties();
            
            EditorGUILayout.Space();
            
            // Hiển thị và quản lý các điểm điều khiển
            showControlPointsList = EditorGUILayout.Foldout(showControlPointsList, "Control Points", true);
            
            if (showControlPointsList)
            {
                EditorGUI.indentLevel++;
                
                // Hiển thị danh sách các điểm điều khiển
                for (int i = 0; i < controlPoints.Count; i++)
                {
                    EditorGUILayout.BeginHorizontal();
                    
                    // Checkbox chọn điểm
                    bool isSelected = i == selectedPointIndex;
                    isSelected = EditorGUILayout.Toggle(isSelected, GUILayout.Width(20));
                    if (isSelected && i != selectedPointIndex)
                    {
                        selectedPointIndex = i;
                    }
                    else if (!isSelected && i == selectedPointIndex)
                    {
                        selectedPointIndex = -1;
                    }
                    
                    // Vector3 field để chỉnh sửa điểm
                    Vector3 point = controlPoints[i];
                    Vector3 newPoint = EditorGUILayout.Vector3Field("Point " + i, point);
                    
                    if (newPoint != point)
                    {
                        controlPoints[i] = newPoint;
                        splineController.UpdateControlPoint(i, newPoint);
                        SceneView.RepaintAll();
                    }
                    
                    // Nút xóa điểm
                    if (GUILayout.Button("X", GUILayout.Width(20)))
                    {
                        if (EditorUtility.DisplayDialog("Delete Control Point", 
                            "Are you sure you want to delete this control point?", 
                            "Delete", "Cancel"))
                        {
                            splineController.RemoveControlPoint(i);
                            RefreshControlPoints();
                            if (selectedPointIndex == i)
                                selectedPointIndex = -1;
                            GUIUtility.ExitGUI();
                        }
                    }
                    
                    EditorGUILayout.EndHorizontal();
                }
                
                EditorGUI.indentLevel--;
                
                EditorGUILayout.Space();
                
                // Các nút thêm điểm mới
                EditorGUILayout.BeginHorizontal();
                
                if (GUILayout.Button("Add Point"))
                {
                    // Thêm điểm mới vào cuối
                    Vector3 newPoint;
                    
                    if (controlPoints.Count > 0)
                    {
                        // Nếu có điểm trước đó, đặt điểm mới cách 1 đơn vị theo trục X
                        newPoint = controlPoints[^1] + new Vector3(1, 0, 0);
                    }
                    else
                    {
                        // Nếu chưa có điểm nào, đặt điểm mới tại gốc
                        newPoint = Vector3.zero;
                    }
                    
                    splineController.AddControlPoint(newPoint);
                    RefreshControlPoints();
                    selectedPointIndex = controlPoints.Count - 1;
                }
                
                if (GUILayout.Button("Insert Point") && selectedPointIndex >= 0)
                {
                    // Chèn điểm mới sau điểm đang chọn
                    Vector3 newPoint;
                    
                    if (selectedPointIndex < controlPoints.Count - 1)
                    {
                        // Tính trung bình của điểm trước và sau
                        newPoint = (controlPoints[selectedPointIndex] + controlPoints[selectedPointIndex + 1]) * 0.5f;
                    }
                    else
                    {
                        // Nếu là điểm cuối, thêm vào sau
                        newPoint = controlPoints[selectedPointIndex] + new Vector3(1, 0, 0);
                    }
                    
                    splineController.InsertControlPoint(selectedPointIndex + 1, newPoint);
                    RefreshControlPoints();
                    selectedPointIndex += 1;
                }
                
                if (GUILayout.Button("Clear All"))
                {
                    if (EditorUtility.DisplayDialog("Clear All Control Points", 
                        "Are you sure you want to delete all control points?", 
                        "Clear All", "Cancel"))
                    {
                        // Lấy SplineData từ controller
                        SplineData splineData = splineController.GetRawSplineData();
                        if (splineData != null)
                        {
                            splineData.ClearControlPoints();
                            splineController.MarkDirty();
                            RefreshControlPoints();
                            selectedPointIndex = -1;
                        }
                    }
                }
                
                EditorGUILayout.EndHorizontal();
            }
            
            // Thông tin về spline
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Spline Info", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Control Points: " + controlPoints.Count);
            EditorGUILayout.LabelField("Selected Point: " + (selectedPointIndex >= 0 ? selectedPointIndex.ToString() : "None"));
            
            // Các tùy chọn SplineData
            EditorGUILayout.Space();
            if (splineController.GetRawSplineData() != null)
            {
                SplineData splineData = splineController.GetRawSplineData();
                
                EditorGUI.BeginChangeCheck();
                
                // Tension
                float tension = splineData.Tension;
                float newTension = EditorGUILayout.Slider("Tension", tension, 0f, 1f);
                if (tension != newTension)
                {
                    splineData.SetTension(newTension);
                    splineController.MarkDirty();
                }
                
                // Closed Loop
                bool closedLoop = splineData.ClosedLoop;
                bool newClosedLoop = EditorGUILayout.Toggle("Closed Loop", closedLoop);
                if (closedLoop != newClosedLoop)
                {
                    splineData.SetClosedLoop(newClosedLoop);
                    splineController.MarkDirty();
                }
                
                if (EditorGUI.EndChangeCheck())
                {
                    SceneView.RepaintAll();
                }
            }
        }
        
        private void RefreshControlPoints()
        {
            controlPoints.Clear();
            SplineData splineData = splineController.GetRawSplineData();
            
            if (splineData != null)
            {
                controlPoints.AddRange(splineData.GetAllControlPoints());
            }
        }
        
        private void OnSceneGUI()
        {
            if (splineController == null) return;
            
            // Vẽ handles cho các điểm điều khiển
            for (int i = 0; i < controlPoints.Count; i++)
            {
                Vector3 point = controlPoints[i];
                
                // Màu cho handle: đỏ cho điểm được chọn, trắng cho điểm thường
                Handles.color = (i == selectedPointIndex) ? Color.red : Color.white;
                
                // Vẽ handle và cho phép di chuyển
                EditorGUI.BeginChangeCheck();
                Vector3 newPoint = Handles.PositionHandle(point, Quaternion.identity);
                
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(splineController, "Move Control Point");
                    splineController.UpdateControlPoint(i, newPoint);
                    controlPoints[i] = newPoint;
                }
                
                // Vẽ nhãn
                Handles.Label(point, "Point " + i);
                
                // Cho phép chọn điểm bằng cách click
                if (Handles.Button(point, Quaternion.identity, 0.1f, 0.1f, Handles.SphereHandleCap))
                {
                    selectedPointIndex = i;
                    Repaint();
                }
            }
        }
    }
} 
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace JUTPS.AI
{	public enum AWMoveTypes { Walk = 0, Run=1 ,Crawl=2 };
	public enum AWPoseTypes { Stand = 0, Crouch =1 ,Lie =2 };
	[System.Serializable]
	public class AdvancedWaypoint
	{
		public Vector3 Position;
		public AWMoveTypes MoveType;
		public AWPoseTypes PoseType;
		public int GrenadeAmount;
		public int FireAmount;
		public AdvancedWaypoint(Vector3 Position,AWMoveTypes MoveType = AWMoveTypes.Walk, AWPoseTypes PoseType = AWPoseTypes.Stand, int GrenadeAmount=0,int FireAmount=0)
		{
			this.Position = Position;
			this.MoveType = MoveType;
			this.PoseType = PoseType;
			this.GrenadeAmount = GrenadeAmount;
			this.FireAmount = FireAmount;
		}
	}
	public class WayPointsPath : MonoBehaviour
    {
	    [HideInInspector]public bool isClosed = false;
	    [HideInInspector]public Color pathColor = Color.green;
	    [HideInInspector]public Color selectedPointColor = Color.yellow;
	    [HideInInspector]public Color centerHandleColor = Color.cyan;
	    [HideInInspector] public float pointRadius = 1f;
	    public bool showCenterHandle = false; // 是否显示中心控制点
	    [HideInInspector] public List<AdvancedWaypoint> AdvanceWaypointsList= new List<AdvancedWaypoint>();
	    [HideInInspector] public AdvancedWaypointObject m_AdvanceWaypointObject;
	    private void OnDrawGizmos()
	    {
		    DrawPath(false);
	    }
    
	    private void OnDrawGizmosSelected()
	    {
		    DrawPath(true);
	    }
    
	    private void DrawPath(bool selected)
	    {
		    if (AdvanceWaypointsList == null || AdvanceWaypointsList.Count < 2) return;
        
		    // 绘制线段
		    Handles.color = pathColor;
		    for (int i = 0; i < AdvanceWaypointsList.Count - 1; i++)
		    {
			    Handles.DrawLine(AdvanceWaypointsList[i].Position, AdvanceWaypointsList[i + 1].Position);
		    }
        
		    if (isClosed && AdvanceWaypointsList.Count > 2)
		    {
			    Handles.DrawLine(AdvanceWaypointsList[AdvanceWaypointsList.Count - 1].Position, AdvanceWaypointsList[0].Position);
		    }
        
		    // 绘制点
		    for (int i = 0; i < AdvanceWaypointsList.Count; i++)
		    {
			    Handles.color = selected ? selectedPointColor : pathColor;
			    Handles.SphereHandleCap(0, AdvanceWaypointsList[i].Position, Quaternion.identity, pointRadius * 0.5f, EventType.Repaint);
		    }
	    }
 
    }
    
    
    
    
    
    
	[CustomEditor(typeof(WayPointsPath))]
	public class WaypointPathEditor : Editor
	{
		GUIStyle FoldoutStyle;
		private int selectedPointIndex = -1;
		private bool showPointHandles = true;
		private WayPointsPath waypointsPath;
		// 新增：整体移动相关的变量
		private bool isMovingAllPoints = false;
		private Vector3 centerPoint;
		private Vector3[] originalPositions;
		private Vector3 lastCenterPosition;
		SerializedProperty AdvanceWaypointsListProp,AdvanceWaypointObjectProp;
		void OnEnable()
		{
			waypointsPath = (WayPointsPath)target;
			AdvanceWaypointsListProp= serializedObject.FindProperty("AdvanceWaypointsList");
			AdvanceWaypointObjectProp = serializedObject.FindProperty("m_AdvanceWaypointObject");
			UpdateCenterPoint();
		}
		
		public override void OnInspectorGUI()
		{
			
			DrawDefaultInspector();
			EditorGUILayout.PropertyField(AdvanceWaypointObjectProp);
			EditorGUILayout.Space();
			if (GUILayout.Button("Import Waypoint Data") && EditorUtility.DisplayDialog("Import Waypoint Data?", "Are you sure you want to clear all of this AI's waypoints and import waypoints from the applied Waypoint Object? This process cannot be undone.", "Yes", "Cancel"))
			{
				waypointsPath.AdvanceWaypointsList = new List<AdvancedWaypoint>(waypointsPath.m_AdvanceWaypointObject.AdvancedWaypointsList);
				EditorUtility.SetDirty(waypointsPath);
			}
			EditorGUILayout.Space();
			if (GUILayout.Button("Export Waypoint Data"))
			{
				//Export all of the AI's current waypoints to a Waypoint Object so it can be imported to other AI.
				string SavePath = EditorUtility.SaveFilePanelInProject("Save Waypoint Data", "New Waypoint Object", "asset", "Please enter a file name to save the file to");
				if (SavePath != string.Empty)
				{
					var m_AdvanceWaypointObject = CreateInstance<AdvancedWaypointObject>();
	 
					m_AdvanceWaypointObject.AdvancedWaypointsList = new List<AdvancedWaypoint>(waypointsPath.AdvanceWaypointsList);
					AssetDatabase.CreateAsset(m_AdvanceWaypointObject, SavePath);
				}

 
			}
			EditorGUILayout.Space();
			// 新增：缩放控制
			if (waypointsPath.AdvanceWaypointsList.Count > 0)
			{
				EditorGUILayout.Space();
				EditorGUILayout.LabelField("Scale Path", EditorStyles.boldLabel);
            
				EditorGUILayout.BeginHorizontal();
				if (GUILayout.Button("Scale 0.5x"))
				{
					ScalePath(0.5f);
				}
				if (GUILayout.Button("Scale 1.5x"))
				{
					ScalePath(1.5f);
				}
				if (GUILayout.Button("Scale 2x"))
				{
					ScalePath(2f);
				}
				EditorGUILayout.EndHorizontal();
			}
			DrawAdvancedWaypointsList(waypointsPath);
			EditorGUILayout.Space();
			serializedObject.ApplyModifiedProperties();
		}
		
		
		

 
		// 新增：计算所有点的中心位置
		private void UpdateCenterPoint()
		{
			if (waypointsPath.AdvanceWaypointsList == null || waypointsPath.AdvanceWaypointsList.Count == 0)
			{
				centerPoint = waypointsPath.transform.position;
				return;
			}
        
			Vector3 sum = Vector3.zero;
			foreach (var advanceWaypoint in waypointsPath.AdvanceWaypointsList)
			{
				sum += advanceWaypoint.Position;
			}
			centerPoint = sum / waypointsPath.AdvanceWaypointsList.Count;
		}
    
		// 新增：保存点的原始位置
		private void SaveOriginalPositions()
		{
			originalPositions = new Vector3[waypointsPath.AdvanceWaypointsList.Count];
			for (int i = 0; i < waypointsPath.AdvanceWaypointsList.Count; i++)
			{
				originalPositions[i] = waypointsPath.AdvanceWaypointsList[i].Position;
			}
		}
    
		// 新增：整体移动所有点
		private void MoveAllPoints(Vector3 delta)
		{
			Undo.RecordObject(waypointsPath, "Move All Points");
        
			for (int i = 0; i < waypointsPath.AdvanceWaypointsList.Count; i++)
			{
				waypointsPath.AdvanceWaypointsList[i].Position = originalPositions[i] + delta;
			}
        
			centerPoint += delta;
			lastCenterPosition = centerPoint;
        
			EditorUtility.SetDirty(waypointsPath);
		}
    
		private void OnSceneGUI()
		{
			if (waypointsPath.AdvanceWaypointsList == null) return;
        
			// 绘制路径
			DrawPath();
        
			// 绘制可编辑的控制点
			if (showPointHandles && !isMovingAllPoints) // 整体移动时隐藏单点控制
			{
				DrawEditablePoints();
			}
        
			// 新增：绘制整体移动控制点
			if (waypointsPath.showCenterHandle && waypointsPath.AdvanceWaypointsList.Count > 0)
			{
				DrawCenterHandle();
			}
        
			// 处理场景中的右键菜单
			HandleRightClick();
        
			HandleUtility.Repaint();
		}
    
		// 新增：绘制中心控制点
		private void DrawCenterHandle()
		{
			UpdateCenterPoint();
        
			EditorGUI.BeginChangeCheck();
        
			// 设置中心点颜色
			Handles.color = waypointsPath.centerHandleColor;
        
			// 绘制中心点的外框（更大的球体）
			Handles.SphereHandleCap(0, centerPoint, Quaternion.identity, waypointsPath.pointRadius * 0.8f, EventType.Repaint);
        
			// 绘制连接线（从中心点到各个点）
			Handles.color = new Color(waypointsPath.centerHandleColor.r, waypointsPath.centerHandleColor.g, waypointsPath.centerHandleColor.b, 0.3f);
        
			foreach (var advanceWaypoint in waypointsPath.AdvanceWaypointsList)
			{
				Handles.DrawDottedLine(centerPoint, advanceWaypoint.Position, 2f);
			}
        
			// 绘制中心点控制手柄
			Handles.color = waypointsPath.centerHandleColor;
			Vector3 newCenter = Handles.PositionHandle(centerPoint, Quaternion.identity);
        
			// 绘制中心点标签
			Handles.BeginGUI();
			Vector2 guiPos = HandleUtility.WorldToGUIPoint(centerPoint);
			GUI.Label(new Rect(guiPos.x - 30, guiPos.y - 30, 60, 20), "Center");
			Handles.EndGUI();
        
			if (EditorGUI.EndChangeCheck())
			{
				if (!isMovingAllPoints)
				{
					// 开始整体移动
					isMovingAllPoints = true;
					SaveOriginalPositions();
					lastCenterPosition = centerPoint;
				}
            
				// 计算移动向量并移动所有点
				Vector3 delta = newCenter - lastCenterPosition;
				MoveAllPoints(delta);
			}
			else if (isMovingAllPoints)
			{
				// 结束整体移动
				isMovingAllPoints = false;
			}
		}
    
		private void DrawPath()
		{
			if (waypointsPath.AdvanceWaypointsList.Count < 2) return;
        
			Handles.color = waypointsPath.pathColor;
        
			for (int i = 0; i < waypointsPath.AdvanceWaypointsList.Count - 1; i++)
			{
				Handles.DrawLine(waypointsPath.AdvanceWaypointsList[i].Position, waypointsPath.AdvanceWaypointsList[i + 1].Position);
			}
        
			if (waypointsPath.isClosed && waypointsPath.AdvanceWaypointsList.Count > 2)
			{
				Handles.DrawLine(waypointsPath.AdvanceWaypointsList[waypointsPath.AdvanceWaypointsList.Count - 1].Position, waypointsPath.AdvanceWaypointsList[0].Position);
			}
		}
    
		private void DrawEditablePoints()
		{
			for (int i = 0; i < waypointsPath.AdvanceWaypointsList.Count; i++)
			{
				EditorGUI.BeginChangeCheck();
            
				Handles.color = (i == selectedPointIndex) ? waypointsPath.selectedPointColor : waypointsPath.pathColor;
            
				Vector3 newPos = Handles.PositionHandle(waypointsPath.AdvanceWaypointsList[i].Position, Quaternion.identity);
            
				Handles.SphereHandleCap(0, waypointsPath.AdvanceWaypointsList[i].Position, Quaternion.identity, waypointsPath.pointRadius * 0.3f, EventType.Repaint);
            
				if (EditorGUI.EndChangeCheck())
				{
					Undo.RecordObject(waypointsPath, "Move Path Point");
					waypointsPath.AdvanceWaypointsList[i].Position = newPos;
					selectedPointIndex = i;
                
					// 更新中心点位置（用于下一次绘制）
					UpdateCenterPoint();
                
					EditorUtility.SetDirty(waypointsPath);
				}
            
				// 显示点的序号
				Handles.BeginGUI();
				Vector2 guiPos = HandleUtility.WorldToGUIPoint(waypointsPath.AdvanceWaypointsList[i].Position);
				GUI.Label(new Rect(guiPos.x - 10, guiPos.y - 20, 30, 20), i.ToString());
				Handles.EndGUI();
			}
		}
    
		private void HandleRightClick()
		{
			Event e = Event.current;
			if (e.type == EventType.MouseDown && e.button == 1)
			{
				Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
				RaycastHit hit;
            
				GenericMenu menu = new GenericMenu();
            
				// 添加点的功能
				menu.AddItem(new GUIContent("Add Point"), false, () => 
				{
					Undo.RecordObject(waypointsPath, "Add Path Point");
                
					if (Physics.Raycast(ray, out hit))
					{
						waypointsPath.AdvanceWaypointsList.Add(new AdvancedWaypoint(hit.point));
					}
					else
					{
						Plane plane = new Plane(Vector3.up, waypointsPath.transform.position);
						float distance;
						if (plane.Raycast(ray, out distance))
						{
							waypointsPath.AdvanceWaypointsList.Add(new AdvancedWaypoint(ray.GetPoint(distance)));
						}
						else
						{
							waypointsPath.AdvanceWaypointsList.Add(new AdvancedWaypoint(waypointsPath.transform.position +ray.direction * 5));
						}
					}
                
					// 更新中心点
					UpdateCenterPoint();
					EditorUtility.SetDirty(waypointsPath);
				});
            
				// 删除选中的点
				if (selectedPointIndex >= 0 && selectedPointIndex < waypointsPath.AdvanceWaypointsList.Count)
				{
					menu.AddItem(new GUIContent("Delete Selected Point"), false, () => 
					{
						Undo.RecordObject(waypointsPath, "Delete Path Point");
						waypointsPath.AdvanceWaypointsList.RemoveAt(selectedPointIndex);
						selectedPointIndex = -1;
                    
						// 更新中心点
						UpdateCenterPoint();
						EditorUtility.SetDirty(waypointsPath);
					});
				}
            
				// 清空所有点
				if ( waypointsPath.AdvanceWaypointsList.Count > 0)
				{
					menu.AddItem(new GUIContent("Clear All Points"), false, () => 
					{
						Undo.RecordObject(waypointsPath, "Clear Path Points");
						waypointsPath.AdvanceWaypointsList.Clear();
						selectedPointIndex = -1;
                    
						// 更新中心点
						UpdateCenterPoint();
						EditorUtility.SetDirty(waypointsPath);
					});
				}
            
				// 新增：整体移动到鼠标位置
				if (waypointsPath.AdvanceWaypointsList.Count > 0)
				{
					menu.AddItem(new GUIContent("Move All Points to Mouse"), false, () => 
					{
						Undo.RecordObject(waypointsPath, "Move All Points to Mouse");
                    
						Vector3 targetPos;
						if (Physics.Raycast(ray, out hit))
						{
							targetPos = hit.point;
						}
						else
						{
							Plane plane = new Plane(Vector3.up, waypointsPath.transform.position);
							float distance;
							if (plane.Raycast(ray, out distance))
							{
								targetPos = ray.GetPoint(distance);
							}
							else
							{
								targetPos = waypointsPath.transform.position + ray.direction * 5;
							}
						}
                    
						// 计算中心点需要移动的向量
						Vector3 delta = targetPos - centerPoint;
                    
						// 移动所有点
						SaveOriginalPositions();
						MoveAllPoints(delta);
					});
				}
            
				menu.ShowAsContext();
				e.Use();
			}
		}
 
    
		// 新增：缩放路径
		private void ScalePath(float scale)
		{
			Undo.RecordObject(waypointsPath, "Scale Path");
        
			UpdateCenterPoint();
        
			for (int i = 0; i < waypointsPath.AdvanceWaypointsList.Count; i++)
			{
				Vector3 direction = waypointsPath.AdvanceWaypointsList[i].Position - centerPoint;
				waypointsPath.AdvanceWaypointsList[i].Position = centerPoint + direction * scale;
			}
        
			EditorUtility.SetDirty(waypointsPath);
		}
    
		private float CalculatePathLength()
		{
			float length = 0;
			for (int i = 0; i < waypointsPath.AdvanceWaypointsList.Count - 1; i++)
			{
				length += Vector3.Distance(waypointsPath.AdvanceWaypointsList[i].Position, waypointsPath.AdvanceWaypointsList[i+1].Position);
			}
        
			if (waypointsPath.isClosed && waypointsPath.AdvanceWaypointsList.Count > 2)
			{
				length += Vector3.Distance(waypointsPath.AdvanceWaypointsList[waypointsPath.AdvanceWaypointsList.Count - 1].Position, waypointsPath.AdvanceWaypointsList[0].Position);
			}
        
			return length;
		}
    
		private void CenterPathOnObject()
		{
			if (waypointsPath.AdvanceWaypointsList.Count == 0) return;
        
			Undo.RecordObject(waypointsPath, "Center Path");
        
			Vector3 center = Vector3.zero;
			foreach (var advanceWaypoint in waypointsPath.AdvanceWaypointsList)
			{
				center += advanceWaypoint.Position;
			}
			center /= waypointsPath.AdvanceWaypointsList.Count;
        
			Vector3 offset = waypointsPath.transform.position - center;
        
			SaveOriginalPositions();
			MoveAllPoints(offset);
		}
		
		
		
		
		
		
		
		
		
		
		
		
		
		
		
		
		
		
		
		
		
		
		
		
		
		
		
		
		
		
		
		
		
		
		void DrawAdvancedWaypointsList(WayPointsPath self)
		{
			if (GUILayout.Button("Add Waypoint"))
			{
				Vector3 newPoint = new Vector3(0, 0, 0);
			
				if (self.AdvanceWaypointsList.Count == 0)
				{
					newPoint = self.transform.position + Vector3.forward * (2 * 2);
				}
				else if (self.AdvanceWaypointsList.Count > 0)
				{
					newPoint = self.AdvanceWaypointsList[self.AdvanceWaypointsList.Count - 1].Position + Vector3.forward * (2 * 2);
				}
			
				Undo.RecordObject(self, "Add Waypoint");
				self.AdvanceWaypointsList.Add(new AdvancedWaypoint(newPoint));
				EditorUtility.SetDirty(self);
			}
			for (int index = 0; index < AdvanceWaypointsListProp.arraySize; index++)
			{
		    	
 
				SerializedProperty AdvancedWaypointElement = AdvanceWaypointsListProp.GetArrayElementAtIndex(index);

				EditorGUILayout.BeginVertical(EditorStyles.helpBox);

				EditorGUILayout.BeginHorizontal(GUILayout.ExpandWidth(true));
				if (GUILayout.Button($"点 {index+1}", GUILayout.Width(50)))
				{
 
					SceneView.lastActiveSceneView.Frame(
						new Bounds(waypointsPath.AdvanceWaypointsList[index].Position, Vector3.one * 2), false);
				}
				EditorGUILayout.PropertyField(AdvancedWaypointElement.FindPropertyRelative("Position"), new GUIContent(" "));
				EditorGUILayout.EndHorizontal();
				// 移动类型下拉框（核心功能）
				SerializedProperty moveTypeProp = AdvancedWaypointElement.FindPropertyRelative("MoveType");
				moveTypeProp.enumValueIndex = EditorGUILayout.Popup(
					"移动类型",
					moveTypeProp.enumValueIndex,
					System.Enum.GetNames(typeof(AWMoveTypes))
				);

				// 姿势类型下拉框（核心功能）
				SerializedProperty poseTypeProp = AdvancedWaypointElement.FindPropertyRelative("PoseType");
				poseTypeProp.enumValueIndex = EditorGUILayout.Popup(
					"姿势类型",
					poseTypeProp.enumValueIndex,
					System.Enum.GetNames(typeof(AWPoseTypes))
				);

				// 数值输入
				SerializedProperty grenadeProp = AdvancedWaypointElement.FindPropertyRelative("GrenadeAmount");
				grenadeProp.intValue = EditorGUILayout.IntField("手雷数量", grenadeProp.intValue);

				SerializedProperty fireProp = AdvancedWaypointElement.FindPropertyRelative("FireAmount");
				fireProp.intValue = EditorGUILayout.IntField("开火数量", fireProp.intValue);
				EditorGUILayout.BeginHorizontal(GUILayout.ExpandWidth(true));
				// 插入按钮
				if (index < AdvanceWaypointsListProp.arraySize - 1)
				{
					if (GUILayout.Button(new GUIContent("插入", "Inserts a point between this point and the next point."), EditorStyles.miniButton, GUILayout.Height(18)))
					{
						Undo.RecordObject(self, "Insert Waypoint Above this Point");
						self.AdvanceWaypointsList.Insert(index + 1,new AdvancedWaypoint((self.AdvanceWaypointsList[index].Position + self.AdvanceWaypointsList[index + 1].Position) / 2f));
						EditorUtility.SetDirty(self); 
						HandleUtility.Repaint();
					}
				}
				// 删除按钮
				if (GUILayout.Button(new GUIContent("删除", "Remove this point from the waypoint list."), EditorStyles.miniButton, GUILayout.Height(18)))
				{
				    
					Undo.RecordObject(self, "Remove Point");
					AdvanceWaypointsListProp.DeleteArrayElementAtIndex(index);
					AdvanceWaypointsListProp.DeleteArrayElementAtIndex(index);
					EditorUtility.SetDirty(self);
					HandleUtility.Repaint();
				}
				EditorGUILayout.EndHorizontal();
				EditorGUILayout.EndVertical();
				EditorGUILayout.Space(5);
			}
		}

	}
}

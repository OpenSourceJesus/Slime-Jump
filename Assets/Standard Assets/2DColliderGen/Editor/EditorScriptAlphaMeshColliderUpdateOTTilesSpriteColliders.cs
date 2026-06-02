#if !(UNITY_3_0 || UNITY_3_1 || UNITY_3_2 || UNITY_3_3 || UNITY_3_4 || UNITY_3_5 || UNITY_4_0 || UNITY_4_0_1 || UNITY_4_1 || UNITY_4_2)
#define UNITY_4_3_AND_LATER
#endif

using UnityEngine;
using UnityEditor;
using System.Collections;

//-------------------------------------------------------------------------
/// <summary>
/// Editor class for the AlphaMeshColliderUpdateOTTilesSpriteColliders component.
/// </summary>
[CustomEditor(typeof(AlphaMeshColliderUpdateOTTilesSpriteColliders))]
public class EditorScriptAlphaMeshColliderUpdateOTTilesSpriteColliders : Editor {

#if UNITY_EDITOR

	//-------------------------------------------------------------------------
	public override void OnInspectorGUI() {
		
		//EditorGUIUtility.LookLikeInspector();
		
		EditorGUILayout.LabelField("This script updates the colliders at Runtime according to the tiles.");
		
		//EditorGUIUtility.LookLikeControls();
	}

#if UNITY_4_3_AND_LATER
    //-------------------------------------------------------------------------
#if UNITY_5_AND_LATER
    [DrawGizmo(GizmoType.InSelectionHierarchy)]
#endif
    static void DrawColliderRuntimeInfo(AlphaMeshColliderUpdateOTTilesSpriteColliders updateObject, GizmoType gizmoType)
    {
		if (updateObject.transform.Find(AlphaMeshColliderUpdateOTTilesSpriteColliders.RUNTIME_GROUP_NODE_NAME) == null) {
			Handles.Label(updateObject.transform.position, "Colliders added at Runtime");
		}
    }
#endif

#endif // #if UNITY_EDITOR
}

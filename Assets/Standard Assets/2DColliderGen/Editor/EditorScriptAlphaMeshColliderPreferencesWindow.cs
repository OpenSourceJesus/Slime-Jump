#if !(UNITY_3_0 || UNITY_3_1 || UNITY_3_2 || UNITY_3_3 || UNITY_3_4 || UNITY_3_5 || UNITY_4_0 || UNITY_4_0_1 || UNITY_4_1 || UNITY_4_2)
#define UNITY_4_3_AND_LATER
#endif

#if !(UNITY_3_0 || UNITY_3_1 || UNITY_3_2 || UNITY_3_3 || UNITY_3_4 || UNITY_3_5 || UNITY_4_0 || UNITY_4_0_1 || UNITY_4_1 || UNITY_4_2 || UNITY_4_3 || UNITY_4_4 || UNITY_4_5 || UNITY_4_6 || UNITY_4_7 || UNITY_5_0)
#define UNITY_5_1_AND_LATER
#endif

using UnityEngine;
using UnityEditor;
using System.Collections;

//-------------------------------------------------------------------------
/// <summary>
/// Editor window for the AlphaMeshCollider preference values.
/// </summary>
public class EditorScriptAlphaMeshColliderPreferencesWindow : EditorWindow {

	GUIContent mDefaultColliderDirectoryLabel = new GUIContent("Collider Directory", "Set the default output directory for generated collider mesh files.");
	GUIContent mDefaultLiveUpdateLabel = new GUIContent("Live Update", "Recalculate the collider mesh when changing parameters in the inspector.");
    GUIContent mDefaultAlphaOpaqueThresholdLabel = new GUIContent("Alpha Opaque Threshold", "Default alpha threshold value in [0..1] above which a pixel is treated as opaque and thus contributes to the outline shape.");
	GUIContent mDefaultColliderPointCountLabel = new GUIContent("Outline Vertex Count", "Default point count of the collider shape.");
	GUIContent mColliderPointCountSliderMaxValueLabel = new GUIContent("Vertex Count Slider Max", "Maximum value of the outline vertex count slider.");
	GUIContent mDefaultColliderThicknessLabel = new GUIContent("Z-Thickness", "Default thickness of a collider.");
#if UNITY_4_3_AND_LATER
	GUIContent mDefaultTargetColliderTypeLabel = new GUIContent("Collider Type", "Default output collider type - MeshCollider or PolygonCollider2D.");
#endif
    GUIContent mDefaultConvexLabel = new GUIContent("Force Convex", "Default value whether to create a convex collider or allow it to be concave.");
    GUIContent mDefaultTriggerLabel = new GUIContent("Collider is Trigger", "Default value whether to set newly generated colliders 'Is Trigger' value to true.");
    GUIContent mDefaultFlipNormalsLabel = new GUIContent("Flip Normals", "Default value whether to flip the normals inside-out.");

    GUIContent mDefaultLowerTopLabel = new GUIContent("Lower Top", "Default value of how much to lower the top border.");
    GUIContent mDefaultRaiseBottomLabel = new GUIContent("Raise Bottom", "Default value of how much to raise the bottom border.");
    GUIContent mDefaultCutLeftLabel = new GUIContent("Cut Left", "Default value of how much to move the left border.");
    GUIContent mDefaultCutRightLabel = new GUIContent("Cut Right", "Default value of how much to move the right border.");
    GUIContent mDefaultExpandTopLabel = new GUIContent("Expand Top", "Default value of how much to raise the top border.");
    GUIContent mDefaultExpandBottomLabel = new GUIContent("Expand Bottom", "Default value of how much to lower the bottom border.");
    GUIContent mDefaultExpandLeftLabel = new GUIContent("Expand Left", "Default value of how much to move the left border.");
    GUIContent mDefaultExpandRightLabel = new GUIContent("Expand Right", "Default value of how much to move the right border.");
    GUIContent mCutSidesMaxValueLabel = new GUIContent("Cut/Expand Slider Max", "Maximum value of the Lower Top, Raise Bottom, Cut Right and Cut Left sliders.");
    
    //-------------------------------------------------------------------------
    [MenuItem ("Window/2D ColliderGen/Collider Preferences", false, 1036)]
	static void ColliderPreferences() {
		
		// Get existing open window or if none, make a new one:
		EditorScriptAlphaMeshColliderPreferencesWindow window = EditorWindow.GetWindow<EditorScriptAlphaMeshColliderPreferencesWindow>();

#if UNITY_5_1_AND_LATER
        window.titleContent = new GUIContent("Default Values");
#else
        window.title = "Default Values";
#endif
    }

    //-------------------------------------------------------------------------
    void OnGUI()
	{
		//EditorGUIUtility.LookLikeControls(150.0f);
		
		AlphaMeshColliderPreferences.Instance.DefaultColliderDirectory = EditorGUILayout.TextField(mDefaultColliderDirectoryLabel, AlphaMeshColliderPreferences.Instance.DefaultColliderDirectory);
		AlphaMeshColliderPreferences.Instance.DefaultLiveUpdate = EditorGUILayout.Toggle(mDefaultLiveUpdateLabel, AlphaMeshColliderPreferences.Instance.DefaultLiveUpdate);
        AlphaMeshColliderPreferences.Instance.DefaultAlphaOpaqueThreshold = EditorGUILayout.FloatField(mDefaultAlphaOpaqueThresholdLabel, AlphaMeshColliderPreferences.Instance.DefaultAlphaOpaqueThreshold);
		AlphaMeshColliderPreferences.Instance.DefaultColliderPointCount = EditorGUILayout.IntField(mDefaultColliderPointCountLabel, AlphaMeshColliderPreferences.Instance.DefaultColliderPointCount);
		AlphaMeshColliderPreferences.Instance.ColliderPointCountSliderMaxValue = EditorGUILayout.IntField(mColliderPointCountSliderMaxValueLabel, AlphaMeshColliderPreferences.Instance.ColliderPointCountSliderMaxValue);
		AlphaMeshColliderPreferences.Instance.DefaultAbsoluteColliderThickness = EditorGUILayout.FloatField(mDefaultColliderThicknessLabel, AlphaMeshColliderPreferences.Instance.DefaultAbsoluteColliderThickness);	
#if UNITY_4_3_AND_LATER
		AlphaMeshColliderPreferences.Instance.DefaultTargetColliderType = (AlphaMeshCollider.TargetColliderType) EditorGUILayout.EnumPopup(mDefaultTargetColliderTypeLabel, AlphaMeshColliderPreferences.Instance.DefaultTargetColliderType);
#endif
        AlphaMeshColliderPreferences.Instance.DefaultConvex = EditorGUILayout.Toggle(mDefaultConvexLabel, AlphaMeshColliderPreferences.Instance.DefaultConvex);
        AlphaMeshColliderPreferences.Instance.DefaultTrigger = EditorGUILayout.Toggle(mDefaultTriggerLabel, AlphaMeshColliderPreferences.Instance.DefaultTrigger);
        AlphaMeshColliderPreferences.Instance.DefaultFlipNormals = EditorGUILayout.Toggle(mDefaultFlipNormalsLabel, AlphaMeshColliderPreferences.Instance.DefaultFlipNormals);

        AlphaMeshColliderPreferences.Instance.DefaultLowerTop = EditorGUILayout.FloatField(mDefaultLowerTopLabel, AlphaMeshColliderPreferences.Instance.DefaultLowerTop);
        AlphaMeshColliderPreferences.Instance.DefaultRaiseBottom = EditorGUILayout.FloatField(mDefaultRaiseBottomLabel, AlphaMeshColliderPreferences.Instance.DefaultRaiseBottom);
        AlphaMeshColliderPreferences.Instance.DefaultCutLeft = EditorGUILayout.FloatField(mDefaultCutLeftLabel, AlphaMeshColliderPreferences.Instance.DefaultCutLeft);
        AlphaMeshColliderPreferences.Instance.DefaultCutRight = EditorGUILayout.FloatField(mDefaultCutRightLabel, AlphaMeshColliderPreferences.Instance.DefaultCutRight);
        AlphaMeshColliderPreferences.Instance.DefaultExpandTop = EditorGUILayout.FloatField(mDefaultExpandTopLabel, AlphaMeshColliderPreferences.Instance.DefaultExpandTop);
        AlphaMeshColliderPreferences.Instance.DefaultExpandBottom = EditorGUILayout.FloatField(mDefaultExpandBottomLabel, AlphaMeshColliderPreferences.Instance.DefaultExpandBottom);
        AlphaMeshColliderPreferences.Instance.DefaultExpandLeft = EditorGUILayout.FloatField(mDefaultExpandLeftLabel, AlphaMeshColliderPreferences.Instance.DefaultExpandLeft);
        AlphaMeshColliderPreferences.Instance.DefaultExpandRight = EditorGUILayout.FloatField(mDefaultExpandRightLabel, AlphaMeshColliderPreferences.Instance.DefaultExpandRight);
        AlphaMeshColliderPreferences.Instance.CutSidesSliderMaxValue = EditorGUILayout.FloatField(mCutSidesMaxValueLabel, AlphaMeshColliderPreferences.Instance.CutSidesSliderMaxValue);

    }
}

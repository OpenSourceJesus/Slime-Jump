#if !(UNITY_3_0 || UNITY_3_1 || UNITY_3_2 || UNITY_3_3 || UNITY_3_4 || UNITY_3_5 || UNITY_4_0 || UNITY_4_0_1 || UNITY_4_1 || UNITY_4_2 || UNITY_4_3 || UNITY_4_4 || UNITY_4_5 || UNITY_4_6 || UNITY_4_7)
#define UNITY_5_AND_LATER
#endif

#if UNITY_5_AND_LATER

using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
using PixelCloudGames;
#endif

/// <summary>
/// This is a dummy component to replace the problematic [MenuItem("Component/...")] entry, which needs an editor-restart to show up.
/// Note that this only applies to the Component menu, other menus (e.g. Window) still work fine.
/// This way we can add the dummy component, check for the condition and perform the add-task and remove the dummy component afterwards.
/// </summary>
[ExecuteInEditMode]
[AddComponentMenu("2D ColliderGen/SmoothMoves Specific/Add AlphaMeshColliders To BoneAnimation")]
public class ComponentMenu8AlphaMeshColliderBoneAnimation : MonoBehaviour {

    [SerializeField] bool mIsInitialized = false;

#if UNITY_EDITOR
    void Update() {

        if (mIsInitialized) {
            return;
        }

        bool isValidBoneAnimation = IsValidBoneAnimation();
        if (isValidBoneAnimation) {
            int undoGroupIndex = Undo.GetCurrentGroup();
            UndoAware.BeginUndoGroup("Add AlphaMeshColliders");

            AddCollidersToBoneAnimationTree(this.transform);
            ComponentMenu2SelectAlphaMeshColliderChildren.SelectChildAlphaMeshColliders(Selection.gameObjects);

            UndoAware.EndUndoGroup();
            Undo.CollapseUndoOperations(undoGroupIndex);
        }
        else {
            Debug.LogError("Not a valid SmoothMoves BoneAnimation object.");
        }
        DestroyImmediate(this, false); // note: we don't want this to be part of the undo.

        mIsInitialized = true;
    }

    //-------------------------------------------------------------------------
    bool IsValidBoneAnimation() {

        Component boneAnimObject = this.GetComponent("BoneAnimation");
        if (boneAnimObject != null) {
            return true;
        }
        return false; // no BoneAnimation component found.
    }

    static void AddCollidersToBoneAnimationTree(Transform node) {
        foreach (Transform child in node) {

            if (!child.name.EndsWith("_Sprite")) {
                AlphaMeshCollider collider = child.GetComponent<AlphaMeshCollider>();
                if (collider == null) {
                    collider = UndoAware.AddComponent<AlphaMeshCollider>(child.gameObject);
                }
            }

            AddCollidersToBoneAnimationTree(child);
        }
    }
#endif
}

#endif

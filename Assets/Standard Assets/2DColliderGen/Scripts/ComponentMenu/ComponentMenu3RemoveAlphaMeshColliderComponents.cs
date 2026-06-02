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
[AddComponentMenu("2D ColliderGen/Remove AlphaMeshCollider Components")]
public class ComponentMenu3RemoveAlphaMeshColliderComponents : MonoBehaviour {

    [SerializeField]
    bool mIsInitialized = false;

#if UNITY_EDITOR
    void Update() {

        if (mIsInitialized) {
            return;
        }

        RemoveColliderAndGenerator();

        DestroyImmediate(this, false); // Note: we don't want this as part of the undo.

        mIsInitialized = true;
    }

    //-------------------------------------------------------------------------
    void RemoveColliderAndGenerator() {

        // AlphaMeshCollider component
        AlphaMeshCollider[] alphaMeshColliderComponents = this.GetComponents<AlphaMeshCollider>();
        List<Transform> colliderNodes = new List<Transform>();
        if (alphaMeshColliderComponents != null) {
            foreach (AlphaMeshCollider component in alphaMeshColliderComponents) {
                colliderNodes.Add(component.TargetNodeToAttachMeshCollider);
                UndoAware.DestroyImmediate(component);
            }
        }
        // secondary components
        AlphaMeshColliderSmoothMovesRestore[] restoreComponents = this.GetComponents<AlphaMeshColliderSmoothMovesRestore>();
        if (restoreComponents != null) {
            foreach (AlphaMeshColliderSmoothMovesRestore restoreComponent in restoreComponents) {
                UndoAware.DestroyImmediate(restoreComponent);
            }
        }
        AlphaMeshColliderUpdateOTTilesSpriteColliders[] updateComponents = this.GetComponents<AlphaMeshColliderUpdateOTTilesSpriteColliders>();
        if (updateComponents != null) {
            foreach (AlphaMeshColliderUpdateOTTilesSpriteColliders updateComponent in updateComponents) {
                UndoAware.DestroyImmediate(updateComponent);
            }
        }
        AlphaMeshColliderCopyColliderEnabled[] copyComponents = this.GetComponents<AlphaMeshColliderCopyColliderEnabled>();
        if (copyComponents != null) {
            foreach (AlphaMeshColliderCopyColliderEnabled copyComponent in copyComponents) {
                UndoAware.DestroyImmediate(copyComponent);
            }
        }
        // MeshCollider components
        MeshCollider[] meshColliderComponents = this.GetComponents<MeshCollider>();
        if (meshColliderComponents != null) {
            foreach (MeshCollider meshColliderComponent in meshColliderComponents) {
                UndoAware.DestroyImmediate(meshColliderComponent);
            }
        }
        // Potentially a MeshCollider at a child node (used for SmoothMoves scale animation)
        foreach (Transform colliderNode in colliderNodes) {
            meshColliderComponents = colliderNode.GetComponents<MeshCollider>();
            foreach (MeshCollider meshColliderComponent in meshColliderComponents) {
                UndoAware.DestroyImmediate(meshColliderComponent);
            }
        }

		// PolygonCollider2D component
		PolygonCollider2D[] polygonColliderComponents = this.GetComponents<PolygonCollider2D>();
		foreach (PolygonCollider2D polygonColliderComponent in polygonColliderComponents) {
            UndoAware.DestroyImmediate(polygonColliderComponent);
		}
		// Potentially a PolygonCollider2D at a child node (used for SmoothMoves scale animation)
		foreach (Transform colliderNode in colliderNodes) {
			polygonColliderComponents = colliderNode.GetComponents<PolygonCollider2D>();
			foreach (PolygonCollider2D polygonColliderComponent in polygonColliderComponents) {
                UndoAware.DestroyImmediate(polygonColliderComponent);
			}
		}

        // EdgeCollider2D component
        EdgeCollider2D[] edgeColliderComponents = this.GetComponents<EdgeCollider2D>();
        foreach (EdgeCollider2D edgeColliderComponent in edgeColliderComponents) {
            UndoAware.DestroyImmediate(edgeColliderComponent);
        }
        // Potentially a EdgeCollider2D at a child node (used for SmoothMoves scale animation)
        foreach (Transform colliderNode in colliderNodes) {
            edgeColliderComponents = colliderNode.GetComponents<EdgeCollider2D>();
            foreach (EdgeCollider2D edgeColliderComponent in edgeColliderComponents) {
                UndoAware.DestroyImmediate(edgeColliderComponent);
            }
        }

        // RuntimeAnimatedColliderSwitch component
        RuntimeAnimatedColliderSwitch[] runtimeAnimatedColliderSwitchComponents = this.GetComponents<RuntimeAnimatedColliderSwitch>();
		foreach (RuntimeAnimatedColliderSwitch switchComponent in runtimeAnimatedColliderSwitchComponents) {
            UndoAware.DestroyImmediate(switchComponent);
		}
		foreach (Transform colliderNode in colliderNodes) {
			runtimeAnimatedColliderSwitchComponents = colliderNode.GetComponents<RuntimeAnimatedColliderSwitch>();
			foreach (RuntimeAnimatedColliderSwitch switchComponent in runtimeAnimatedColliderSwitchComponents) {
                UndoAware.DestroyImmediate(switchComponent);
			}
		}

        // AlphaMeshColliders child gameobject node
        foreach (Transform child in this.transform) {
            if (child.name.Equals("AlphaMeshColliders")) {
                UndoAware.DestroyImmediate(child.gameObject);
            }
        }
    }
#endif
}

#endif

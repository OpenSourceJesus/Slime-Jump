#if !(UNITY_3_0 || UNITY_3_1 || UNITY_3_2 || UNITY_3_3 || UNITY_3_4 || UNITY_3_5 || UNITY_4_0 || UNITY_4_0_1 || UNITY_4_1 || UNITY_4_2 || UNITY_4_3 || UNITY_4_4 || UNITY_4_5 || UNITY_4_6 || UNITY_4_7)
#define UNITY_5_AND_LATER
#endif

#if UNITY_5_AND_LATER

using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// This is a dummy component to replace the problematic [MenuItem("Component/...")] entry, which needs an editor-restart to show up.
/// Note that this only applies to the Component menu, other menus (e.g. Window) still work fine.
/// This way we can add the dummy component, check for the condition and perform the add-task and remove the dummy component afterwards.
/// </summary>
[ExecuteInEditMode]
[AddComponentMenu("2D ColliderGen/Select AlphaMeshCollider Children")]
public class ComponentMenu2SelectAlphaMeshColliderChildren : MonoBehaviour {

    [SerializeField]
    bool mIsInitialized = false;

#if UNITY_EDITOR
    void Update() {

        if (mIsInitialized) {
            return;
        }

        SelectChildAlphaMeshColliders(Selection.gameObjects);

        DestroyImmediate(this, false); // Note: we don't want this as part of the undo.

        mIsInitialized = true;
    }

    //-------------------------------------------------------------------------
    public static void SelectChildAlphaMeshColliders(GameObject[] gameObjects) {

        List<GameObject> newSelectionList = new List<GameObject>();

        foreach (GameObject gameObj in gameObjects) {

            AddAlphaMeshCollidersOfTreeToList(gameObj.transform, ref newSelectionList);
        }

        GameObject[] newSelection = newSelectionList.ToArray();
        Selection.objects = newSelection;
    }

    //-------------------------------------------------------------------------
    static void AddAlphaMeshCollidersOfTreeToList(Transform node, ref List<GameObject> resultList) {

        AlphaMeshCollider alphaCollider = node.GetComponent<AlphaMeshCollider>();
        if (alphaCollider != null) {
            resultList.Add(node.gameObject);
        }

        foreach (Transform child in node) {
            AddAlphaMeshCollidersOfTreeToList(child, ref resultList);
        }
    }
#endif
}

#endif

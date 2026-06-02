#if !(UNITY_3_0 || UNITY_3_1 || UNITY_3_2 || UNITY_3_3 || UNITY_3_4 || UNITY_3_5 || UNITY_4_0 || UNITY_4_0_1 || UNITY_4_1 || UNITY_4_2)
#define UNITY_4_3_AND_LATER
#endif

using UnityEngine;
using System.Collections;

namespace PixelCloudGames {
    
    /// <summary>
    /// Note: The following code is adapted from tk2dUtil.cs from 2D Toolkit by Unikron Software.
    /// </summary>
    public static class UndoAware {

        static string mUndoGroupName = "";

        public static void BeginUndoGroup(string name) {
            mUndoGroupName = name;
        }

        public static void EndUndoGroup() {
            mUndoGroupName = "";
        }

        public static void DestroyImmediate(UnityEngine.Object obj) {
            if (obj == null) {
                return;
            }

#if UNITY_EDITOR && UNITY_4_3_AND_LATER
            if (!Application.isPlaying) {
                UnityEditor.Undo.DestroyObjectImmediate(obj);
            }
            else
#endif
            {
                UnityEngine.Object.DestroyImmediate(obj);
            }
        }

        public static GameObject CreateGameObject(string name) {
            GameObject go = new GameObject(name);
#if UNITY_EDITOR && UNITY_4_3_AND_LATER
            if (!Application.isPlaying) {
                UnityEditor.Undo.RegisterCreatedObjectUndo(go, mUndoGroupName);
            }
#endif
            return go;
        }

        public static T AddComponent<T>(GameObject go) where T : UnityEngine.Component {
            T t = go.AddComponent<T>();
#if UNITY_EDITOR && UNITY_4_3_AND_LATER
            if (!Application.isPlaying) {
                UnityEditor.Undo.RegisterCreatedObjectUndo(t, mUndoGroupName);
            }
#endif
            return t;
        }

        public static void SetTransformParent(Transform t, Transform parent) {
#if UNITY_EDITOR && UNITY_4_3_AND_LATER
            if (!Application.isPlaying) {
                UnityEditor.Undo.SetTransformParent(t, parent, mUndoGroupName);
            }
            else
#endif
            {
                t.parent = parent;
            }
        }
    }
}

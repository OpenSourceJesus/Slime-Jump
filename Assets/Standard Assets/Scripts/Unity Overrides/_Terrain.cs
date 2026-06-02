#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

[ExecuteInEditMode]
[RequireComponent(typeof(Terrain))]
public class _Terrain : MonoBehaviour
{
	public Terrain terrain;
	public Material normalMat;
	public Material selectedMat;
	
	public virtual void OnEnable ()
	{
		if (Application.isPlaying)
		{
			Destroy(this);
			return;
		}
		terrain = GetComponent<Terrain>();
		EditorApplication.update += DoUpdate;
	}
	
	public virtual void OnDisable ()
	{
		EditorApplication.update -= DoUpdate;
	}
	
	public virtual void DoUpdate ()
	{
		foreach  (GameObject go in Selection.gameObjects)
		{
			if (go.name == gameObject.name)
			{
				terrain.materialTemplate = selectedMat;
				return;
			}
		}
		terrain.materialTemplate = normalMat;
	}
}
#endif
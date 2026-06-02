using Extensions;
using UnityEngine;

namespace SlimeJump
{
	[ExecuteInEditMode]
	public class ToggleOverTime : MonoBehaviour
	{
		public static ToggleOverTime[] instances = new ToggleOverTime[0];
		public float timeInterval;
		public SerializableDictionary<Transform, Transform> trsDict = new SerializableDictionary<Transform, Transform>();
		public bool initActive;
		[HideInInspector]
		public bool initialized;

		public void Awake ()
		{
#if UNITY_EDITOR
			if (!Application.isPlaying)
				return;
			trsDict.Init ();
			for (int i = 0; i < trsDict.Count; i ++)
			{
				Transform ghostTrs = trsDict.values[i];
				if (ghostTrs != null && ghostTrs.parent.GetComponent<FollowWaypoints>() == null)
					ghostTrs.SetParent(null);
			}
#endif
			gameObject.SetActive(initActive);
			EventManager.AddEvent (Toggle, Time.time + timeInterval);
		}

#if UNITY_EDITOR
		void OnValidate ()
		{
			if (Application.isPlaying || BuildManager.isBuilding)
				return;
			trsDict.Init ();
			for (int i = 0; i < trsDict.Count; i ++)
			{
				Transform ghostTrs = trsDict.values[i];
				if (ghostTrs != null)
					GameManager.DestroyOnNextEditorUpdate (ghostTrs.gameObject);
				Transform actualTrs = trsDict.keys[i];
				ghostTrs = Instantiate(actualTrs, actualTrs.position, actualTrs.rotation, actualTrs);
				ghostTrs.localScale = Vector3.one;
				FollowWaypoints followWaypoints = ghostTrs.GetComponent<FollowWaypoints>();
				if (followWaypoints == null)
				{
					FollowWaypoints parentFollowWaypoints = actualTrs.GetComponentInParent<FollowWaypoints>();
					if (parentFollowWaypoints != null)
					{
						trsDict.Init ();
						ghostTrs.SetParent(trsDict[actualTrs.parent]);
					}
				}
				for (int i2 = 0; i2 < ghostTrs.childCount; i2 ++)
				{
					Transform child = ghostTrs.GetChild(i2);
					if (followWaypoints == null || child.name != "Waypoints Parent")
						GameManager.DestroyOnNextEditorUpdate (child.gameObject);
				}
				Component[] components = ghostTrs.GetComponents<Component>();
				for (int i2 = 0; i2 < components.Length; i2 ++)
				{
					Component component = components[i2];
					if (component is Transform || component is FollowWaypoints || component is DontChangeFacing)
						continue;
					Renderer renderer = component as Renderer;
					if (renderer != null)
					{
						renderer.sortingOrder --;
						SpriteRenderer spriteRenderer = renderer as SpriteRenderer;
						if (spriteRenderer != null)
							spriteRenderer.color = spriteRenderer.color.DivideAlpha(2);
						else
						{
							LineRenderer lineRenderer = renderer as  LineRenderer;
							lineRenderer.startColor = lineRenderer.startColor.DivideAlpha(2);
							lineRenderer.endColor = lineRenderer.endColor.DivideAlpha(2);
						}
					}
					else
						GameManager.DestroyOnNextEditorUpdate (component);
				}
				trsDict.values[i] = ghostTrs;
			}
		}
#endif

		void Toggle ()
		{
			if (this == null)
				return;
			gameObject.SetActive(!gameObject.activeSelf);
			EventManager.AddEvent (Toggle, Time.time + timeInterval);
		}
	}
}
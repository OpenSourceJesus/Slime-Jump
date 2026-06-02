using SlimeJump;
using Extensions;
using UnityEngine;

namespace Extensions
{
	public static class LineRendererExtensions
	{
		public static void SetLineRenderersToBoundsSides (Bounds bounds, LineRenderer[] lineRenderers)
		{
			LineSegment3D[] sides = bounds.GetSides();
			for (int i = 0; i < 12; i ++)
			{
				LineSegment3D side = sides[i];
				lineRenderers[i].SetPositions(new Vector3[2] { side.start, side.end });
			}
		}

		public static LineRenderer AddLineRendererToGameObjectOrMakeNew (GameObject go)
		{
			if (go == null)
				go = new GameObject();
			else if (go.GetComponent<LineRenderer>() != null)
			{
				Transform trs = go.GetComponent<Transform>();
				go = new GameObject();
				go.GetComponent<Transform>().SetParent(trs);
			}
			return go.AddComponent<LineRenderer>();
		}

		public static LineRenderer AddLineRendererToGameObjectOrMakeNew (GameObject go, FollowWaypoints.WaypointPath waypointPath)
		{
			LineRenderer lineRenderer = AddLineRendererToGameObjectOrMakeNew(go);
			SetGraphicsStyle (lineRenderer, waypointPath);
			return lineRenderer;
		}

		public static LineRenderer AddLineRendererToGameObject (GameObject go, FollowWaypoints.WaypointPath waypointPath)
		{
			LineRenderer lineRenderer = go.AddComponent<LineRenderer>();
			SetGraphicsStyle (lineRenderer, waypointPath);
			return lineRenderer;
		}

		public static void SetGraphicsStyle (this LineRenderer lineRenderer, FollowWaypoints.WaypointPath waypointPath)
		{
			lineRenderer.material = waypointPath.material;
			lineRenderer.startColor = waypointPath.color;
			lineRenderer.endColor = waypointPath.color;
			lineRenderer.startWidth = waypointPath.width;
			lineRenderer.endWidth = waypointPath.width;
			lineRenderer.sortingLayerName = waypointPath.sortingLayerName;
			lineRenderer.sortingOrder = Mathf.Clamp(waypointPath.sortingOrder, -32768, 32767);
		}

		public static void RemoveLineRendererAndGameObjectIfEmpty (this LineRenderer lineRenderer)
		{
			Object destroyObject = lineRenderer;
			if (lineRenderer.GetComponents<Component>().Length == 2)
				destroyObject = lineRenderer.gameObject;
			GameManager.DestroyImmediate (lineRenderer);
		}

		public static void SetUseWorldSpace (this LineRenderer lineRenderer, bool useWorldSpace)
		{
			SetUseWorldSpace (lineRenderer, lineRenderer.GetComponent<Transform>(), useWorldSpace);
		}

		public static void SetUseWorldSpace (this LineRenderer lineRenderer, Transform trs, bool useWorldSpace)
		{
			lineRenderer.useWorldSpace = useWorldSpace;
			Vector3[] positions = new Vector3[lineRenderer.positionCount];
			lineRenderer.GetPositions(positions);
			for (int i = 0; i < lineRenderer.positionCount; i ++)
			{
				if (useWorldSpace)
					positions[i] = trs.TransformPoint(positions[i]);
				else
					positions[i] = trs.InverseTransformPoint(positions[i]);
			}
			lineRenderer.SetPositions(positions);
		}

#if UNITY_EDITOR
		public static void RemoveLineRendererAndGameObjectIfEmpty (this LineRenderer lineRenderer, bool destroyOnNextEditorUpdate)
		{
			if (!destroyOnNextEditorUpdate)
				RemoveLineRendererAndGameObjectIfEmpty (lineRenderer);
			else
			{
				Object destroyObject = lineRenderer;
				if (lineRenderer.GetComponents<Component>().Length == 2)
					destroyObject = lineRenderer.gameObject;
				GameManager.DestroyOnNextEditorUpdate (destroyObject);
			}
		}
#endif
	}
}
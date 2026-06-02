using System;
using Extensions;
using UnityEngine;
using System.Collections;
using UnityEngine.Rendering;
using System.Collections.Generic;

namespace SlimeJump
{
	[ExecuteInEditMode]
	public class FollowWaypoints : UpdateWhileEnabled
	{
#if UNITY_EDITOR
		public bool autoSetWaypoints = true;
		public bool autoSetPivotOffset = true;
		public bool autoSetLineRenderers = true;
		public Transform waypointsParent;
#endif
		public Transform trs;
		// public Rigidbody2D rigid;
		public FollowType followType;
		public List<Waypoint> waypoints = new List<Waypoint>();
		public float moveSpeed;
		public float rotateSpeed;
		public int currentWaypointIndex;
		public bool isBacktracking;
		public WaypointPath path;
		public Vector2 pivotOffset;
		public Transform playerAttachParent;
		[HideInInspector]
		public Vector2 initPos;
		[HideInInspector]
		public float initRot;
		[HideInInspector]
		public int initCurrentWaypointIndex;
		[HideInInspector]
		public bool initIsBacktracking;
		[HideInInspector]
		public Transform currWaypointTrs;
		public FollowWaypoints copy;
		public Collider2D triggerCollider;
		public static FollowWaypoints[] instances = new FollowWaypoints[0];
		RotatePlayerUpdater rotatePlayerUpdater = new RotatePlayerUpdater();
		// List<Rigidbody2D> collidingRigidbodies = new List<Rigidbody2D>();
		
		public override void OnEnable ()
		{
#if UNITY_EDITOR
			if (!Application.isPlaying)
			{
				triggerCollider = GetComponent<Collider2D>();
				Rect boundsRect = GetBoundsRect();
				if (boundsRect == RectExtensions.INFINITE)
					return;
				if (autoSetPivotOffset)
				{
					Vector2 previousPivotOffset = pivotOffset;
					pivotOffset = boundsRect.center - (Vector2) trs.position;
					waypointsParent.localPosition = pivotOffset;
					for (int i = 0; i < waypointsParent.childCount; i ++)
					{
						Transform child = waypointsParent.GetChild(i);
						child.position += (Vector3) (previousPivotOffset - pivotOffset);
					}
				}
				if (autoSetLineRenderers)
					AutoSetLineRenderers ();
				return;
			}
#endif
			for (int i = 0; i < waypoints.Count; i ++)
			{
				Waypoint waypoint = waypoints[i];
				waypoint.trs.SetParent(null);
			}
			currWaypointTrs = waypoints[currentWaypointIndex].trs;
			if (copy != null)
			{
				trs.position = copy.trs.position;
				trs.rotation = copy.trs.rotation;
				currentWaypointIndex = copy.currentWaypointIndex;
				currWaypointTrs = copy.currWaypointTrs;
				isBacktracking = copy.isBacktracking;
			}
			base.OnEnable ();
		}

#if UNITY_EDITOR
		void OnValidate ()
		{
			if (trs == null)
				trs = GetComponent<Transform>();
			// if (rigid == null)
			// 	rigid = GetComponent<Rigidbody2D>();
			// if (rigid != null)
			// {
			// 	rigid.mass = 0;
			// 	Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
			// 	for (int i = 0; i < renderers.Length; i ++)
			// 	{
			// 		Renderer renderer = renderers[i];
			// 		Vector2 size = renderer.bounds.ToRect().size;
			// 		rigid.mass += size.x * size.y;
			// 	}
			// }
			if (autoSetWaypoints)
			{
				if (waypointsParent == null)
					waypointsParent = trs;
				Waypoint[] _waypoints = new Waypoint[waypointsParent.childCount];
				for (int i = 0; i < waypointsParent.childCount; i ++)
				{
					Transform child = waypointsParent.GetChild(i);
					_waypoints[i] = new Waypoint(child, Vector3.zero);
				}
				waypoints = new List<Waypoint>(_waypoints);
			}
			initPos = trs.position;
			initRot = trs.eulerAngles.z;
			initCurrentWaypointIndex = currentWaypointIndex;
			initIsBacktracking = isBacktracking;
		}

		LineRenderer[] AutoSetLineRenderers ()
		{
			List<LineRenderer> output = new List<LineRenderer>();
			LineRenderer[] lineRenderers = GetComponentsInChildren<LineRenderer>();
			for (int i = 0; i < lineRenderers.Length; i ++)
			{
				LineRenderer lineRenderer = lineRenderers[i];
				LineRendererExtensions.RemoveLineRendererAndGameObjectIfEmpty (lineRenderer, true);
			}
			int previousCurrentWaypointIndex = currentWaypointIndex;
			bool previousIsBacktracking = isBacktracking;
			Waypoint nextWaypoint = waypoints[currentWaypointIndex];
			isBacktracking = !isBacktracking;
			OnReachedWaypoint ();
			int previousWaypointIndex = currentWaypointIndex;
			Waypoint previousWaypoint = waypoints[previousWaypointIndex];
			currentWaypointIndex = previousCurrentWaypointIndex;
			isBacktracking = previousIsBacktracking;
			int passedPreviousCurrentWaypointCount = 0;
			List<LineRenderer> joiningLineRenderers = new List<LineRenderer>();
			while (true)
			{
				Waypoint waypoint = nextWaypoint;
				if (currentWaypointIndex == previousCurrentWaypointIndex)
					passedPreviousCurrentWaypointCount ++;
				OnReachedWaypoint ();
				nextWaypoint = waypoints[currentWaypointIndex];
				if (followType == FollowType.Loop)
				{
					if (passedPreviousCurrentWaypointCount == 2)
						break;
				}
				else if (followType == FollowType.PingPong)
				{
					if ((passedPreviousCurrentWaypointCount == 2 && (previousCurrentWaypointIndex == 0 || previousCurrentWaypointIndex == waypoints.Count - 1)) || passedPreviousCurrentWaypointCount == 3)
						break;
				}
				else// if (followType == FollowType.Once)
				{
					if (previousWaypointIndex == currentWaypointIndex)
						break;
				}
				if (waypoint.trs.eulerAngles == previousWaypoint.trs.eulerAngles)
				{
					lineRenderers = new LineRenderer[4];
					for (int i = 0; i < 4; i ++)
					{
						LineRenderer lineRenderer = LineRendererExtensions.AddLineRendererToGameObjectOrMakeNew(gameObject, path);
						lineRenderer.shadowCastingMode = ShadowCastingMode.Off;
						lineRenderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
						lineRenderers[i] = lineRenderer;
					}
					SetLineRenderersAtWaypoint (lineRenderers, waypoint);
					if (output.Count > 3)
					{
						for (int i = 0; i < 4; i ++)
						{
							LineRenderer lineRenderer = LineRendererExtensions.AddLineRendererToGameObjectOrMakeNew(gameObject, path);
							lineRenderer.shadowCastingMode = ShadowCastingMode.Off;
							lineRenderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
							lineRenderer.SetPositions(new Vector3[] { output[output.Count - 4 + i].GetPosition(0), lineRenderers[i].GetPosition(0) });
							joiningLineRenderers.Add(lineRenderer);
						}
					}
					output.AddRange(lineRenderers);
				}
				else
				{
				}
				previousWaypoint = waypoint;
				previousWaypointIndex = currentWaypointIndex;
			}
			currentWaypointIndex = previousCurrentWaypointIndex;
			isBacktracking = previousIsBacktracking;
			output.AddRange(joiningLineRenderers);
			if (waypoints.Count == 2)
			{
				if (waypoints[1].trs.position.x < waypoints[0].trs.position.x)
				{
					LineRendererExtensions.RemoveLineRendererAndGameObjectIfEmpty (output[0], true);
					LineRendererExtensions.RemoveLineRendererAndGameObjectIfEmpty (output[3], true);
					LineRendererExtensions.RemoveLineRendererAndGameObjectIfEmpty (output[5], true);
					LineRendererExtensions.RemoveLineRendererAndGameObjectIfEmpty (output[6], true);
					LineRendererExtensions.RemoveLineRendererAndGameObjectIfEmpty (output[8], true);
					LineRendererExtensions.RemoveLineRendererAndGameObjectIfEmpty (output[10], true);
				}
				else
				{
					
				}
			}
			return output.ToArray();
		}
#endif
		
		Rect GetBoundsRect ()
		{
			Collider2D[] colliders = GetComponentsInChildren<Collider2D>();
			if (colliders.Length == 0)
				return RectExtensions.INFINITE;
			Rect[] childBoundsRectsArray = new Rect[colliders.Length];
			for (int i = 0; i < colliders.Length; i ++)
			{
				Collider2D collider = colliders[i];
				childBoundsRectsArray[i] = collider.bounds.ToRect();
			}
			return childBoundsRectsArray.Combine();
		}

		void SetLineRenderersAtWaypoint (LineRenderer[] lineRenderers, Waypoint waypoint)
		{
			Vector2 boundsRectSize = GetBoundsRect().size;
			Rect boundsRect = new Rect((Vector2) waypoint.trs.position - boundsRectSize / 2, boundsRectSize);
			SetLineRenderersToBoundsRectSidesAndRotate (boundsRect, lineRenderers, (Vector2) waypoint.trs.position + waypoint.pivotOffset, waypoint.trs.eulerAngles.z);
		}

		void SetLineRenderersToBoundsRectSidesAndRotate (Rect boundsRect, LineRenderer[] lineRenderers, Vector3 pivotPoint, float rotation)
		{
			LineSegment2D[] sides = boundsRect.GetSides();
			for (int i = 0; i < 4; i ++)
			{
				LineSegment2D side = sides[i];
				lineRenderers[i].SetPositions(new Vector3[] { side.start.Rotate(pivotPoint, rotation), side.end.Rotate(pivotPoint, rotation) });
			}
		}

		public override void DoUpdate ()
		{
			if (GameManager.paused || _SceneManager.isLoading)
				return;
			if (moveSpeed != 0)
			{
				Vector2 newPosition = Vector3.Lerp(trs.position, (Vector2) currWaypointTrs.position - pivotOffset, moveSpeed * Time.deltaTime * (1f / Vector2.Distance(trs.position, (Vector2) currWaypointTrs.position - pivotOffset)));
				if (!float.IsNaN(newPosition.x))
					trs.position = newPosition;
			}
			if (rotateSpeed != 0)
				trs.rotation = Quaternion.Slerp(trs.rotation, currWaypointTrs.rotation, rotateSpeed * Time.deltaTime * (1f / Quaternion.Angle(trs.rotation, currWaypointTrs.rotation)));
			if (((Vector2) trs.position == (Vector2) currWaypointTrs.position - pivotOffset || moveSpeed == 0) && (trs.eulerAngles == currWaypointTrs.eulerAngles || rotateSpeed == 0))
				OnReachedWaypoint ();
		}
		
		void OnReachedWaypoint ()
		{
			if (isBacktracking)
				currentWaypointIndex --;
			else
				currentWaypointIndex ++;
			switch (followType)
			{
				case FollowType.Once:
					if (currentWaypointIndex == waypoints.Count)
						currentWaypointIndex = waypoints.Count - 1;
					else if (currentWaypointIndex == -1)
						currentWaypointIndex = 0;
					break;
				case FollowType.Loop:
					if (currentWaypointIndex == waypoints.Count)
						currentWaypointIndex = 0;
					else if (currentWaypointIndex == -1)
						currentWaypointIndex = waypoints.Count - 1;
					break;
				case FollowType.PingPong:
					if (currentWaypointIndex == waypoints.Count)
					{
						currentWaypointIndex -= 2;
						isBacktracking = !isBacktracking;
					}
					else if (currentWaypointIndex == -1)
					{
						currentWaypointIndex += 2;
						isBacktracking = !isBacktracking;
					}
					break;
			}
			currWaypointTrs = waypoints[currentWaypointIndex].trs;
		}

		// void OnCollisionEnter (Collision coll)
		// {
		// 	if (rigid == null)
		// 		return;
		// 	Rigidbody collidingRigid = coll.rigidbody;
		// 	if (collidingRigid != null)
		// 	{
		// 		collidingRigidbodies.Add(collidingRigid);
		// 		OnCollisionStay (coll);
		// 	}
		// }

		// void OnCollisionStay (Collision coll)
		// {
		// 	if (rigid == null)
		// 		return;
		// 	for (int i = 0; i < collidingRigidbodies.Count; i ++)
		// 	{
		// 		Rigidbody collidingRigid = collidingRigidbodies[i];
		// 		RaycastHit hit;
		// 		if (collidingRigid.SweepTest(currWaypointTrs.position - pivotOffset - trs.position, out hit, moveSpeed * Time.deltaTime))
		// 		{
		// 			enabled = false;
		// 			return;
		// 		}
		// 	}
		// 	enabled = true;
		// }

		// void OnCollisionExit (Collision coll)
		// {
		// 	if (rigid == null)
		// 		return;
		// 	Rigidbody collidingRigid = coll.rigidbody;
		// 	if (collidingRigid != null)
		// 	{
		// 		collidingRigidbodies.Remove(collidingRigid);
		// 		OnCollisionStay (coll);
		// 	}
		// }

		void OnTriggerEnter2D (Collider2D other)
		{
			Player player = other.GetComponentInParent<Player>();
			if (player != null)
			{
				player.trs.SetParent(playerAttachParent);
				if (currWaypointTrs.eulerAngles.z != trs.eulerAngles.z && rotateSpeed != 0)
					GameManager.updatables = GameManager.updatables.Add(rotatePlayerUpdater);
			}
		}

		void OnTriggerExit2D (Collider2D other)
		{
			Player player = other.GetComponentInParent<Player>();
			if (player != null && (triggerCollider == null || !Player.instance.collider.IsTouching(triggerCollider)))
			{
				player.trs.SetParent(null);
				GameManager.updatables = GameManager.updatables.Remove(rotatePlayerUpdater);
			}
		}

		public void TeleportToWaypoint (int waypointIndex)
		{
			trs.position = (Vector2) waypoints[waypointIndex].trs.position - pivotOffset;
		}

		public override void OnDisable ()
		{
			base.OnDisable ();
			if (Player.instance != null && Player.instance.trs.parent != null)
			{
				Player.instance.trs.SetParent(null);
				GameManager.updatables = GameManager.updatables.Remove(rotatePlayerUpdater);
			}
		}

		public override void OnDestroy ()
		{
			base.OnDestroy ();
			GameManager.updatables = GameManager.updatables.Remove(rotatePlayerUpdater);
		}

		public class RotatePlayerUpdater : IUpdatable
		{
			public void DoUpdate ()
			{
				Player.instance.trs.eulerAngles = Vector3.zero;
			}
		}

		[Serializable]
		public struct Waypoint
		{
			public Transform trs;
			public Vector2 pivotOffset;

			public Waypoint (Transform trs, Vector2 pivotOffset)
			{
				this.trs = trs;
				this.pivotOffset = pivotOffset;
			}
		}

		[Serializable]
		public struct WaypointPath
		{
			public float width;
			public Color color;
			public Material material;
			public string sortingLayerName;
			[Range(-32768, 32767)]
			public int sortingOrder;
		}

		public enum FollowType
		{
			Once,
			Loop,
			PingPong
		}
	}
}
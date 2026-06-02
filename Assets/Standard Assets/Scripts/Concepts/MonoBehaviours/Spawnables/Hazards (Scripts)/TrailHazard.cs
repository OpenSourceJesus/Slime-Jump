using Extensions;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace SlimeJump
{
	public class TrailHazard : Hazard, IUpdatable
	{
		public Transform sourceTrs;
		public LineRenderer lineRenderer;
		public float minPointSeparation;
		public PolygonCollider2D polygonCollider;
		public float duration;
		public float emitTime;
		public float multiplyWidth;
		float emitTimeRemaining;
		List<Point> points = new List<Point>();
		bool emitting;
		List<Vector3> previousPositions3D = new List<Vector3>();
		List<Vector2> previousPositions2D = new List<Vector2>();
		Vector2 previousPosition;

		void OnEnable ()
		{
#if UNITY_EDITOR
			if (!Application.isPlaying)
				return;
#endif
			trs.eulerAngles = Vector3.zero;
			previousPosition = sourceTrs.position;
			emitting = true;
			emitTimeRemaining = emitTime;
			GameManager.updatables = GameManager.updatables.Add(this);
		}

		void OnDisable ()
		{
			GameManager.updatables = GameManager.updatables.Remove(this);
			lineRenderer.positionCount = 0;
			polygonCollider.points = new Vector2[0];
			previousPositions3D.Clear();
			previousPositions2D.Clear();
			points.Clear();
		}

		public void DoUpdate ()
		{
			if (sourceTrs == null || !sourceTrs.gameObject.activeInHierarchy)
				emitting = false;
			if (emitting)
			{
				if ((Vector2) sourceTrs.position != previousPosition)
				{
					Vector2 toCurrentPosition = (Vector2) sourceTrs.position - previousPosition;
					float distanceToPreviousPosition = toCurrentPosition.magnitude;
					float totalMoveAmount = distanceToPreviousPosition;
					while (totalMoveAmount > 0)
					{
						float moveAmount = Mathf.Min(minPointSeparation, totalMoveAmount);
						previousPosition += toCurrentPosition.normalized * moveAmount;
						Vector2 position = previousPosition - (Vector2) trs.position;
						points.Add(new Point(position, Time.time));
						previousPositions3D.Add(position);
						previousPositions2D.Add(position);
						totalMoveAmount -= moveAmount;
					}
					previousPosition = sourceTrs.position;
				}
				RemovePoints ();
				emitTimeRemaining -= Time.deltaTime;
				if (emitTimeRemaining <= 0)
					emitting = false;
			}
			else
				RemovePoints ();
			Keyframe[] keyframes = new Keyframe[points.Count];
			for (int i = 0; i < points.Count; i ++)
			{
				Point point = points[i];
				keyframes[i] = new Keyframe((float) i / points.Count, multiplyWidth - (Time.time - points[i].time) / duration * multiplyWidth);
			}
			lineRenderer.widthCurve = new AnimationCurve(keyframes);
		}

		void RemovePoints ()
		{
			if (points.Count == 0)
				return;
			while (true)
			{
				Point point = points[0];
				if (Time.time - point.time > duration)
				{
					points.RemoveAt(0);
					previousPositions3D.RemoveAt(0);
					previousPositions2D.RemoveAt(0);
					if (points.Count == 0)
					{
						if (!emitting)
						{
                            Destroy(gameObject);
							return;
						}
						break;
					}
				}
				else
					break;
			}
			lineRenderer.positionCount = points.Count;
			lineRenderer.SetPositions(previousPositions3D.ToArray());
			if (points.Count > 1)
				polygonCollider.points = Stroke2D.FromPoints(lineRenderer.widthCurve, previousPositions2D.ToArray()).corners;
		}

		public struct Point
		{
			public Vector2 point;
			public float time;

			public Point (Vector2 point, float time)
			{
				this.point = point;
				this.time = time;
			}
		}
	}
}
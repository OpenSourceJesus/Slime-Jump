using Extensions;
using UnityEngine;

namespace SlimeJump
{
	public class ChaserObject : UpdateWhileEnabled
	{
		public Transform trs;
		public float chaseSpeed;
		public float waitDur;
		public float returnSpeed;
		public Transform endPnt;
		[HideInInspector]
		public Vector2 initPos;
#if UNITY_EDITOR
		public Collider2D triggerCollider;
		public Transform spriteMaskTrs;
		public float visibleBorderWidth;
#endif
		public static ChaserObject[] instances = new ChaserObject[0];
		Phase phase;
		float waitTimer;

		void Awake ()
		{
			if (tag != "EditorOnly")
			{
				trs.SetParent(null);
				endPnt.SetParent(null);
			}
		}

#if UNITY_EDITOR
		public void OnValidate ()
		{
			if (Application.isPlaying)
				return;
			initPos = trs.position;
			Collider2D[] colliders = GetComponentsInChildren<Collider2D>().Remove(triggerCollider);
			Bounds[] boundsInstances = new Bounds[colliders.Length * 2];
			for (int i = 0; i < colliders.Length; i ++)
			{
				Bounds bounds = colliders[i].bounds;
				boundsInstances[i] = bounds;
				bounds.center += endPnt.position - trs.position;
				boundsInstances[i + colliders.Length] = bounds;
			}
			Bounds mergedBounds = boundsInstances.Combine();
			spriteMaskTrs.parent.position = mergedBounds.center;
			spriteMaskTrs.parent.localScale = mergedBounds.size.SetZ(1);
			spriteMaskTrs.SetWorldScale ((Vector2) triggerCollider.bounds.size - Vector2.one * visibleBorderWidth * 2);
		}
#endif

		public override void DoUpdate ()
		{
			if (phase == Phase.Chasing)
			{
				trs.position = Vector3.Lerp(trs.position, endPnt.position, chaseSpeed * Time.deltaTime * (1f / Vector2.Distance(trs.position, endPnt.position)));
				if (trs.position == endPnt.position)
				{
					phase = Phase.Waiting;
					waitTimer = waitDur;
				}
			}
			else if (phase == Phase.Waiting)
			{
				waitTimer -= Time.deltaTime;
				if (waitTimer <= 0)
					phase = Phase.Returning;
			}
			else if (phase == Phase.Returning)
			{
				trs.position = Vector3.Lerp(trs.position, initPos, returnSpeed * Time.deltaTime * (1f / Vector2.Distance(trs.position, initPos)));
				if ((Vector2) trs.position == initPos)
					phase = Phase.Looking;
			}
		}

		void OnTriggerEnter2D (Collider2D other)
		{
			if (phase == Phase.Looking)
				phase = Phase.Chasing;
		}

		void OnTriggerStay2D (Collider2D other)
		{
			OnTriggerEnter2D (other);
		}

		enum Phase
		{
			Looking,
			Chasing,
			Waiting,
			Returning
		}
	}
}
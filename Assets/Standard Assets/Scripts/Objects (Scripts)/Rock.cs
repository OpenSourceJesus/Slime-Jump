using UnityEngine;
using System.Collections.Generic;

namespace SlimeJump
{
	public class Rock : UpdateWhileEnabled, ISpawnable
	{
		public int prefabIndex;
		public int PrefabIndex
		{
			get
			{
				return prefabIndex;
			}
		}
		public Transform trs;
		public Rigidbody2D rigid;
		public Collider2D collider;
		public float moveSpeed;
		public Transform dangerIconTrs;
		public float warnTime;
		float warnDistance;

		public override void OnEnable ()
		{
			base.OnEnable ();
			rigid.linearVelocity = Vector2.down * moveSpeed;
			dangerIconTrs.SetParent(null);
			warnDistance = moveSpeed / warnTime;
		}

		public override void DoUpdate ()
		{
			Rect viewRect = GameCamera.instance.viewRect;
			float distanceToViewRectTop = collider.bounds.min.y - viewRect.yMax;
			if (trs.position.x >= viewRect.xMin && trs.position.x <= viewRect.xMax && distanceToViewRectTop <= warnDistance && distanceToViewRectTop > 0)
			{
				dangerIconTrs.position = new Vector2(trs.position.x, viewRect.yMax);
				dangerIconTrs.gameObject.SetActive(true);
			}
			else
				dangerIconTrs.gameObject.SetActive(false);
		}

		void OnTriggerEnter2D (Collider2D other)
		{
			if (other.isTrigger)
				return;
			if (other.GetComponentInParent<Player>() != null)
				Player.instance.Death (null);
			else
			{
				Destroy(gameObject);
				Destroy(dangerIconTrs.gameObject);
			}
		}
	}
}

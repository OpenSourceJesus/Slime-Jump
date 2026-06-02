using Extensions;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace SlimeJump
{
	public class DissolveOnHit : UpdateWhileEnabled
	{
		public SpriteRenderer spriteRenderer;
		public float dissolveTime;
		public static DissolveOnHit[] instances = new DissolveOnHit[0];
		float dissolveTimer;
		[HideInInspector]
		public bool wasHit;

		void OnCollisionEnter2D (Collision2D coll)
		{
			if (wasHit)
				return;
			dissolveTimer = dissolveTime;
			wasHit = true;
		}

		public override void DoUpdate ()
		{
			if (!wasHit)
				return;
			dissolveTimer -= Time.deltaTime;
			if (dissolveTimer <= 0)
				gameObject.SetActive(false);
			else
				spriteRenderer.color = spriteRenderer.color.SetAlpha(dissolveTimer / dissolveTime);
		}
	}
}
using Extensions;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace SlimeJump
{
	public class DustCloud : UpdateWhileEnabled
	{
		public SpriteRenderer spriteRenderer;
		public float dissolveTime;
		float dissolveTimer;

		public override void DoUpdate ()
		{
			dissolveTimer += Time.deltaTime;
			if (dissolveTimer >= dissolveTime)
				Destroy(gameObject);
			else
				spriteRenderer.color = spriteRenderer.color.SetAlpha(1f - dissolveTimer / dissolveTime);
		}
	}
}
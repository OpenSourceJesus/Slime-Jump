using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace SlimeJump
{
	[ExecuteInEditMode]
	public class Spikes : Hazard, ICollisionEnterHandler2D
	{
		public Collider2D collider;
		public Collider2D Collider
		{
			get
			{
				return collider;
			}
		}

		void Awake ()
		{
#if UNITY_EDITOR
			if (!Application.isPlaying)
			{
				if (trs == null)
					trs = GetComponent<Transform>();
				if (collider == null)
					collider = GetComponent<Collider2D>();
				return;
			}
#endif
		}
	}
}
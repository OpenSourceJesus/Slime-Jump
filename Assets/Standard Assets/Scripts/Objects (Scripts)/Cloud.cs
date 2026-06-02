using Extensions;
using UnityEngine;
using System.Collections.Generic;

namespace SlimeJump
{
	public class Cloud : UpdateWhileEnabled, ISpawnable
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
		public FloatRange moveSpeedRange;
		public FloatRange sizeRange;
		public SpriteRenderer spriteRenderer;
		public FloatRange brigtnessRange;
		public static List<Cloud> instances = new List<Cloud>();
		bool initialized;

		public override void OnEnable ()
		{
			if (initialized)
				return;
			base.OnEnable ();
			instances.Add(this);
			rigid.linearVelocity = Vector2.right * moveSpeedRange.Get(Random.value);
			trs.localScale = Vector3.one * sizeRange.Get(Random.value);
			if (Random.value  < 0.5f)
				trs.localScale = trs.localScale.SetX(-trs.localScale.x);
			float alpha = spriteRenderer.color.a;
			spriteRenderer.color *= brigtnessRange.Get(Random.value);
			spriteRenderer.color = spriteRenderer.color.SetAlpha(alpha);
			initialized = true;
		}

		public override void OnDisable ()
		{
			base.OnDisable ();
			instances.Remove(this);
		}

		public override void DoUpdate ()
		{
			if (trs.position.x > CloudSpawner.instance.spawnZoneBoxCollider.bounds.max.x)
				Destroy(gameObject);
		}
	}
}

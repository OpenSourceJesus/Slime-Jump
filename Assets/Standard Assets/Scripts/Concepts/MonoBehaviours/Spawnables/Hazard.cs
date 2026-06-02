using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace SlimeJump
{
	public class Hazard : Spawnable
	{
		public float damage;

		public virtual void OnTriggerEnter2D (Collider2D other)
		{
			if (other.isTrigger)
				return;
			IDestructable destructable = other.GetComponentInParent<IDestructable>();
			if (destructable != null && other == Player.instance.collider)
				ApplyDamage (destructable, other.transform.position - trs.position, damage);
		}
		
		public virtual void OnCollisionEnter2D (Collision2D coll)
		{
			OnTriggerEnter2D (coll.collider);
		}
		
		public virtual void ApplyDamage (IDestructable destructable, Vector2 direction, float amount)
		{
			destructable.TakeDamage (amount, direction, null);
		}
	}
}
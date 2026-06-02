using Extensions;
using UnityEngine;
using System.Collections.Generic;

namespace SlimeJump
{
	public class BlasterBullet : Bullet
	{
		public float launchSpeed;

		public override void OnEnable ()
		{
			base.OnEnable ();
			rigid.linearVelocity += Player.instance.rigid.linearVelocity;
		}
		
		public override void OnCollisionEnter2D (Collision2D coll)
		{
			if (coll.collider.isTrigger)
				return;
			ContactFilter2D contactFilter = new ContactFilter2D();
			contactFilter.useLayerMask = true;
			contactFilter.layerMask = LayerMask.GetMask("Player");
			Collider2D[] hitColliders = new Collider2D[1];
			ContactPoint2D contactPnt = coll.GetContact(0);
			trs.position += trs.up * contactPnt.separation;
			Physics2D.SyncTransforms();
			if (collider.Overlap(contactFilter, hitColliders) > 0 && Player.instance.timerTillBlasterLaunchNotSubtractJumpVel <= 0)
			{
				float multiplyLaunchSpeed = 1;
				if (Player.instance.isJumping)
					multiplyLaunchSpeed = Player.instance.multiplyBlasterLaunchSpeedWithJumpIfJumpFirst;
				Player.instance.timerTillBlasterLaunchNotSubtractJumpVel = Player.instance.maxTimeAfterBlasterLaunchToSubtractJumpVel;
				Player.instance.blasterLaunchVel -= Vector2.right * trs.up.x * launchSpeed;
				Player.instance.rigid.linearVelocity -= Vector2.up * trs.up.y * launchSpeed * multiplyLaunchSpeed;
			}
			base.OnCollisionEnter2D (coll);
		}
	}
}
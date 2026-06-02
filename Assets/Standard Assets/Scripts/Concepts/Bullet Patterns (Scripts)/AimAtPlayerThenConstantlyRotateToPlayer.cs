using Extensions;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace SlimeJump
{
	[CreateAssetMenu]
	public class AimAtPlayerThenConstantlyRotateTowardsPlayer : AimAtPlayer
	{
		[MakeConfigurable]
		public float rotateRate;
		
		public override Bullet[] Shoot (Transform spawner, Bullet bulletPrefab)
		{
			Bullet[] output = base.Shoot(spawner, bulletPrefab);
			for (int i = 0; i < output.Length; i ++)
			{
				Bullet bullet = output[i];
				GameManager.updatables = GameManager.updatables.Add(new RotateUpdater(bullet, this));
			}
			return output;
		}

		class RotateUpdater : IUpdatable
		{
			Bullet bullet;
			AimAtPlayerThenConstantlyRotateTowardsPlayer aimAtPlayerThenConstantlyRotateTowardsPlayer;

			public RotateUpdater (Bullet bullet, AimAtPlayerThenConstantlyRotateTowardsPlayer aimAtPlayerThenConstantlyRotateTowardsPlayer)
			{
				this.bullet = bullet;
				this.aimAtPlayerThenConstantlyRotateTowardsPlayer = aimAtPlayerThenConstantlyRotateTowardsPlayer;
			}

			public void DoUpdate ()
			{
				if (bullet == null || !bullet.gameObject.activeInHierarchy)
					GameManager.updatables = GameManager.updatables.Remove(this);
				else
					bullet.Retarget (bullet.rigid.linearVelocity.normalized.RotateTo(Player.instance.trs.position - bullet.trs.position, aimAtPlayerThenConstantlyRotateTowardsPlayer.rotateRate * Time.deltaTime));
			}
		}
	}
}
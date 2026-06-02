using System;
using UnityEngine;

namespace SlimeJump
{
	[Serializable]
	public class BulletPatternEntry
	{
		public string name;
		public BulletPattern bulletPattern;
		public Bullet bulletPrefab;
		public Transform spawner;
		
		public Bullet[] Shoot ()
		{
			return bulletPattern.Shoot(spawner, bulletPrefab);
		}
		
		public Bullet[] Shoot (params Collider2D[] dontCollideWith)
		{
			Bullet[] output = bulletPattern.Shoot(spawner, bulletPrefab);
			for (int i = 0; i < output.Length; i ++)
			{
				Bullet bullet = output[i];
				for (int i2 = 0; i2 < dontCollideWith.Length; i2 ++)
				{
					Collider2D _dontCollideWith = dontCollideWith[i2];
					Physics2D.IgnoreCollision(bullet.collider, _dontCollideWith, true);
				}
				bullet.dontCollideWith = dontCollideWith;
				bullet.collider.enabled = true;
			}
			return output;
		}
	}
}
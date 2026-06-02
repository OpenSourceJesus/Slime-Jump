using System.Collections;
using UnityEngine;
using System;

namespace SlimeJump
{
	[CreateAssetMenu]
	public class RepeatBulletPatternsWithDelay : BulletPattern
	{
		[MakeConfigurable]
		public int repeatCount;
		public BulletPatternEntry[] bulletPatternEntries;

		public override void Init (Transform spawner)
		{
			foreach (BulletPatternEntry bulletPatternEntry in bulletPatternEntries)
				bulletPatternEntry.bulletPattern.Init (spawner);
		}

		public override Bullet[] Shoot (Transform spawner, Bullet bulletPrefab)
		{
			GameManager.Instance.StartCoroutine(ShootRoutine (spawner, bulletPrefab));
			return null;
		}
		
		public override Bullet[] Shoot (Vector2 spawnPos, Vector2 direction, Bullet bulletPrefab)
		{
			GameManager.Instance.StartCoroutine(ShootRoutine (spawnPos, direction, bulletPrefab));
			return null;
		}

		public virtual IEnumerator ShootRoutine (Transform spawner, Bullet bulletPrefab)
		{
			for (int i = 0; i < repeatCount; i ++)
			{
				foreach (BulletPatternEntry bulletPatternEntry in bulletPatternEntries)
				{
					yield return new WaitForSeconds(bulletPatternEntry.delay);
					base.Shoot (spawner, bulletPrefab);
				}
			}
		}

		public virtual IEnumerator ShootRoutine (Vector2 spawnPos, Vector2 direction, Bullet bulletPrefab)
		{
			for (int i = 0; i < repeatCount; i ++)
			{
				foreach (BulletPatternEntry bulletPatternEntry in bulletPatternEntries)
				{
					yield return new WaitForSeconds(bulletPatternEntry.delay);
					base.Shoot (spawnPos, direction, bulletPrefab);
				}
			}
		}

		[Serializable]
		public class BulletPatternEntry
		{
			public BulletPattern bulletPattern;
			public float delay;
		}
	}
}
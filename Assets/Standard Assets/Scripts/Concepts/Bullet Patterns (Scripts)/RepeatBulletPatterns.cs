using UnityEngine;
using Extensions;

namespace SlimeJump
{
	[CreateAssetMenu]
	public class RepeatBulletPatterns : BulletPattern
	{
		[MakeConfigurable]
		public int repeatCount;
		public BulletPattern[] bulletPatterns;

		public override void Init (Transform spawner)
		{
			base.Init (spawner);
			foreach (BulletPattern bulletPattern in bulletPatterns)
				bulletPattern.Init (spawner);
		}

		public override Bullet[] Shoot (Transform spawner, Bullet bulletPrefab)
		{
			Bullet[] output = new Bullet[0];
			for (int i = 0; i < repeatCount; i ++)
			{
				foreach (BulletPattern bulletPattern in bulletPatterns)
					output = output.AddRange(bulletPattern.Shoot (spawner, bulletPrefab));
			}
			return output;
		}
		
		public override Bullet[] Shoot (Vector2 spawnPos, Vector2 direction, Bullet bulletPrefab)
		{
			Bullet[] output = new Bullet[0];
			for (int i = 0; i < repeatCount; i ++)
			{
				foreach (BulletPattern bulletPattern in bulletPatterns)
					output = output.AddRange(bulletPattern.Shoot (spawnPos, direction, bulletPrefab));
			}
			return output;
		}
	}
}
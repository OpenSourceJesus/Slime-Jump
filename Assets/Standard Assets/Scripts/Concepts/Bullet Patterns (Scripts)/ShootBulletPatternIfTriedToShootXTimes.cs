using UnityEngine;
using SlimeJump;

public class ShootBulletPatternIfTriedToShootXTimes : BulletPattern
{
	public int requiredShootCount; // "XTimes in ShootBulletPatternIfTriedToShootXTimes"
	int shootCount;

	public override Bullet[] Shoot (Transform spawner, Bullet bulletPrefab)
	{
		if (shootCount == requiredShootCount)
			return base.Shoot (spawner, bulletPrefab);
		shootCount ++;
		return new Bullet[0];
	}
}
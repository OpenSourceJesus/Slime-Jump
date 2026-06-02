namespace SlimeJump
{
	public class Weapon : Item
	{
		public BulletPatternEntry bulletPatternEntry;
		public AnimationEntry animationEntry;

		public override void OnGain (Player player)
		{
			animationEntry.animator = player.animator;
			player.bulletPatternEntriesSortedList[bulletPatternEntry.name] = bulletPatternEntry;
		}

		public override void OnDisable ()
		{
			if (animationEntry.animator != null)
				animationEntry.animator.Play("None", animationEntry.layer);
		}

		public override string ToString ()
		{
			string output = base.ToString();
			Bullet bulletPrefab = bulletPatternEntry.bulletPrefab;
			if (bulletPrefab != null)
			{
				output += " Projectile move speed: " + bulletPrefab.moveSpeed + ". Projectile damage: " + bulletPrefab.damage + ".";
				if (bulletPrefab.autoDespawnMode == Bullet.AutoDespawnMode.RangedAutoDespawn)
					output +=  " Projectile range: " + bulletPrefab.range + ".";
				else if (bulletPrefab.autoDespawnMode == Bullet.AutoDespawnMode.DelayedAutoDespawn)
					output += " Projectile range: " + bulletPrefab.lifetime * bulletPrefab.moveSpeed + ".";
			}
			output += " Cooldown: " + animationEntry.length + "s.\n";
			return output;
		}
	}
}
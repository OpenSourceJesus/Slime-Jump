using UnityEngine;

namespace SlimeJump
{
	[CreateAssetMenu]
	public class AimAtPlayer : BulletPattern
	{
		public override Vector2 GetShootDirection (Transform spawner)
		{
			return Player.instance.trs.position - spawner.position;
		}
	}
}
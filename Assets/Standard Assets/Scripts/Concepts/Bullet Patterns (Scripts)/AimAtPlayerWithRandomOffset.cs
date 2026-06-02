using Extensions;
using UnityEngine;

namespace SlimeJump
{
	[CreateAssetMenu]
	public class AimAtPlayerWithRandomOffset : AimAtPlayer
	{
		[MakeConfigurable]
		public FloatRange randomOffset;
		
		public override Vector2 GetShootDirection (Transform spawner)
		{
			return base.GetShootDirection(spawner).Rotate(randomOffset.Get(Random.value));
		}
	}
}
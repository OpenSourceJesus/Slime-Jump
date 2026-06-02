using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Extensions;

namespace SlimeJump
{
	[CreateAssetMenu]
	public class AimAtPlayerWithOffset : AimAtPlayer
	{
		[MakeConfigurable]
		public float shootOffset;
		
		public override Vector2 GetShootDirection (Transform spawner)
		{
			return base.GetShootDirection(spawner).Rotate(shootOffset);
		}
	}
}
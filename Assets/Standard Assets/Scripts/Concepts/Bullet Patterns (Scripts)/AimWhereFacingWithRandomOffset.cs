using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Extensions;

namespace SlimeJump
{
	[CreateAssetMenu]
	public class AimWhereFacingWithRandomOffset : AimWhereFacing
	{
		public FloatRange shootOffsetRange;
		
		public override Vector2 GetShootDirection (Transform spawner)
		{
			return base.GetShootDirection(spawner).Rotate(Random.Range(shootOffsetRange.min, shootOffsetRange.max));
		}
	}
}
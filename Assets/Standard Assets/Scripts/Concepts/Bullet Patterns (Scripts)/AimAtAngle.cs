  
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Extensions;

namespace SlimeJump
{
	[CreateAssetMenu]
	public class AimAtAngle : BulletPattern
	{
		public float angle;

		public override Vector2 GetShootDirection (Transform spawner)
		{
			return VectorExtensions.FromFacingAngle(angle);
		}
	}
}
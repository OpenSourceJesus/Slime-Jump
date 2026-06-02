using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Extensions;

namespace SlimeJump
{
	[CreateAssetMenu]
	public class AimAtClosestAngleToPlayer : BulletPattern
	{
		public float[] angles;

		public override Vector2 GetShootDirection (Transform spawner)
		{
			float toPlayerAngle = VectorExtensions.GetFacingAngle(Player.instance.trs.position - spawner.position);
			float closestAngle = angles[0];
			float degreesToClosestAngle = Mathf.DeltaAngle(closestAngle, toPlayerAngle);
			float angle;
			float degreesToAngle;
			for (int i = 1; i < angles.Length; i ++)
			{
				angle = angles[i];
				degreesToAngle = Mathf.DeltaAngle(angle, toPlayerAngle);
				if (degreesToAngle < degreesToClosestAngle)
				{
					closestAngle = angle;
					degreesToClosestAngle = degreesToAngle;
				}
			}
			return VectorExtensions.FromFacingAngle(closestAngle);
		}
	}
}
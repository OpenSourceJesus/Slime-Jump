using Extensions;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace SlimeJump
{
	[CreateAssetMenu]
	public class AimAtClosestAngleToFacing : BulletPattern
	{
		public float[] angles;

		public override Vector2 GetShootDirection (Transform spawner)
		{
			float closestAngle = angles[0];
			float degreesToClosestAngle = Mathf.DeltaAngle(closestAngle, spawner.eulerAngles.z);
			for (int i = 1; i < angles.Length; i ++)
			{
				float angle = angles[i];
				float degreesToAngle = Mathf.DeltaAngle(angle, spawner.eulerAngles.z);
				if (degreesToAngle < degreesToClosestAngle)
				{
					closestAngle = angle;
					degreesToClosestAngle = degreesToAngle;
				}
			}
			return VectorExtensions.FromFacingAngle(closestAngle - 90);
		}
	}
}
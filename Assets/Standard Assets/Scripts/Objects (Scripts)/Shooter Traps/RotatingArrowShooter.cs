using Extensions;
using UnityEngine;

namespace SlimeJump
{
	public class RotatingArrowShooter : ShooterTrap
	{
		public FollowWaypoints waypointFollower;
		float previousNormalizedPlayPosition;
		float initRotationOffset;

		public override void Awake ()
		{
			base.Awake ();
			initRotationOffset = trs.eulerAngles.z % (shootAnimationEntry.length * waypointFollower.rotateSpeed);
		}

		public override void DoUpdate ()
		{
			float degreesBetweenShots = shootAnimationEntry.length * waypointFollower.rotateSpeed;
			float normalizedPlayPosition = 1f - ((trs.eulerAngles.z + initRotationOffset) % degreesBetweenShots) / degreesBetweenShots;
			if (normalizedPlayPosition < previousNormalizedPlayPosition)
			{
				float previousRotation = trs.eulerAngles.z;
				trs.eulerAngles = Vector3.forward * (MathfExtensions.SnapToInterval(trs.eulerAngles.z, degreesBetweenShots - initRotationOffset));
				if (Time.time - timeAtLastShot >= shootAnimationEntry.length / 2)
					Shoot ();
				trs.eulerAngles = Vector3.forward * previousRotation;
			}
			shootAnimationEntry.Play (normalizedPlayPosition);
			previousNormalizedPlayPosition = normalizedPlayPosition;
		}
	}
}
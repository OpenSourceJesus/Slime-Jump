using Extensions;
using UnityEngine;

namespace SlimeJump
{
	public class PortalItem2 : UseableItem, IUpdatable
	{
		public Transform targetPositionIndicatorTrs;
		public Transform destinationIndicatorTrs;
		public FloatRange destinationDistanceRange;
		public float durationToChangeDestinationDistance;
		public LayerMask whatICanTeleportIn;
		float destinationDistance;
		float destinationDistanceChangeSpeed;
		float destinationDistanceRangeSize;

		public override void OnGain (Player player)
		{
			base.OnGain (player);
			destinationDistanceRangeSize = destinationDistanceRange.max - destinationDistanceRange.min;
			destinationDistance = destinationDistanceRange.min;
			destinationDistanceChangeSpeed = destinationDistanceRangeSize / durationToChangeDestinationDistance;
		}

		public override void OnBeganUsing ()
		{
			base.OnBeganUsing ();
			destinationDistance = destinationDistanceRange.min;
			targetPositionIndicatorTrs.localPosition = Vector3.up * destinationDistance;
			targetPositionIndicatorTrs.gameObject.SetActive(true);
			SetDestinationIndicatorPosition ();
			destinationIndicatorTrs.gameObject.SetActive(true);
			GameManager.updatables = GameManager.updatables.Add(this);
		}

		public override void OnStopUsing ()
		{
			base.OnStopUsing ();
			OnDisable ();
		}

		public override void OnDisable ()
		{
			if (destinationIndicatorTrs != null)
			{
				destinationIndicatorTrs.gameObject.SetActive(false);
				Player.instance.trs.position = destinationIndicatorTrs.position;
			}
			if (targetPositionIndicatorTrs != null)
				targetPositionIndicatorTrs.gameObject.SetActive(false);
			GameManager.updatables = GameManager.updatables.Remove(this);
		}

		public void DoUpdate ()
		{
			if (destinationDistance < destinationDistanceRange.max)
			{
				destinationDistance += destinationDistanceChangeSpeed * Time.deltaTime;
				targetPositionIndicatorTrs.localPosition = Vector3.up * destinationDistance;
			}
			else if (destinationDistance > destinationDistanceRange.max)
			{
				destinationDistance = destinationDistanceRange.max;
				targetPositionIndicatorTrs.localPosition = Vector3.up * destinationDistance;
			}
			SetDestinationIndicatorPosition ();
		}

		void SetDestinationIndicatorPosition ()
		{
			RaycastHit2D[] hits = new RaycastHit2D[1];
			ContactFilter2D contactFilter = new ContactFilter2D();
			contactFilter.SetLayerMask(whatICanTeleportIn);
			contactFilter.useTriggers = true;
			if (Player.instance.rigid.Cast(destinationIndicatorTrs.up, contactFilter, hits, destinationDistance) > 0)
				destinationIndicatorTrs.localPosition = Vector3.up * hits[0].distance;
			else
				destinationIndicatorTrs.position = targetPositionIndicatorTrs.position;
		}
	}
}
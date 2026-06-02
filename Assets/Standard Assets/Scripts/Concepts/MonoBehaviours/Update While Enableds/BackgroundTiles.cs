using Extensions;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace SlimeJump
{
	public class BackgroundTiles : UpdateWhileEnabled
	{
		public Material material;
		public float missileMapBounds;
		public float missileTurnRate;
		public float missileSpeed;
		Vector2 missilePosition;
		Vector2 missileDestination;
		Vector2 missileVelocity;
		float missileDistToDestSqr;
		bool missileHasReducedDistToDest;

		public override void OnEnable ()
		{
			missileVelocity = Random.insideUnitCircle.normalized * missileSpeed;
			missileDestination = Random.insideUnitCircle * missileMapBounds;
			base.OnEnable ();
		}

		public override void DoUpdate ()
		{
			float prevMissileDistToDestSqr = (missilePosition - missileDestination).sqrMagnitude;
			missileVelocity = missileVelocity.RotateTo(missileDestination - missilePosition, missileTurnRate * Time.deltaTime);
			missilePosition += missileVelocity * Time.deltaTime;
			float missileDistToDestSqr = (missilePosition - missileDestination).sqrMagnitude;
			if (missileHasReducedDistToDest && missileDistToDestSqr > prevMissileDistToDestSqr)
			{
				missileDestination = Random.insideUnitCircle * missileMapBounds;
				missileHasReducedDistToDest = false;
			}
			else if (missileDistToDestSqr < prevMissileDistToDestSqr)
				missileHasReducedDistToDest = true;
			material.SetVector("_flipbookUVOffset", missilePosition);
		}
	}
}
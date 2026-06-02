using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace SlimeJump
{
	public class Vortex : UpdateWhileEnabled
	{
		public Transform trs;
		public float strength;
		public float distance;
		public Dictionary<AffectedByVortex, Vector2> affectedByVortexVelocitiesDict = new Dictionary<AffectedByVortex, Vector2>();
		public static Vortex[] instances = new Vortex[0];

		public override void DoUpdate ()
		{
			for (int i = 0; i < AffectedByVortex.instances.Count; i ++)
			{
				AffectedByVortex affectedByVortex = AffectedByVortex.instances[i];
				Vector2 velocity;
				if (affectedByVortexVelocitiesDict.TryGetValue(affectedByVortex, out velocity))
					affectedByVortex.velocity -= velocity;
				Vector2 toAffectedByVortex = affectedByVortex.trs.position - trs.position;
				if (toAffectedByVortex.sqrMagnitude <= distance * distance)
				{
					velocity = -toAffectedByVortex.normalized * Mathf.InverseLerp(distance, 0, toAffectedByVortex.magnitude) * strength;
					affectedByVortex.velocity += velocity;
					affectedByVortexVelocitiesDict[affectedByVortex] = velocity;
				}
				else
					affectedByVortexVelocitiesDict.Remove(affectedByVortex);
			}
		}
	}
}
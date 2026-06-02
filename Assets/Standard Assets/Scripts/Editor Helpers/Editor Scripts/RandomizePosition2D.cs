#if UNITY_EDITOR
using Extensions;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace SlimeJump
{
	public class RandomizePosition2D : EditorScript
	{
		public Transform trs;
		public Mode mode;
		public Collider2D collider;
		public Zone2D zone;

		public override void Do ()
		{
			if (trs == null)
				trs = transform;
			if (mode == Mode.Circle)
				trs.position = collider.bounds.center + (Vector3) Random.insideUnitCircle.normalized * Random.value * collider.bounds.extents.x;
			else if (mode == Mode.Box)
				trs.position = collider.bounds.ToRect().RandomPoint();
			else
			{
				zone = zone.Gen();
				trs.position = zone.GetRandomPoint();
			}
		}

		public enum Mode
		{
			Circle,
			Box,
			Zone
		}
	}
}
#else
namespace SlimeJump
{
	public class RandomizePosition2D : EditorScript
	{
	}
}
#endif
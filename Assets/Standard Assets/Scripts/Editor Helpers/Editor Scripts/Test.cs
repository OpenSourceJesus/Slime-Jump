#if UNITY_EDITOR
using Extensions;
using UnityEngine;

namespace SlimeJump
{
	public class Test : EditorScript
	{
		public PolygonCollider2D polygonCollider;
		public PolygonCollider2D polygonCollider2;
		public FloatRange angleRangeToCopy;

		public override void Do ()
		{
			Vector2[] points = polygonCollider.points;
			for (int i = 0; i < points.Length; i ++)
			{
				Vector2 point = points[i];
				if (!angleRangeToCopy.Contains(Vector2.SignedAngle(Vector2.left, point)))
					point = polygonCollider2.points[i];
				points[i] = point;
			}
			polygonCollider.points = points;
		}
	}
}
#else
namespace SlimeJump
{
	public class Test : EditorScript
	{
	}
}
#endif
using UnityEngine;

namespace Extensions
{
	public static class ColliderExtensions
	{
		public static Rect GetRect (this Collider2D collider)
		{
			Rect output = new Rect();
			Transform trs = collider.transform;
			BoxCollider2D boxCollider = collider as BoxCollider2D;
			if (boxCollider != null)
			{
				Vector2 center = trs.position + trs.TransformVector(boxCollider.offset);
				Vector2 halfSize = trs.TransformVector(boxCollider.size + Vector2.one * boxCollider.edgeRadius * 2) / 2;
				Vector2 min = center - halfSize;
				Vector2 max = center + halfSize;
				output = Rect.MinMaxRect(min.x, min.y, max.x, max.y);
			}
			else
			{
				PolygonCollider2D polygonCollider = collider as PolygonCollider2D;
				if (polygonCollider != null)
				{
					Vector2[] pnts = new Vector2[polygonCollider.points.Length];
					for (int i = 0; i < polygonCollider.points.Length; i ++)
					{
						Vector2 pnt = polygonCollider.points[i];
						pnts[i] = trs.TransformPoint(pnt);
					}
					return pnts.GetBoundsRect();
				}
			}
			return output.SetToPositiveSize();
		}
	}
}
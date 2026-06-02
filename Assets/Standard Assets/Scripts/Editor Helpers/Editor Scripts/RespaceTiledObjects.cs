#if UNITY_EDITOR
using Extensions;
using UnityEngine;

namespace SlimeJump
{
	public class RespaceTiledObjects : EditorScript
	{
		public Transform[] parents = new Transform[0];
		public float distance;

		public override void Do ()
		{
			for (int i = 0; i < parents.Length; i ++)
			{
				Transform parent = parents[i];
				float oldDistance = Mathf.Infinity;
				for (int i2 = 0; i2 < parent.childCount; i2 ++)
				{
					Transform child = parent.GetChild(i2);
					oldDistance = Mathf.Min(oldDistance, child.localPosition.magnitude);
				}
				for (int i2 = 0; i2 < parent.childCount; i2 ++)
				{
					Transform child = parent.GetChild(i2);
					child.localPosition = child.localPosition.normalized * Mathf.Round(child.localPosition.Snap(Vector2.one * oldDistance).magnitude / oldDistance) * distance / parent.lossyScale.x;
				}
			}
		}
	}
}
#else
namespace SlimeJump
{
	public class RespaceTiledObjects : EditorScript
	{
	}
}
#endif
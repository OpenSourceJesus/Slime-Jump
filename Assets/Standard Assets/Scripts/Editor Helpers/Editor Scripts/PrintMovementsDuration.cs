#if UNITY_EDITOR
using UnityEngine;

namespace SlimeJump
{
	public class PrintMovementsDuration : EditorScript
	{
		public Transform[] pathTransforms = new Transform[0];

		public override void Do ()
		{
			float duration = 0;
			Transform previousPathTrs = pathTransforms[0];
			for (int i = 1; i < pathTransforms.Length; i ++)
			{
				Transform pathTrs = pathTransforms[i];
				duration += 1f / Player.Instance.moveSpeed * (pathTrs.position.x - previousPathTrs.position.x + Player.instance.collider.bounds.extents.x);
				previousPathTrs = pathTrs;
			}
			print(duration);
		}
	}
}
#else
namespace SlimeJump
{
	public class PrintMovementsDuration : EditorScript
	{
	}
}
#endif
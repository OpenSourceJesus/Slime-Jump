#if UNITY_EDITOR
using Extensions;
using UnityEngine;
using UnityEditor;

namespace SlimeJump
{
	public class SetZPositionsToYPositionsWithOffset : EditorScript
	{
		public Transform[] transforms = new Transform[0];
		public float offset;

		public override void Do ()
		{
			foreach (Transform trs in transforms)
				trs.position = trs.position.SetZ(trs.position.y + offset);
		}

		[MenuItem("Tools/Set Z positions to Y positions for selected transforms")]
		static void DoForSelected ()
		{
			foreach (Transform trs in Selection.transforms)
				trs.position = trs.position.SetZ(trs.position.y);
		}
	}
}
#else
namespace SlimeJump
{
	public class SetZPositionsToYPositionsWithOffset : EditorScript
	{
	}
}
#endif
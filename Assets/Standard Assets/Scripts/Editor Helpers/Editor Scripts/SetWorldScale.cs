#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Extensions;

namespace SlimeJump
{
	[ExecuteInEditMode]
	public class SetWorldScale : EditorScript
	{
		public Transform trs;
		public Vector3 scale;
		
		public override void Do ()
		{
			trs.SetWorldScale(scale);
		}
	}
}
#else
namespace SlimeJump
{
	public class SetWorldScale : EditorScript
	{
	}
}
#endif
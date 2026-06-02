#if UNITY_EDITOR
using UnityEngine;
using System.Collections.Generic;

namespace SlimeJump
{
	public class DeleteFractionOfGameObjects : EditorScript
	{
		public List<GameObject> gos = new List<GameObject>();
		public float fraction;

		public override void Do ()
		{
			for (float f = 0; f < gos.Count; f += 1f / fraction)
			{
				GameObject go = gos[(int) f];
				GameManager.DestroyOnNextEditorUpdate (go);
				gos.RemoveAt((int) f);
				f --;
			}
		}
	}
}
#else
namespace SlimeJump
{
	public class DeleteFractionOfGameObjects : EditorScript
	{
	}
}
#endif
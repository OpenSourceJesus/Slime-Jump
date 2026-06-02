#if UNITY_EDITOR
using UnityEngine;

namespace SlimeJump
{
	public class Test2 : EditorScript
	{
		public Tooltip[] tooltips = new Tooltip[0];

		public override void Do ()
		{
			for (int i = 0; i < tooltips.Length; i ++)
			{
				Tooltip tooltip = tooltips[i];
				tooltip.onSelect = tooltip.onShow;
				tooltip.onDeslect = tooltip.onHide;
			}
		}
	}
}
#else
namespace SlimeJump
{
	public class Test2 : EditorScript
	{
	}
}
#endif
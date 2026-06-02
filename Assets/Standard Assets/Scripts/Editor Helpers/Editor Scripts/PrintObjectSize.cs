#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace SlimeJump
{
	public class PrintObjectSize : EditorScript
	{
		public SpriteRenderer spriteRenderer;

		public override void Do ()
		{
			if (spriteRenderer == null)
				spriteRenderer = GetComponent<SpriteRenderer>();
			Do (spriteRenderer);
		}

		static void Do (SpriteRenderer spriteRenderer)
		{
			Vector2 size = spriteRenderer.bounds.size;
			print(Mathf.Max(size.x, size.y));
		}

		[MenuItem("Tools/Print object size")]
		static void DoForSelected ()
		{
			Do (Selection.activeTransform.GetComponent<SpriteRenderer>());
		}
	}
}
#else
namespace SlimeJump
{
	public class PrintObjectSize : EditorScript
	{
	}
}
#endif
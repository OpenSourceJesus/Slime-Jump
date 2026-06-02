#if UNITY_EDITOR
using Extensions;
using UnityEngine;
using UnityEditor;
using UnityEngine.Tilemaps;

namespace SlimeJump
{
	public class DoEditorScripts : EditorScript
	{
		public EditorScript[] editorScripts = new EditorScript[0];

		public override void Do ()
		{
			for (int i = 0; i < editorScripts.Length; i ++)
			{
				EditorScript editorScript = editorScripts[i];
				editorScript.Do ();
			}
		}
	}
}
#else
namespace SlimeJump
{
	public class DoEditorScripts : EditorScript
	{
	}
}
#endif
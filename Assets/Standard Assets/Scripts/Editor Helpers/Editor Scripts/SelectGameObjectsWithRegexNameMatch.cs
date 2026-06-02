#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace SlimeJump
{
	[ExecuteInEditMode]
	public class SelectGameObjectsWithRegexNameMatch : EditorScript
	{
		public _Regex regex;

		public override void Do ()
		{
			List<GameObject> selected = new List<GameObject>();
			GameObject[] allGameObjects = FindObjectsOfType<GameObject>();
			for (int i = 0; i < allGameObjects.Length; i ++)
			{
				GameObject go = allGameObjects[i];
				if (new Regex(regex.pattern, regex.options).IsMatch(go.name))
					selected.Add(go);
			}
			Selection.objects = selected.ToArray();
		}
	}
}
#else
namespace SlimeJump
{
	public class SelectGameObjectsWithRegexNameMatch : EditorScript
	{
	}
}
#endif
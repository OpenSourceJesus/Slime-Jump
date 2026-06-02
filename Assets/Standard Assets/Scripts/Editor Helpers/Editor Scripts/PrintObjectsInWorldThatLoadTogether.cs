#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace SlimeJump
{
	[ExecuteInEditMode]
	public class PrintObjectsInWorldThatLoadTogether : EditorScript
	{
		public BoxCollider2D boxCollider;

		public override void Do ()
		{
			List<ObjectInWorld> worldObjects = new List<ObjectInWorld>(FindObjectsOfType<ObjectInWorld>());
			if (boxCollider != null)
			{
				for (int i = 0; i < worldObjects.Count; i ++)
				{
					ObjectInWorld worldObject = worldObjects[i];
					if (!boxCollider.bounds.Contains(worldObject.trs.position))
					{
						worldObjects.RemoveAt(i);
						i --;
					}
				}
			}
			_Do (worldObjects.ToArray());
		}

		[MenuItem("Tools/Print Objects In World That Load Together")]
		static void _Do (ObjectInWorld[] worldObjects)
		{
			foreach (ObjectInWorld worldObject in worldObjects)
				if (worldObject.objectsToLoadAndUnloadWithMe.Length > 1)
					print(worldObject);
		}
	}
}
#else
namespace SlimeJump
{
	public class PrintObjectsInWorldThatLoadTogether : EditorScript
	{
	}
}
#endif
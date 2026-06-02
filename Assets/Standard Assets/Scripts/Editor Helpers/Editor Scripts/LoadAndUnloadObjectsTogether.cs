#if UNITY_EDITOR
using UnityEngine;
using System.Collections.Generic;

namespace SlimeJump
{
	public class LoadAndUnloadObjectsTogether : EditorScript
	{
		public ObjectInWorld[] worldObjects = new ObjectInWorld[0];

		public override void Do ()
		{
			for (int i = 0; i < worldObjects.Length; i ++)
			{
				ObjectInWorld worldObject = worldObjects[i];
				worldObject.objectsToLoadAndUnloadWithMe = new List<ObjectInWorld>(worldObjects).ToArray();
			}
		}
	}
}
#else
namespace SlimeJump
{
	public class LoadAndUnloadObjectsTogether : EditorScript
	{
	}
}
#endif
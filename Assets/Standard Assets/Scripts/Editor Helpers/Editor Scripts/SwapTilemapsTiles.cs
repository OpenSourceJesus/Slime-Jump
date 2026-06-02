#if UNITY_EDITOR
using Extensions;
using UnityEngine;
using UnityEditor;
using UnityEngine.Tilemaps;

namespace SlimeJump
{
	public class SwapTilemapsTiles : EditorScript
	{
		public Tilemap[] tilemaps = new Tilemap[0];
		public TileBase from;
		public TileBase to;

		public override void Do ()
		{
			for (int i = 0; i < tilemaps.Length; i ++)
			{
				Tilemap tilemap = tilemaps[i];
				tilemap.SwapTile(from, to);
			}
		}
	}
}
#else
namespace SlimeJump
{
	public class SwapTilemapsTiles : EditorScript
	{
	}
}
#endif
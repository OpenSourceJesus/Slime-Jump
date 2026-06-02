#if UNITY_EDITOR
using Extensions;
using UnityEngine;
using UnityEditor;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

namespace SlimeJump
{
	public class MakeTilemapTileGameObjects : EditorScript
	{
		public Tilemap tilemap;
		public GameObject[] gos = new GameObject[0];

		public override void Do ()
		{
			if (tilemap == null)
				tilemap = GetComponent<Tilemap>();
			for (int i = 0; i < gos.Length; i ++)
			{
				GameObject go = gos[i];
				GameManager.DestroyOnNextEditorUpdate (go);
			}
			List<GameObject> _gos = new List<GameObject>();
			foreach (Vector3Int cellPosition in tilemap.cellBounds.allPositionsWithin)
			{
				Tile tile = tilemap.GetTile(cellPosition) as Tile;
				if (tile != null)
					_gos.Add(Instantiate(tile.gameObject, tilemap.GetCellCenterWorld(cellPosition), Quaternion.identity));
			}
			gos = _gos.ToArray();
		}
	}
}
#else
namespace SlimeJump
{
	public class MakeTilemapTileGameObjects : EditorScript
	{
	}
}
#endif
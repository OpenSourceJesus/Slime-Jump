#if UNITY_EDITOR
using Extensions;
using UnityEngine;
using UnityEditor;
using UnityEngine.Tilemaps;

namespace SlimeJump
{
	public class MakeTilemapInverseOfTilemaps : EditorScript
	{
		public Tilemap[] invertByTilemaps = new Tilemap[0];
		public Tilemap invertTilemap;
		public TileBase tile;

		public override void Do ()
		{
			BoundsInt[] boundsArray = new BoundsInt[invertByTilemaps.Length];
			for (int i = 0; i < invertByTilemaps.Length; i ++)
			{
				Tilemap tilemap = invertByTilemaps[i];
				tilemap.CompressBounds();
				boundsArray[i] = tilemap.cellBounds;
			}
			BoundsInt cellBounds = boundsArray.Combine();
			foreach (Vector3Int cellPosition in cellBounds.allPositionsWithin)
			{
				bool hasTile = false;
				for (int i = 0; i < invertByTilemaps.Length; i ++)
				{
					Tilemap tilemap = invertByTilemaps[i];
					if (tilemap.HasTile(cellPosition))
					{
						hasTile = true;
						break;
					}
				}
				if (!hasTile)
					invertTilemap.SetTile(cellPosition, tile);
				else
					invertTilemap.SetTile(cellPosition, null);
			}
		}
	}
}
#else
namespace SlimeJump
{
	public class MakeTilemapInverseOfTilemaps : EditorScript
	{
	}
}
#endif
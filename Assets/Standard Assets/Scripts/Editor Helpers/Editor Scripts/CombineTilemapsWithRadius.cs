#if UNITY_EDITOR
using Extensions;
using UnityEngine;
using UnityEditor;
using UnityEngine.Tilemaps;

namespace SlimeJump
{
	public class CombineTilemapsWithRadius : EditorScript
	{
		public Tilemap[] tilemaps = new Tilemap[0];
		public Tilemap combineToTilemap;
		public uint cellRadius;
		public TileBase radiusTile;

		public override void Do ()
		{
			combineToTilemap.ClearAllTiles();
			BoundsInt[] boundsArray = new BoundsInt[tilemaps.Length];
			for (int i = 0; i < tilemaps.Length; i ++)
			{
				Tilemap tilemap = tilemaps[i];
				tilemap.CompressBounds();
				boundsArray[i] = tilemap.cellBounds;
			}
			BoundsInt cellBounds = boundsArray.Combine();
			foreach (Vector3Int cellPosition in cellBounds.allPositionsWithin)
			{
				for (int i = 0; i < tilemaps.Length; i ++)
				{
					Tilemap tilemap = tilemaps[i];
					if (tilemap.HasTile(cellPosition))
					{
						BoundsInt radiusCellBounds = new BoundsInt();
						radiusCellBounds.SetMinMax(cellPosition - (Vector2Int.one * (int) cellRadius).ToVec3Int(), cellPosition + new Vector3Int((int) cellRadius, (int) cellRadius, 1));
						combineToTilemap.SetTilesBlock(radiusCellBounds, CollectionExtensions.GetHomogenized(radiusTile, (uint) radiusCellBounds.GetVolume()));
					}
				}
			}
		}
	}
}
#else
namespace SlimeJump
{
	public class CombineTilemapsWithRadius : EditorScript
	{
	}
}
#endif
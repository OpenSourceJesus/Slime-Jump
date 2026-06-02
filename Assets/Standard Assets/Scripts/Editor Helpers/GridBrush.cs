#if UNITY_EDITOR
using SlimeJump;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

[CustomGridBrush(true, false, false, "Default")]
public class GridBrush : GridBrushBase
{
	public static Tilemap activeTilemap;
	public static Tilemap ActiveTilemap
	{
		get
		{
			Tilemap output;
			foreach (Transform trs in Selection.transforms)
			{
				output = trs.GetComponent<Tilemap>();
				if (output != null)
					return output;
			}
			return activeTilemap;
		}
		set
		{
			activeTilemap = value;
		}
	}
	public static TileBase activeTile;

	public virtual void OnEnable ()
	{
		if (!Application.isPlaying)
			EditorApplication.update += DoUpdate;
	}

	public virtual void OnDisable ()
	{
		if (!Application.isPlaying)
			EditorApplication.update -= DoUpdate;
	}

	public virtual void DoUpdate ()
	{
		TileBase tile = Selection.activeObject as TileBase;
		if (tile != null)
			activeTile = tile;
	}

	public override void Paint (GridLayout grid, GameObject brushTarget, Vector3Int position)
	{
		base.Paint (grid, brushTarget, position);
		if (brushTarget != null)
			ActiveTilemap = brushTarget.GetComponent<Tilemap>();
		else if (brushTarget == null && ActiveTilemap != null)
			brushTarget = ActiveTilemap.gameObject;
		ActiveTilemap.SetTile(position, activeTile);
	}

	public override void Select (GridLayout grid, GameObject brushTarget, BoundsInt bounds)
	{
		if (brushTarget == null && ActiveTilemap != null)
			brushTarget = ActiveTilemap.gameObject;
		base.Select (grid, brushTarget, bounds);
		ActiveTilemap = brushTarget.GetComponent<Tilemap>();
		activeTile = ActiveTilemap.GetTile(bounds.min);
	}

	public override void Pick (GridLayout gridLayout, GameObject brushTarget, BoundsInt position, Vector3Int pivot)
	{
		if (brushTarget == null && ActiveTilemap != null)
			brushTarget = ActiveTilemap.gameObject;
		base.Pick (gridLayout, brushTarget, position, pivot);
		ActiveTilemap = brushTarget.GetComponent<Tilemap>();
		activeTile = ActiveTilemap.GetTile(pivot);
	}

	public override void Erase (GridLayout grid, GameObject brushTarget, Vector3Int position)
	{
		if (brushTarget == null && ActiveTilemap != null)
			brushTarget = ActiveTilemap.gameObject;
		base.Erase (grid, brushTarget, position);
		ActiveTilemap = brushTarget.GetComponent<Tilemap>();
		ActiveTilemap.SetTile(position, null);
	}
}
#endif
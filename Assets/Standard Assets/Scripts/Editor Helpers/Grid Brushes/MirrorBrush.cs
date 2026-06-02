#if UNITY_EDITOR
using System;
using Extensions;
using UnityEngine;
using UnityEditor;
using System.Collections;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

namespace ArcherGame
{
	[CustomGridBrush(false, false, false, "Mirror Brush")]
	public class MirrorBrush : GridBrush
	{
		public Vector3 origin;
		public bool mirrorX;
		public bool mirrorY;
		public bool mirrorZ;
		public bool radialSymmetry;
		
		public override void Paint (GridLayout grid, GameObject brushTarget, Vector3Int position)
		{
			base.Paint (grid, brushTarget, position);
			Vector3 toOrigin = origin - position;
			if (radialSymmetry)
				base.Paint (grid, brushTarget, position + (toOrigin * 2).ToVec3Int());
			if (mirrorX)
				base.Paint (grid, brushTarget, position + Vector3Int.right * (toOrigin * 2).ToVec3Int().x);
			if (mirrorY)
				base.Paint (grid, brushTarget, position + Vector3Int.up * (toOrigin * 2).ToVec3Int().y);
			if (mirrorZ)
				base.Paint (grid, brushTarget, position + new Vector3Int(0, 0, 1) * (toOrigin * 2).ToVec3Int().z);
		}
		
		public override void Erase (GridLayout grid, GameObject brushTarget, Vector3Int position)
		{
			base.Erase (grid, brushTarget, position);
			Vector3 toOrigin = origin - position;
			if (radialSymmetry)
				base.Erase (grid, brushTarget, position + (toOrigin * 2).ToVec3Int());
			if (mirrorX)
				base.Erase (grid, brushTarget, position + Vector3Int.right * (toOrigin * 2).ToVec3Int().x);
			if (mirrorY)
				base.Erase (grid, brushTarget, position + Vector3Int.up * (toOrigin * 2).ToVec3Int().y);
			if (mirrorZ)
				base.Erase (grid, brushTarget, position + new Vector3Int(0, 0, 1) * (toOrigin * 2).ToVec3Int().z);
		}
		
		// public override void Select (GridLayout grid, GameObject brushTarget, BoundsInt bounds)
		// {
		// 	base.Select (grid, brushTarget, bounds);
		// 	Vector3 toOrigin = origin - bounds.center;
		// 	if (radialSymmetry)
		// 		base.Select (grid, brushTarget, bounds.Move((toOrigin * 2).ToVec3Int()));
		// 	if (mirrorX)
		// 		base.Select (grid, brushTarget, bounds.Move(Vector3Int.right * (toOrigin * 2).ToVec3Int().x));
		// 	if (mirrorY)
		// 		base.Select (grid, brushTarget, bounds.Move(Vector3Int.up * (toOrigin * 2).ToVec3Int().y));
		// 	if (mirrorZ)
		// 		base.Select (grid, brushTarget, bounds.Move(new Vector3Int(0, 0, 1) * (toOrigin * 2).ToVec3Int().z));
		// }
	}

	[CustomEditor(typeof(MirrorBrush))]
	public class MirrorBrushEditor : Editor
	{
		MirrorBrush MirrorBrush
		{
			get
			{
				return (MirrorBrush) target;
			}
		}
		
		public virtual void OnSceneGUI ()
		{
		}
	}
}
#endif
#if UNITY_EDITOR
using Extensions;
using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

namespace SlimeJump
{
	[ExecuteInEditMode]
	public class TestTrim_ConvexPolygonZone : EditorScript
	{
		public Collider2D collider; 
		public LineSegment2D lineSegment;
		public bool trimClockwiseSideOfLineSegmentStart;
		public Shape2D output;

		public override void Do ()
		{
			Zone2D zone = Zone2D.Gen(collider);
			output = zone.Trim_ConvexPolygon(lineSegment, trimClockwiseSideOfLineSegmentStart);
			GizmosManager.gizmosEntries.Clear();
			lineSegment.DrawGizmos (Color.yellow);
			output.DrawGizmos (Color.red);
		}
	}
}
#else
namespace SlimeJump
{
	public class Untitled : EditorScript
	{
	}
}
#endif
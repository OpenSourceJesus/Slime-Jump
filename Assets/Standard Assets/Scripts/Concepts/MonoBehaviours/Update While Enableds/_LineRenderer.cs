using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace SlimeJump
{
	[ExecuteInEditMode]
	[RequireComponent(typeof(LineRenderer))]
	public class _LineRenderer : UpdateWhileEnabled
	{
		public LineRenderer lineRenderer;
		public Transform[] points = new Transform[0];

		public override void OnEnable ()
		{
#if UNITY_EDITOR
			if (!Application.isPlaying)
			{
				if (lineRenderer == null)
					lineRenderer = GetComponent<LineRenderer>();
				DoUpdate ();
				return;
			}
#endif
			base.OnEnable ();
		}

		public override void DoUpdate ()
		{
			if (points.Length == 0)
				return;
			lineRenderer.positionCount = points.Length;
			for (int i = 0; i < points.Length; i ++)
				lineRenderer.SetPosition(i, points[i].position);
		}
	}
}
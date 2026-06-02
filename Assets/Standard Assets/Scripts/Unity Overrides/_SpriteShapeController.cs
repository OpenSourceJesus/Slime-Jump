using System;
using Extensions;
using UnityEngine;
using UnityEngine.U2D;
using System.Collections;
using System.Collections.Generic;

namespace SlimeJump
{
	[DisallowMultipleComponent]
	public class _SpriteShapeController : SpriteShapeController
	{
#if UNITY_EDITOR
		public Vector2 pivotOffset;
		public bool recenter;
		public Transform trs;
		public float snapInterval;
		public Cutout[] cutouts = new Cutout[0];
		public SpriteShapeRenderer spriteShapeRenderer;
		[HideInInspector]
		public Cutout[] previousCutouts = new Cutout[0];
		public ShapeTangentMode tangentMode;

		void OnValidate ()
		{
			if (trs == null)
				trs = GetComponent<Transform>();
			if (spriteShapeRenderer == null)
				spriteShapeRenderer = GetComponent<SpriteShapeRenderer>();
			Vector2 center = new Vector2();
			for (int i = 0; i < spline.GetPointCount(); i ++)
			{
				Vector2 point = spline.GetPosition(i);
				point = trs.InverseTransformPoint(trs.TransformPoint(point).Snap(Vector2.one * snapInterval));
				spline.SetPosition(i, point);
				center += (Vector2) trs.TransformPoint(point);
				spline.SetTangentMode(i, tangentMode);
			}
			if (recenter)
			{
				recenter = false;
				center /= spline.GetPointCount();
				center += pivotOffset;
				Vector2 previousPosition = trs.position;
				trs.position = center.SetZ(trs.position.z);
				for (int i = 0; i < spline.GetPointCount(); i ++)
				{
					Vector2 point = spline.GetPosition(i);
					point += previousPosition - (Vector2) trs.position;
					spline.SetPosition(i, point);
				}
			}
			for (int i = 0; i < cutouts.Length; i ++)
			{
				Cutout cutout = cutouts[i];
				if (cutout.Equals(default(Cutout)))
				{
					_SpriteShapeController shapeController = new GameObject().AddComponent<_SpriteShapeController>();
					cutout = new Cutout(spriteShapeRenderer.color.a, shapeController);
					cutouts[i] = cutout;
				}
			}
			if (cutouts.Length != previousCutouts.Length)
			{
				for (int i = 0; i < previousCutouts.Length; i ++)
				{
					Cutout previousCutout = previousCutouts[i];
					bool foundShapeController = false;
					for (int i2 = 0; i2 < cutouts.Length; i2 ++)
					{
						Cutout cutout = cutouts[i2];
						if (cutout.shapeController == previousCutout.shapeController)
						{
							foundShapeController = true;
							break;
						}
					}
					if (foundShapeController)
						GameManager.DestroyOnNextEditorUpdate (previousCutout.shapeController.gameObject);
				}
			}
			previousCutouts = new Cutout[cutouts.Length];
			cutouts.CopyTo(previousCutouts, 0);
		}

		[Serializable]
		public struct Cutout
		{
			public float alpha;
			public _SpriteShapeController shapeController;

			public Cutout (float alpha, _SpriteShapeController shapeController)
			{
				this.alpha = alpha;
				this.shapeController = shapeController;
			}
		}
#endif
	}
}
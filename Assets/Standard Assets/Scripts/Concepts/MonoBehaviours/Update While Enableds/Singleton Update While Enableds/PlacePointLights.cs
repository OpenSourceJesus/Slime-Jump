#if UNITY_EDITOR
using System;
using Extensions;
using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Rendering.Universal;
using Random = UnityEngine.Random;

namespace SlimeJump
{
	public class PlacePointLights : EditorScript
	{
		public Transform lightsParent;
		public Collider2D[] colliders = new Collider2D[0];
		public Light2D ligthPrefab;
		public int targetPlaceCnt;
		public int maxRetries;
		public float maxOverlap;
		public BoxCollider2D placementBoxCollider;
		public FloatRange intensityRange = new FloatRange(0, float.MaxValue);
		public FloatRange falloffRange = new FloatRange(0, float.MaxValue);
		public FloatRange falloffStrengthRange = new FloatRange(0, 1);
		public bool useValidColorsGradient;
		public Gradient validColorsGraidient;
		public ColorPalette validColorPalette;

		public override void Do ()
		{
			List<Collider2D> _colliders = new List<Collider2D>(colliders);
			for (int i = 0; i < _colliders.Count; i ++)
			{
				Collider2D collder = _colliders[i];
				if (collder == null || !collder.gameObject.activeInHierarchy)
				{
					_colliders.RemoveAt(i);
					i --;
				}
			}
			colliders = _colliders.ToArray();
			for (int i = 0; i < targetPlaceCnt; i ++)
			{
				for (int i2 = 0; i2 < maxRetries; i2 ++)
				{
					Vector2 pos = placementBoxCollider.bounds.ToRect().RandomPoint();
					bool hit = false;
					float falloff = falloffRange.Get(Random.value);
					float falloffStrength = falloffStrengthRange.Get(Random.value);
					float lightRange = falloff * (1f - falloffStrength);
					for (int i3 = 0; i3 < colliders.Length; i3 ++)
					{
						Collider2D collider = colliders[i3];
						if (collider.bounds.ToRect().Grow(Vector2.one * lightRange).Contains(pos))
						{
							hit = true;
							break;
						}
					}
					if (!hit)
						continue;
					Light2D light = Instantiate(ligthPrefab, pos, Quaternion.identity, lightsParent);
					light.intensity = intensityRange.Get(Random.value);
					light.shapeLightFalloffSize = falloff;
					light.falloffIntensity = falloffStrength;
					if (useValidColorsGradient)
						light.color = validColorsGraidient.Evaluate(Random.value);
					else
						light.color = validColorPalette.Get(Random.value);
					bool isLightValid = true;
					Light2D[] lights = lightsParent.GetComponentsInChildren<Light2D>();
					for (int i3 = 0; i3 < lights.Length; i3 ++)
					{
						Light2D _light = lights[i3];
						float _lightRange = _light.shapeLightFalloffSize * (1f - _light.falloffIntensity);
						float maxSeparation = lightRange + _lightRange - maxOverlap;
						if (light != _light && (light.transform.position - _light.transform.position).sqrMagnitude < maxSeparation * maxSeparation)
						{
							isLightValid = false;
							break;
						}
					}
					if (isLightValid)
					{
						print("Made a light");
						break;
					}
					light.transform.SetParent(null);
					GameManager.DestroyOnNextEditorUpdate (light.gameObject);
					if (i2 == maxRetries - 1)
						print("Didn't make a light");
				}
			}
		}

		[Serializable]
		public struct ColorPalette
		{
			public AnimationCurve redCurve;
			public AnimationCurve greenCurve;
			public AnimationCurve blueCurve;

			public Color Get (float value)
			{
				return new Color(redCurve.Evaluate(value), greenCurve.Evaluate(value), blueCurve.Evaluate(value));
			}
		}
	}
}
#else
namespace SlimeJump
{
	public class PlacePointLights : EditorScript
	{
	}
}
#endif
using Extensions;
using UnityEngine;
using System.Collections.Generic;

namespace SlimeJump
{
	public class LaserShooterLaser : UpdateWhileEnabled
	{
		public Transform trs;
		public LineRenderer lineRenderer;
		public Collider2D collider;
		public Color activatedColor;
		public float activateDelay;
		public float duration;
		public AudioClip shootSound;
		public float shootSoundVolume;
		public static List<LaserShooterLaser> instances = new List<LaserShooterLaser>();
		float timeActivated;
		float timeCreated;
		bool activated;

		public override void OnEnable ()
		{
			base.OnEnable ();
			timeCreated = Time.time;
			instances.Add(this);
		}

		public override void OnDisable ()
		{
			base.OnDisable ();
			instances.Remove(this);
		}

		public override void DoUpdate ()
		{
			if (activated)
			{
				float normalizedDurationRemaining = (Time.time - timeActivated) / duration;
				lineRenderer.widthMultiplier = 1f - normalizedDurationRemaining;
				trs.localScale = trs.localScale.SetX(1f - normalizedDurationRemaining);
			}
			else if (Time.time - timeCreated >= activateDelay)
			{
				lineRenderer.startColor = activatedColor;
				lineRenderer.endColor = activatedColor;
				collider.enabled = true;
				activated = true;
				timeActivated = Time.time;
				Destroy(gameObject, duration);
				AudioManager.instance.MakeSoundEffect (shootSound, trs.position, shootSoundVolume);
			}
		}
	}
}
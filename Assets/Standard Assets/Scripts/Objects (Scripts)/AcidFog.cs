using Extensions;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace SlimeJump
{
	public class AcidFog : UpdateWhileEnabled
	{
		public Collider2D collider;
		public float killTime;
		float killTimer;
		bool playerIsInside;

		void OnTriggerEnter2D (Collider2D other)
		{
			if (WorldMap.isOpen || playerIsInside)
				return;
			killTimer = killTime;
			playerIsInside = true;
			Player.instance.acidBarTrs.parent.gameObject.SetActive(true);
			Player.instance.inAcidFogAnimationEntry.Play ();
		}

		public override void DoUpdate ()
		{
			if (WorldMap.isOpen || !playerIsInside)
				return;
			List<Collider2D> hitColliders = new List<Collider2D>();
			collider.Overlap(hitColliders);
			hitColliders.Remove(Player.instance.climbableSensor);
			hitColliders.Remove(Player.instance.wallSensor);
			if (!WorldMap.isOpen && hitColliders.Count == 0)
			{
				playerIsInside = false;
				Player.instance.acidBarTrs.parent.gameObject.SetActive(false);
				Player.instance.idleAnimationEntry.Play ();
				return;
			}
			killTimer -= Time.deltaTime;
			if (killTimer <= 0)
				Player.instance.Death (Player.instance);
			else
				Player.instance.acidBarTrs.localScale = Player.instance.acidBarTrs.localScale.SetX(killTimer / killTime);
		}
	}
}
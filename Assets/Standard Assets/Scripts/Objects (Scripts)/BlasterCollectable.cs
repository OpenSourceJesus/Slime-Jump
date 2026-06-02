using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace SlimeJump
{
	public class BlasterCollectable : MonoBehaviour
	{
		void Awake ()
		{
			if (Blaster.Collected)
				Destroy(gameObject);
		}

		void OnTriggerEnter2D (Collider2D other)
		{
			if (other != Player.instance.climbableSensor && other != Player.instance.wallSensor)
			{
				Blaster.Collected = true;
				Blaster.Instance.gameObject.SetActive(true);
				Blaster.instance.OnGain (Player.instance);
				Player.instance.toggleShootBlasterImage.gameObject.SetActive(true);
				Destroy(gameObject);
			}
		}

		void OnCollisionEnter2D (Collision2D coll)
		{
			OnTriggerEnter2D (coll.collider);
		}
	}
}
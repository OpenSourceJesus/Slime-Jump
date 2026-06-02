using UnityEngine;

namespace SlimeJump
{
	public class LassoCollectable : MonoBehaviour
	{
		void Awake ()
		{
			if (Lasso.Collected)
				Destroy(gameObject);
		}

		void OnTriggerEnter2D (Collider2D other)
		{
			if (other != Player.instance.climbableSensor && other != Player.instance.wallSensor)
			{
				Lasso.Collected = true;
				Lasso.instance.gameObject.SetActive(true);
				Player.instance.toggleShootLassoImage.gameObject.SetActive(true);
				Destroy(gameObject);
			}
		}

		void OnCollisionEnter2D (Collision2D coll)
		{
			OnTriggerEnter2D (coll.collider);
		}
	}
}
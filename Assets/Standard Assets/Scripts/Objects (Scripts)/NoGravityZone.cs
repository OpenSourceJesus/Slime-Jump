using UnityEngine;

namespace SlimeJump
{
	public class NoGravityZone : MonoBehaviour
	{
		void OnTriggerEnter2D (Collider2D other)
		{
			Player.instance.inNoGravityZone = true;
		}

		void OnTriggerExit2D (Collider2D other)
		{
			Player.instance.inNoGravityZone = false;
		}
	}
}
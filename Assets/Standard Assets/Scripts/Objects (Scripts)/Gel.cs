using UnityEngine;
using System.Collections.Generic;

namespace SlimeJump
{
	public class Gel : MonoBehaviour
	{
		public float addToDrag;
		public float addTomultSpeed;
		static List<Rigidbody2D> rigidbodiesInsideMe = new List<Rigidbody2D>();

		void OnTriggerEnter2D (Collider2D other)
		{
			Rigidbody2D rigid = other.GetComponent<Rigidbody2D>();
			if (rigid != null)
			{
				if (!rigidbodiesInsideMe.Contains(rigid))
				{
					Player player = other.GetComponent<Player>();
					if (player != null)
						player.multSpeed += addTomultSpeed;
					rigid.linearDamping += addToDrag;
				}
				rigidbodiesInsideMe.Add(rigid);
			}
			else
				Lasso.Instance.multiplyShootAndChangeLengthSpeed += addTomultSpeed;
		}

		void OnTriggerExit2D (Collider2D other)
		{
			Rigidbody2D rigid = other.GetComponent<Rigidbody2D>();
			if (rigid != null)
			{
				rigidbodiesInsideMe.Remove(rigid);
				if (!rigidbodiesInsideMe.Contains(rigid))
				{
					Player player = other.GetComponent<Player>();
					if (player != null)
						player.multSpeed -= addTomultSpeed;
					rigid.linearDamping -= addToDrag;
				}
			}
			else
				Lasso.Instance.multiplyShootAndChangeLengthSpeed -= addTomultSpeed;
		}
	}
}
using Extensions;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace SlimeJump
{
	public class HellPortal : Hazard
	{
		void OnTriggerEnter2D (Collider2D other)
		{
			IDestructable destructable = other.GetComponent<IDestructable>();
			if (destructable != null)
			{
				Player player = destructable as Player;
				if (player != null)
					player.Death (player);
				else
				{
					other.gameObject.SetActive(false);
					destructable.Death (null);
				}
			}
		}
	}
}
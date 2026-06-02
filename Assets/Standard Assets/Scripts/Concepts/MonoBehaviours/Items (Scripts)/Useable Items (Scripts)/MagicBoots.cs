using UnityEngine;

namespace SlimeJump
{
	public class MagicBoots : UseableItem
	{
		public float addToMoveSpeed;
		public GameObject usingIndicator;

		public override void OnBeganUsing ()
		{
			Player.instance.invulnerable = true;
			Player.instance.moveSpeed += addToMoveSpeed;
			usingIndicator.SetActive(true);
			base.OnBeganUsing ();
        }

		public override void OnStopUsing ()
		{
			if (Player.instance == null || Player.instance.gameObject == null)
				return;
			usingIndicator.SetActive(false);
			Player.instance.moveSpeed -= addToMoveSpeed;
			Player.instance.invulnerable = false;
			base.OnStopUsing ();
        }
	}
}
using UnityEngine;

namespace SlimeJump
{
	public class PortalItem : UseableItem
	{
		public Transform portalTrs;

		public override void OnGain (Player player)
		{
			base.OnGain (player);
			portalTrs.SetParent(null);
		}

		public override void Use ()
		{
			base.Use ();
			if (portalTrs.gameObject.activeSelf)
			{
				Player.instance.trs.position = portalTrs.position;
				portalTrs.gameObject.SetActive(false);
			}
			else
			{
				portalTrs.position = Player.instance.trs.position;
				portalTrs.gameObject.SetActive(true);
			}
		}
	}
}
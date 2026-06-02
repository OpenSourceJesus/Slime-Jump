using TMPro;
using Extensions;
using UnityEngine;

namespace SlimeJump
{
	public class WinScreen : SingletonMonoBehaviour<WinScreen>
	{
		public TMP_Text text;
		public string forDemoStr;
		public string forNotDemoStr;
		public Transform confettiParent;

		void OnEnable ()
		{
			string str = forNotDemoStr;
			if (GameManager.instance.isDemo)
				str = forDemoStr;
			text.text = str.Replace("_", "" + Player.Gems);
			text.text = text.text.Replace("|", "" + Gem.instances.Length);
			confettiParent.SetParent(null);
			confettiParent.localScale = Vector3.one;
			foreach (Transform child in confettiParent)
				child.position = GameCamera.instance.camera.ViewportToWorldPoint(new Vector2(Random.value, Random.value)).SetZ(0);
		}
	}
}
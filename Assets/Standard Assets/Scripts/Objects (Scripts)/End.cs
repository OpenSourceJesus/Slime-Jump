using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace SlimeJump
{
	public class End : MonoBehaviour
	{
		public AudioClip hitSound;
		public float hitSoundVolume;
		public bool forDemo;

		void OnTriggerEnter2D (Collider2D other)
		{
			if (other != Player.instance.climbableSensor && other != Player.instance.wallSensor && GameManager.instance.isDemo == forDemo)
			{
				SoundEffect soundEffect = AudioManager.instance.MakeSoundEffect(hitSound, Vector3.zero, hitSoundVolume);
				soundEffect.audioSource.spatialBlend = 0;
				WinScreen.Instance.gameObject.SetActive(true);
				GameManager.SetPaused (true);
				if (!forDemo)
					return;
				WinAchievement winAchievement = null;
				for (int i = 0; i < WinAchievement.instances.Length; i ++)
				{
					WinAchievement _winAchievement = WinAchievement.instances[i];
					if (_winAchievement.sceneName == _SceneManager.CurrentScene.name)
					{
						winAchievement = _winAchievement;
						break;
					}
				}
				if (!winAchievement.Achieved)
					CosmeticsMenu.AddPoints (CosmeticsMenu.Instance.pointsPerSavePoint);
				winAchievement.Achieved = true;
				winAchievement.HandleAchieve ();
			}
		}

		void OnCollisionEnter2D (Collision2D coll)
		{
			OnTriggerEnter2D (coll.collider);
		}
	}
}
using Extensions;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace SlimeJump
{
	public class Gem : MonoBehaviour
	{
		public SpriteRenderer[] spriteRenderers = new SpriteRenderer[0];
		public AudioClip pickUpSound;
		public float pickUpSoundVolume;
		public GameObject confetiiGo;
		public bool Collected
		{
			get
			{
				return SaveAndLoadManager.GetBool("Collected " + _SceneManager.CurrentScene.name + ' ' + name, false);
			}
			set
			{
				SaveAndLoadManager.SetBool ("Collected " + _SceneManager.CurrentScene.name + ' ' + name, value);
			}
		}
		public static Gem[] instances = new Gem[0];

		public void Start ()
		{
			if (Collected)
				for (int i = 0; i < spriteRenderers.Length; i ++)
				{
					SpriteRenderer spriteRenderer = spriteRenderers[i];
					spriteRenderer.color = spriteRenderer.color.SetAlpha(0.25f);
				}
		}

		public void OnTriggerEnter2D (Collider2D other)
		{
			if (other != Player.instance.climbableSensor && other != Player.instance.wallSensor)
			{
				confetiiGo.transform.SetParent(null);
				confetiiGo.SetActive(true);
				gameObject.SetActive(false);
				SoundEffect soundEffect = AudioManager.instance.MakeSoundEffect(pickUpSound, Vector3.zero, pickUpSoundVolume);
				soundEffect.audioSource.spatialBlend = 0;
			}
		}
	}
}
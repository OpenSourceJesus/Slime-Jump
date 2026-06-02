﻿using Extensions;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace SlimeJump
{
	public class AudioManager : SingletonMonoBehaviour<AudioManager>
	{
		public float Volume
		{
			get
			{
				return PlayerPrefs.GetFloat("Volume", 1);
			}
			set
			{
				AudioListener.volume = value;
				PlayerPrefs.SetFloat("Volume", value);
			}
		}
		public bool Mute
		{
			get
			{
				return PlayerPrefsExtensions.GetBool("Mute");
			}
			set
			{
				AudioListener.pause = value;
				PlayerPrefsExtensions.SetBool("Mute", value);
			}
		}
		public SoundEffect soundEffectPrefab;

		public void MakeSoundEffect (AudioClip audioClip)
		{
			MakeSoundEffect (audioClip, Vector3.zero);
		}
		
		public SoundEffect MakeSoundEffect (AudioClip audioClip, Vector3 position)
		{
			return MakeSoundEffect(audioClip, position, soundEffectPrefab.settings.Volume);
		}
		
		public SoundEffect MakeSoundEffect (AudioClip audioClip, Vector3 position, float volume)
		{
			SoundEffect.Settings soundEffectSettings = new SoundEffect.Settings(soundEffectPrefab.settings);
			soundEffectSettings.audioClip = audioClip;
			soundEffectSettings.Position = position;
			soundEffectSettings.Volume = volume;
			return MakeSoundEffect(soundEffectSettings);
		}
		
		public SoundEffect MakeSoundEffect (SoundEffect.Settings soundEffectSettings, Vector3 position)
		{
			SoundEffect.Settings _soundEffectSettings = new SoundEffect.Settings(soundEffectSettings);
			_soundEffectSettings.Position = position;
			return MakeSoundEffect(_soundEffectSettings);
		}
		
		public SoundEffect MakeSoundEffect (SoundEffect.Settings soundEffectSettings)
		{
			SoundEffect output = ObjectPool.instance.SpawnComponent<SoundEffect>(soundEffectPrefab.prefabIndex, soundEffectSettings.Position.SetZ(GameCamera.instance.trs.position.z), soundEffectSettings.Rotation);
			output.settings = soundEffectSettings;
			output.Play ();
			return output;
		}
	}
}
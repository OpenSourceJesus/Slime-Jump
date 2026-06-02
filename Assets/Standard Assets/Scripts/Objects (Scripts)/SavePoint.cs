using Extensions;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace SlimeJump
{
	public class SavePoint : MonoBehaviour
	{
		public SpriteRenderer spriteRenderer;
		public SpriteRenderer worldMapSpriteRenderer;
		public Sprite touchedSprite;
		public WorldMapIcon worldMapIcon;
		[HideInInspector]
		public Vector2 initPosition;
		public AudioClip touchSound;
		public float touchSoundVolume;
		public bool Touched
		{
			get 
			{
				return SaveAndLoadManager.GetBool("Touched " + _SceneManager.CurrentScene.name + ' ' + name, false);
			}
			set
			{
				SaveAndLoadManager.SetBool ("Touched " + _SceneManager.CurrentScene.name + ' ' + name, value);
			}
		}
		public static SavePoint[] instances = new SavePoint[0];

		void Start ()
		{
			if (Touched)
			{
				spriteRenderer.sprite = touchedSprite;
				worldMapSpriteRenderer.sprite = touchedSprite;
			}
		}

#if UNITY_EDITOR
		void OnValidate ()
		{
			initPosition = transform.position;
		}
#endif

		void OnTriggerEnter2D (Collider2D other)
		{
			SoundEffect soundEffect = AudioManager.instance.MakeSoundEffect(touchSound, Vector3.zero, touchSoundVolume);
			soundEffect.audioSource.spatialBlend = 0;
			GameManager.instance.saveNotificationTempActiveText.Do ();
			Player.instance.SavedPosition = initPosition;
			Gem.instances = FindObjectsOfType<Gem>(true);
			for (int i = 0; i < Gem.instances.Length; i ++)
			{
				Gem gem = Gem.instances[i];
				if (!gem.gameObject.activeSelf)
				{
					for (int i2 = 0; i2 < gem.spriteRenderers.Length; i2 ++)
					{
						SpriteRenderer spriteRenderer = gem.spriteRenderers[i2];
						spriteRenderer.color = spriteRenderer.color.SetAlpha(0.25f);
					}
					gem.gameObject.SetActive(true);
					if (!gem.Collected)
					{
						gem.Collected = true;
						Player.Gems ++;
						CosmeticsMenu.AddPoints (CosmeticsMenu.Instance.pointsPerGem);
						GemsAchievement[] gemsAchievements = FindObjectsOfType<GemsAchievement>();
						for (int i2 = 0; i2 < gemsAchievements.Length; i2 ++)
						{
							GemsAchievement gemsAchievement = gemsAchievements[i2];
							gemsAchievement.HandleAchieve ();
						}
					}
				}
			}
#if !UNITY_WEBGL
			WorldMap.savePointPlayerIsAt = this;
			if (WorldMap.foundNewCellSinceSaved)
			{
				bool[] exploredCellPositions = new bool[WorldMap.exploredCellPositions.Length];
				WorldMap.exploredCellPositions.CopyTo(exploredCellPositions, 0);
				WorldMap.ExploredCellPositions = exploredCellPositions;
			}
			if (!Touched)
				CosmeticsMenu.AddPoints (CosmeticsMenu.Instance.pointsPerSavePoint);
			Touched = true;
			spriteRenderer.sprite = touchedSprite;
			worldMapSpriteRenderer.sprite = touchedSprite;
			SaveAndLoadManager.Save ();
			WorldMap.foundNewCellSinceSaved = false;
#endif
			for (int i2 = 0; i2 < TouchSavePointAchievement.instances.Length; i2 ++)
			{
				TouchSavePointAchievement touchSavePointAchievement = TouchSavePointAchievement.instances[i2];
				touchSavePointAchievement.HandleAchieve ();
			}
			for (int i2 = 0; i2 < SpeedAchievement.instances.Length; i2 ++)
			{
				SpeedAchievement speedAchievement = SpeedAchievement.instances[i2];
				if (name == speedAchievement.savePointsNames[0])
				{
					SpeedAchievement.startTime = Time.time;
					SpeedAchievement.current = speedAchievement;
					SpeedAchievement.savePointsNamesTouchedWithoutFastTraveling.Clear();
					GameManager.instance.speedrunTimerText.gameObject.SetActive(true);
				}
				else if (name == speedAchievement.savePointsNames[speedAchievement.savePointsNames.Length - 1])
				{
					speedAchievement.HandleAchieve ();
					SpeedAchievement.current = null;
					GameManager.instance.speedrunTimerText.gameObject.SetActive(false);
				}
			}
			string myIdInAchievements = _SceneManager.CurrentScene.name + ' ' + name;
			if (!SpeedAchievement.savePointsNamesTouchedWithoutFastTraveling.Contains(myIdInAchievements))
				SpeedAchievement.savePointsNamesTouchedWithoutFastTraveling.Add(myIdInAchievements);
			if (!OneLifeAchievement.savePointsNamesTouchedInOneLife.Contains(myIdInAchievements))
				OneLifeAchievement.savePointsNamesTouchedInOneLife.Add(myIdInAchievements);
			for (int i2 = 0; i2 < OneLifeAchievement.instances.Length; i2 ++)
			{
				OneLifeAchievement oneLifeAchievement = OneLifeAchievement.instances[i2];
				oneLifeAchievement.HandleAchieve ();
			}
			for (int i2 = 0; i2 < SpeedAchievement.instances.Length; i2 ++)
			{
				SpeedAchievement speedAchievement = SpeedAchievement.instances[i2];
				speedAchievement.HandleAchieve ();
			}
		}

		void OnTriggerExit2D (Collider2D other)
		{
			if (!WorldMap.isOpen)
				WorldMap.savePointPlayerIsAt = null;
		}
	}
}
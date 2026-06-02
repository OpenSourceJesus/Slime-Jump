using TMPro;
using System;
using Extensions;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Random = UnityEngine.Random;

namespace SlimeJump
{
	public class CosmeticsMenu : SingletonMonoBehaviour<CosmeticsMenu>
	{
		public byte pointsPerGem;
		public byte pointsPerSavePoint;
		public TMP_Text cosmeticNameText;
		public Toggle[] toggles = new Toggle[0];
		public static List<Cosmetic> equipped = new List<Cosmetic>();
		public static int Points
		{
			get
			{
				return SaveAndLoadManager.GetInt("Points", 0);
			}
			set
			{
				SaveAndLoadManager.SetInt ("Points", value);
			}
		}

		public override void Awake ()
		{
			base.Awake ();
			if (transform.parent != null)
				transform.parent.gameObject.SetActive(false);
			gameObject.SetActive(false);
			Cosmetic.instances = FindObjectsOfType<Cosmetic>(true);
			Cosmetic.instances = Cosmetic.instances._Sort(new CosmeticComparer());
			for (int i = 0; i < toggles.Length; i ++)
			{
				Toggle toggle = toggles[i];
				Cosmetic cosmetic = Cosmetic.instances[i];
				toggle.interactable = cosmetic.Unlocked || Debug.isDebugBuild;
				toggle.isOn = cosmetic.Equipped;
			}
		}

		public static void AddPoints (byte amount)
		{
			Points += amount;
			while (true)
			{
				List<Cosmetic> canUnlock = new List<Cosmetic>();
				for (int i = 0; i < Cosmetic.instances.Length; i ++)
				{
					Cosmetic cosmetic = Cosmetic.instances[i];
					if (Points >= cosmetic.pointsRequired)
						canUnlock.Add(cosmetic);
				}
				if (canUnlock.Count == 0)
					return;
				Cosmetic unlock = canUnlock[Random.Range(0, canUnlock.Count)];
				unlock.Unlocked = true;
				Points -= unlock.pointsRequired;
				if (GameManager.instance.achieveNotificationGo.activeSelf)
					EventManager.AddEvent (unlock.ShowUnlockNotification, Time.time + GameManager.instance.achieveNotificationDur);
				else if (GameManager.instance.cosmeticNotificationGo.activeSelf)
					EventManager.AddEvent (unlock.ShowUnlockNotification, Time.time + GameManager.instance.cosmeticNotificationDur);
				else
					unlock.ShowUnlockNotification ();
			}
		}

		public static void Apply ()
		{
			for (int i = 0; i < Cosmetic.instances.Length; i ++)
			{
				Cosmetic cosmetic = Cosmetic.instances[i];
				cosmetic.Equipped = cosmetic.Preview;
			}
			SaveAndLoadManager.Save ();
		}

		public void LockAll ()
		{
			for (int i = 0; i < toggles.Length; i ++)
			{
				Toggle toggle = toggles[i];
				toggle.interactable = false;
				toggle.isOn = false;
			}
		}

		public void OnMouseOverCosmetic (Cosmetic cosmetic)
		{
			cosmeticNameText.text = cosmetic.name;
		}

		class CosmeticComparer : IComparer<Cosmetic>
		{
			public int Compare (Cosmetic cosmetic, Cosmetic cosmetic2)
			{
				return cosmetic.CompareTo(cosmetic2);
			}
		}
	}
}
using UnityEngine;

namespace SlimeJump
{
	public class Achievement : SingletonMonoBehaviour<Achievement>
	{
		public string displayName;
		public string description;
		public Sprite icon;
		public bool Achieved
		{
			get
			{
				return SaveAndLoadManager.GetBool("Achieved " + name, false);
			}
			set
			{
				SaveAndLoadManager.SetBool ("Achieved " + name, value);
			}
		}
		bool previousAchieved;

		public override void Awake ()
		{
			base.Awake ();
			previousAchieved = Achieved;
		}

		void OnAchieve ()
		{
			SaveAndLoadManager.Save ();
			if (GameManager.instance.achieveNotificationGo.activeSelf)
				EventManager.AddEvent (ShowAchieveNotification, Time.time + GameManager.instance.achieveNotificationDur);
			else if (GameManager.instance.cosmeticNotificationGo.activeSelf)
				EventManager.AddEvent (ShowAchieveNotification, Time.time + GameManager.instance.cosmeticNotificationDur);
			else
				ShowAchieveNotification ();
		}

		void ShowAchieveNotification ()
		{
			GameManager.instance.achieveNotificationText.text = "Achievement unlocked!\n" + description;
			GameManager.instance.achieveNotificationImage.sprite = icon;
			GameManager.instance.achieveNotificationGo.SetActive(true);
		}

		public virtual bool HandleAchieve ()
		{
			if (Achieved && !previousAchieved)
			{
				OnAchieve ();
				previousAchieved = true;
			}
			return Achieved;
		}
	}
}
using UnityEngine;

namespace SlimeJump
{
	public class TimeAchievement : Achievement
	{
		public new static TimeAchievement instance;
		public new static TimeAchievement Instance
		{
			get
			{
				if (instance == null)
					instance = FindObjectOfType<TimeAchievement>(true);
				return instance;
			}
		}
		public uint duration;
		public static float TimePlayed
		{
			get
			{
				return SaveAndLoadManager.GetFloat("Time played", 0);
			}
			set
			{
				SaveAndLoadManager.SetFloat ("Time played", value);
			}
		}

		public override bool HandleAchieve ()
		{
			Achieved = TimePlayed >= duration;
			return base.HandleAchieve();
		}
	}
}
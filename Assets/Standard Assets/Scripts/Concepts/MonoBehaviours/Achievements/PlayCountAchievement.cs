using UnityEngine;

namespace SlimeJump
{
	public class PlayCountAchievement : Achievement
	{
		public new static PlayCountAchievement instance;
		public new static PlayCountAchievement Instance
		{
			get
			{
				if (instance == null)
					instance = FindObjectOfType<PlayCountAchievement>(true);
				return instance;
			}
		}
		public uint count;
		public static int PlayCount
		{
			get
			{
				return SaveAndLoadManager.GetInt("Play count", 0);
			}
			set
			{
				SaveAndLoadManager.SetInt ("Play count", value);
			}
		}

		public override bool HandleAchieve ()
		{
			Achieved = PlayCount >= count;
			return base.HandleAchieve();
		}
	}
}
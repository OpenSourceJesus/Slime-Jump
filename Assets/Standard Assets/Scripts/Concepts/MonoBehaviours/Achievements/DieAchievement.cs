using UnityEngine;

namespace SlimeJump
{
	public class DieAchievement : Achievement
	{
		public new static DieAchievement instance;
		public new static DieAchievement Instance
		{
			get
			{
				if (instance == null)
					instance = FindObjectOfType<DieAchievement>(true);
				return instance;
			}
		}
		public uint count;
		public static int DieCount
		{
			get
			{
				return SaveAndLoadManager.GetInt("Die count", 0);
			}
			set
			{
				SaveAndLoadManager.SetInt ("Die count", value);
			}
		}

		public override bool HandleAchieve ()
		{
			Achieved = DieCount >= count;
			return base.HandleAchieve();
		}
	}
}
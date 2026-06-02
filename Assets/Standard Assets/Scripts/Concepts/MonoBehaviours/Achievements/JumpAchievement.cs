using UnityEngine;

namespace SlimeJump
{
	public class JumpAchievement : Achievement
	{
		public new static JumpAchievement instance;
		public new static JumpAchievement Instance
		{
			get
			{
				if (instance == null)
					instance = FindObjectOfType<JumpAchievement>(true);
				return instance;
			}
		}
		public uint count;
		public static int JumpCount
		{
			get
			{
				return SaveAndLoadManager.GetInt("Jump count", 0);
			}
			set
			{
				SaveAndLoadManager.SetInt ("Jump count", value);
			}
		}

		public override bool HandleAchieve ()
		{
			Achieved = JumpCount >= count;
			return base.HandleAchieve();
		}
	}
}
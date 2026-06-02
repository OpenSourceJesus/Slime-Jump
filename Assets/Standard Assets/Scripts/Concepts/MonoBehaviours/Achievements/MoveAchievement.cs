using UnityEngine;

namespace SlimeJump
{
	public class MoveAchievement : Achievement
	{
		public new static MoveAchievement instance;
		public new static MoveAchievement Instance
		{
			get
			{
				if (instance == null)
					instance = FindObjectOfType<MoveAchievement>(true);
				return instance;
			}
		}
		public uint units;
		public static float MovedDistance
		{
			get
			{
				return SaveAndLoadManager.GetFloat("Moved distance", 0);
			}
			set
			{
				SaveAndLoadManager.SetFloat ("Moved distance", value);
			}
		}

		public override bool HandleAchieve ()
		{
			Achieved = MovedDistance >= units;
			return base.HandleAchieve();
		}
	}
}
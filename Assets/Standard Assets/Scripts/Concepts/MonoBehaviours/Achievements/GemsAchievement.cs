using UnityEngine;

namespace SlimeJump
{
	public class GemsAchievement : Achievement
	{
		public string sceneName;
		public byte count;

		public override bool HandleAchieve ()
		{
			Achieved = SaveAndLoadManager.GetInt("Gems " + sceneName, 0) >= count;
			return base.HandleAchieve();
		}
	}
}
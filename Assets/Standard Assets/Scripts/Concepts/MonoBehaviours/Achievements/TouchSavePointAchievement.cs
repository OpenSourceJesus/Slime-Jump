using UnityEngine;

namespace SlimeJump
{
	public class TouchSavePointAchievement : Achievement
	{
		public string sceneName;
		public string savePointName;
		public static TouchSavePointAchievement[] instances = new TouchSavePointAchievement[0];

		public override bool HandleAchieve ()
		{
			for (int i = 0; i < SavePoint.instances.Length; i ++)
			{
				SavePoint savePoint = SavePoint.instances[i];
				if (savePoint.name == savePointName)
				{
					Achieved = SaveAndLoadManager.GetBool("Touched " + sceneName + ' ' + savePointName, false);
					break;
				}
			}
			return base.HandleAchieve();
		}
	}
}
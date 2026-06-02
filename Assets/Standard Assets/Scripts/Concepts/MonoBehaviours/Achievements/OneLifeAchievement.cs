using UnityEngine;
using System.Collections.Generic;

namespace SlimeJump
{
	public class OneLifeAchievement : Achievement
	{
		public string sceneName;
		public string[] savePointsNames = new string[0];
		public byte maxOpenMainMenuCnt;
		public static List<string> savePointsNamesTouchedInOneLife = new List<string>();
		public static OneLifeAchievement[] instances = new OneLifeAchievement[0];
		public static uint openedMainMenuCnt;

		public override bool HandleAchieve ()
		{
			if (openedMainMenuCnt > maxOpenMainMenuCnt)
				return false;
			for (int i = 0; i < savePointsNames.Length; i ++)
			{
				string savePointName = savePointsNames[i];
				if (!savePointsNamesTouchedInOneLife.Contains(savePointName + ' ' + sceneName))
					return false;
			}
			Achieved = true;
			return base.HandleAchieve();
		}
	}
}
using UnityEngine;
using System.Collections.Generic;

namespace SlimeJump
{
	public class SpeedAchievement : Achievement
	{
		public string sceneName;
		public uint duration;
		public string[] savePointsNames = new string[0];
		public static List<string> savePointsNamesTouchedWithoutFastTraveling = new List<string>();
		public static SpeedAchievement[] instances = new SpeedAchievement[0];
		public static float startTime;
		public static SpeedAchievement current;
		const string REPLACE_INDICATOR = "|";

		public override void Awake ()
		{
			base.Awake ();
			description = description.Replace(REPLACE_INDICATOR, "" + duration);
		}

		public override bool HandleAchieve ()
		{
			if (Time.time - startTime > duration)
				return false;
			for (int i = 0; i < savePointsNames.Length; i ++)
			{
				string savePointName = savePointsNames[i];
				if (!savePointsNamesTouchedWithoutFastTraveling.Contains(savePointName + ' ' + sceneName))
					return false;
			}
			Achieved = true;
			return base.HandleAchieve();
		}
	}
}
using UnityEngine;
using System.Collections.Generic;

namespace SlimeJump
{
	public class SpeedAchievement : Achievement
	{
		public string sceneName;
		public uint duration;
		public string[] savePointsNames = new string[0];
		public float TimeLeft
		{
			get
			{
				return SaveAndLoadManager.GetFloat(sceneName + " " + name + " time left");
			}
			set
			{
				SaveAndLoadManager.SetFloat (sceneName + " " + name + " time left", value);
			}
		}
		public int SavePointsTouchedCntWithoutFastTraveling
		{
			get
			{
				return SaveAndLoadManager.GetInt(sceneName + " " + name + " save points touched count without fast traveling");
			}
			set
			{
				SaveAndLoadManager.SetInt (sceneName + " " + name + " save points touched count without fast traveling", value);
			}
		}
		public static SpeedAchievement[] instances = new SpeedAchievement[0];
		public static SpeedAchievement current;
		const string REPLACE_INDICATOR = "|";

		public override void Awake ()
		{
			base.Awake ();
			description = description.Replace(REPLACE_INDICATOR, "" + duration);
		}

		public override bool HandleAchieve ()
		{
			if (TimeLeft <= 0 || SavePointsTouchedCntWithoutFastTraveling < savePointsNames.Length)
				return false;
			Achieved = true;
			return base.HandleAchieve();
		}
	}
}
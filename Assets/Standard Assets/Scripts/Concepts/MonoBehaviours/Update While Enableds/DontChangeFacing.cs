using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace SlimeJump
{
	public class DontChangeFacing : UpdateWhileEnabled
	{
		public Transform trs;
		Vector3 initEulerAngles;

		void Awake ()
		{
			initEulerAngles = trs.eulerAngles;
		}

		public override void DoUpdate ()
		{
			trs.eulerAngles = initEulerAngles;
		}
	}
}
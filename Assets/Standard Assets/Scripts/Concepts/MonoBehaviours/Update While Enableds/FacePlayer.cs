using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace SlimeJump
{
	public class FacePlayer : UpdateWhileEnabled
	{
		public Transform trs;

		public override void DoUpdate ()
		{
			trs.up = Player.instance.trs.position - trs.position;
		}
	}
}
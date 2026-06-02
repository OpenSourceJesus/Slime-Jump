using Extensions;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace SlimeJump
{
	public class GameCamera : CameraScript
	{
		public new static GameCamera instance;
		public new static GameCamera Instance
		{
			get
			{
				if (instance == null)
					instance = FindObjectOfType<GameCamera>(true);
				return instance;
			}
		}
		public bool followPlayer;
		
		public override void Awake ()
		{
			instance = this;
			base.Awake ();
		}

		public override void HandlePosition ()
		{
			if (followPlayer)
            	trs.position = Player.instance.trs.position.SetZ(trs.position.z);
            base.HandlePosition ();
		}
	}
}
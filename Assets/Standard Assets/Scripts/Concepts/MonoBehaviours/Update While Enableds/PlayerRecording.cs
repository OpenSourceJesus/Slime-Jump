using UnityEngine;
using System.Collections.Generic;

namespace SlimeJump
{
	public struct PlayerRecording
	{
		public List<Frame> frames;

		public struct Frame
		{
			public Vector3 position;
			public bool facingLeft;
			public float time;

			public Frame (float time)
			{
				position = Player.instance.trs.position;
				facingLeft = Player.instance.trs.localScale.x < 0;
				this.time = time;
			}
		}
	}
}
using UnityEngine;
using System.Collections.Generic;

namespace SlimeJump
{
	public class PlayerMimic : Player
	{
		public float startTime;
		public float pausedTime;
		public int currentFrameIndex;
		public PlayerRecording playing;
		public PlayerRecording.Frame currentFrame;
		public PlayerRecording.Frame nextFrame;
		public static List<PlayerMimic> instances = new List<PlayerMimic>();

		public override void Awake ()
		{
			instances.Add(this);
			whatICollideWith = Physics2D.GetLayerCollisionMask(gameObject.layer);
		}

		void OnDestroy ()
		{
			instances.Remove(this);
		}

		public override void OnCollisionEnter2D (Collision2D coll)
		{
		}

		public override void OnCollisionExit2D (Collision2D coll)
		{
		}
	}
}
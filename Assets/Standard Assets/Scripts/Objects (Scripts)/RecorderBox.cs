using System;
using Extensions;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace SlimeJump
{
	public class RecorderBox : UpdateWhileEnabled
	{
		public SpriteRenderer spriteRenderer;
		public PlayerRecording currentRecording;
		[HideInInspector]
		public bool hasBeenUsed;
		public static RecorderBox[] instances = new RecorderBox[0];
		public static RecorderBox[] areRecording = new RecorderBox[0];
		float timeStarted;

		public override void OnEnable ()
		{
		}

		void Awake ()
		{
			areRecording = new RecorderBox[0];
		}
		
		void OnCollisionEnter2D (Collision2D coll)
		{
			if (hasBeenUsed)
				return;
			hasBeenUsed = true;
			spriteRenderer.color = spriteRenderer.color.Divide(2);
			StartRecording ();
		}

		void StartRecording ()
		{
			areRecording = areRecording.Add(this);
			timeStarted = GameManager.TimeSinceLevelLoad;
			currentRecording = new PlayerRecording();
			currentRecording.frames = new List<PlayerRecording.Frame>(new PlayerRecording.Frame[] { new PlayerRecording.Frame(0) });
			GameManager.updatables = GameManager.updatables.Add(this);
		}

		public override void DoUpdate ()
		{
			currentRecording.frames.Add(new PlayerRecording.Frame(GameManager.TimeSinceLevelLoad - timeStarted));
		}

		public void StopRecording ()
		{
			areRecording = areRecording.Remove(this);
			GameManager.updatables = GameManager.updatables.Remove(this);
		}
	}
}
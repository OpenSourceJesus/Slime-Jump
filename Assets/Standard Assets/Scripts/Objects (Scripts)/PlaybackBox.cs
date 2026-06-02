using Extensions;
using UnityEngine;
using System.Collections.Generic;

namespace SlimeJump
{
	public class PlaybackBox : UpdateWhileEnabled
	{
		public SpriteRenderer spriteRenderer;
		public PlayerMimic playerMimicPrefab;
		[HideInInspector]
		public bool hasBeenUsed;
		[HideInInspector]
		public List<PlayerMimic> playerMimics = new List<PlayerMimic>();
		public static PlaybackBox[] instances = new PlaybackBox[0];
		public static PlayerRecording[] recordings = new PlayerRecording[0];

		public override void OnEnable ()
		{
		}

		public override void OnDisable ()
		{
		}

		void Awake ()
		{
			recordings = new PlayerRecording[0];
		}
		
		void OnCollisionEnter2D (Collision2D coll)
		{
			if (hasBeenUsed)
			{
				if (coll.collider == Player.instance.collider)
					return;
				for (int i = 0; i < playerMimics.Count; i ++)
				{
					PlayerMimic playerMimic = playerMimics[i];
					DestroyPlayerMimic (playerMimic);
					i --;
				}
				return;
			}
			hasBeenUsed = true;
            spriteRenderer.color = spriteRenderer.color.Divide(2);
			for (int i = 0; i < RecorderBox.areRecording.Length; i ++)
			{
				RecorderBox recorder = RecorderBox.areRecording[i];
				recorder.StopRecording ();
				recordings = recordings.Add(recorder.currentRecording);
			}
			for (int i = 0; i < recordings.Length; i ++)
			{
				PlayerRecording recording = recordings[i];
				PlayerMimic playerMimic = Instantiate(playerMimicPrefab, recording.frames[0].position, Quaternion.identity);
				playerMimic.playing = recording;
				playerMimic.startTime = GameManager.TimeSinceLevelLoad;
				playerMimic.currentFrame = playerMimic.playing.frames[0];
				playerMimic.nextFrame = playerMimic.playing.frames[1];
				playerMimics.Add(playerMimic);
				GameManager.updatables = GameManager.updatables.Add(this);
			}
		}

		public override void DoUpdate ()
		{
			for (int i = 0; i < playerMimics.Count; i ++)
			{
				PlayerMimic playerMimic = playerMimics[i];
				ContactFilter2D contactFilter = new ContactFilter2D();
				contactFilter.useLayerMask = true;
				contactFilter.layerMask = LayerMask.GetMask("Player");
				contactFilter.useTriggers = false;
				while (GameManager.TimeSinceLevelLoad - playerMimic.startTime - playerMimic.pausedTime >= playerMimic.nextFrame.time)
				{
					List<RaycastHit2D> hits = new List<RaycastHit2D>();
					Vector2 move = playerMimic.nextFrame.position - playerMimic.trs.position;
					if (playerMimic.collider.Cast(move, contactFilter, hits, move.magnitude) == 0)
					{
						playerMimic.animator.speed = 1;
						playerMimic.trs.position = playerMimic.nextFrame.position;
						playerMimic.currentFrame = playerMimic.nextFrame;
						playerMimic.trs.localScale = playerMimic.trs.localScale.SetX(playerMimic.currentFrame.facingLeft.PositiveOrNegative());
						playerMimic.currentFrameIndex ++;
						if (playerMimic.currentFrameIndex == playerMimic.playing.frames.Count)
						{
							DestroyPlayerMimic (playerMimic);
							i --;
							break;
						}
						playerMimic.nextFrame = playerMimic.playing.frames[playerMimic.currentFrameIndex];
					}
					else
					{
						// playerMimic.trs.position = hits[0].centroid;
						playerMimic.animator.speed = 0;
						playerMimic.pausedTime += Time.deltaTime;
						break;
					}
				}
			}
		}

		void DestroyPlayerMimic (PlayerMimic playerMimic)
		{
			Destroy(playerMimic.gameObject);
			playerMimics.Remove(playerMimic);
			if (playerMimics.Count == 0)
				GameManager.updatables = GameManager.updatables.Remove(this);
		}
	}
}
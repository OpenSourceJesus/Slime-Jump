using UnityEngine;

namespace SlimeJump
{
	public class PrintAverageFPS : SingletonUpdateWhileEnabled<PrintAverageFPS>
	{
		public uint frames;
		float timeSinceLevelLoaded;

		public override void OnEnable ()
		{
			base.OnEnable ();
			timeSinceLevelLoaded = 0;
		}

		public override void DoUpdate ()
		{
			if (GameManager.framesSinceLevelLoaded > GameManager.LAGGY_FRAMES_ON_LOAD_SCENE)
			{
				timeSinceLevelLoaded += Time.deltaTime;
				if (GameManager.framesSinceLevelLoaded == frames)
				{
					print(1f / (timeSinceLevelLoaded / frames));
					enabled = false;
				}
			}
		}
	}
}
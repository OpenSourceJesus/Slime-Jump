using Extensions;
using UnityEngine;
using System.Collections.Generic;

namespace SlimeJump
{
	public class Hourglass : UseableItem, IUpdatable
	{
		public float rewindTime;
		public Transform rewindPoitionIndicatorTrs;
		public static List<Snapshot> snapshots = new List<Snapshot>();

        public override void OnGain (Player player)
        {
			base.OnGain (player);
			GameManager.updatables = GameManager.updatables.Add(this);
        }

		public void DoUpdate ()
		{
			snapshots.Add(new Snapshot());
			while (GameManager.TimeSinceLevelLoad - snapshots[0].time > rewindTime)
				snapshots.RemoveAt(0);
			rewindPoitionIndicatorTrs.position = snapshots[0].position;
		}

        public override void OnDisable ()
		{
			GameManager.updatables = GameManager.updatables.Remove(this);
		}

		public override void Use ()
		{
			base.Use ();
			Player.instance.trs.position = snapshots[0].position;
		}

		public class Snapshot
		{
			public Vector2 position;
			public float time;

			public Snapshot ()
			{
				position = Player.instance.trs.position;
				time = GameManager.TimeSinceLevelLoad;
			}
		}
	}
}
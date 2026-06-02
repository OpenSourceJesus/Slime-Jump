using Extensions;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace SlimeJump
{
	public class CloudSpawner : SingletonUpdateWhileEnabled<CloudSpawner>
	{
		public BoxCollider2D spawnZoneBoxCollider; 
		public Cloud cloudPrefab;
		public FloatRange spawnIntervalRange;
		public Transform cloudsParent;
		public float cloudsParallax;
		public float prewarmDur;
		float spawnInterval;
		float spawnTimer;
		const float PREWARM_SAMPLE_INTVAL = .5f;

		public override void OnEnable ()
		{
			base.OnEnable ();
			Init ();
		}

		public void Init ()
		{
			for (int i = 0; i < Cloud.instances.Count; i ++)
			{
				Cloud cloud = Cloud.instances[i];
				Destroy(cloud.gameObject);
				i --;
			}
			cloudsParent.position = Player.instance.trs.position;
			List<Cloud> clouds = new List<Cloud>();
			spawnInterval = spawnIntervalRange.Get(Random.value);
			for (float time = 0; time <= prewarmDur; time += PREWARM_SAMPLE_INTVAL)
			{
				for (int i = 0; i < clouds.Count; i ++)
				{
					Cloud cloud = clouds[i];
					cloud.OnEnable ();
					cloud.trs.position += (Vector3) cloud.rigid.linearVelocity * PREWARM_SAMPLE_INTVAL;
				}
				spawnTimer += PREWARM_SAMPLE_INTVAL;
				if (spawnTimer >= spawnInterval)
					clouds.Add(Spawn());
			}
		}

		public override void DoUpdate ()
		{
			if (GameManager.paused)
				return;
			spawnTimer += Time.deltaTime;
			if (spawnTimer >= spawnInterval)
				Spawn ();
			if (Player.instance.respawnTimer <= 0)
				cloudsParent.position += (Vector3) Player.instance.lastMovement * cloudsParallax;
		}

		Cloud Spawn ()
		{
			spawnTimer -= spawnInterval;
			spawnInterval = spawnIntervalRange.Get(Random.value);
			Bounds spawnZoneBounds = spawnZoneBoxCollider.bounds;
			return Instantiate(cloudPrefab, new Vector2(spawnZoneBounds.min.x, Random.Range(spawnZoneBounds.min.y, spawnZoneBounds.max.y)), Quaternion.identity, cloudsParent);
		}
	}
}
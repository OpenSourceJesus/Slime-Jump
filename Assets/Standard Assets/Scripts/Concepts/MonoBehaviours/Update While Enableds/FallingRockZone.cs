using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace SlimeJump
{
	public class FallingRockZone : UpdateWhileEnabled
	{
		public BoxCollider2D spawnZoneBoxCollider; 
		public Transform rockPrefab;
		public float spawnIntervalPerWidthUnit;
		float spawnInterval;
		float spawnTimer;

		public override void OnEnable ()
		{
			base.OnEnable ();
			spawnInterval = spawnIntervalPerWidthUnit / spawnZoneBoxCollider.bounds.size.x;
		}

		public override void DoUpdate ()
		{
			spawnTimer += Time.deltaTime;
			if (spawnTimer >= spawnInterval)
			{
				spawnTimer -= spawnInterval;
				Bounds spawnZoneBounds = spawnZoneBoxCollider.bounds;
				Instantiate(rockPrefab, new Vector2(Random.Range(spawnZoneBounds.min.x, spawnZoneBounds.max.x), spawnZoneBounds.max.y), Quaternion.identity);
			}
		}
	}
}
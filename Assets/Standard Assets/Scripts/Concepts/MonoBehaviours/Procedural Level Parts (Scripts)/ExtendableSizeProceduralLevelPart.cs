using Extensions;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace SlimeJump
{
	public class ExtendableSizeProceduralLevelPart : ProceduralLevelPart
	{
		public Collider2D[] lastAddedColliders = new Collider2D[0];
		public Path[] lastAddedPaths = new Path[0];
		public Vector3 extendDirection;
		public bool isLocalDirection;
		[HideInInspector]
		public int timesExtended;

		public override void Start ()
		{
			base.Start ();
#if UNITY_EDITOR
			if (!Application.isPlaying)
			{
				lastAddedColliders = colliders.ToArray();
				for (int i = 0; i < lastAddedColliders.Length; i ++)
				{
					Collider2D lastAddedTile = lastAddedColliders[i];
					int indexOfSuffix = lastAddedTile.name.IndexOf(" (");
					if (indexOfSuffix != -1)
						lastAddedTile.name = lastAddedTile.name.Remove(indexOfSuffix);
				}
				lastAddedPaths = paths.ToArray();
				return;
			}
#endif
		}

		public Vector3 GetExtendDirection (Vector3? extendDirection = null, bool? isLocalDirection = null)
		{
			if (extendDirection == null)
				extendDirection = this.extendDirection;
			if (isLocalDirection == null)
				isLocalDirection = this.isLocalDirection;
			if ((bool) isLocalDirection)
				return trs.rotation * (Vector3) extendDirection;
			else
				return (Vector3) extendDirection;
		}

		public bool CanExtendInDirection (Vector2? extendDirection = null, bool? isLocalDirection = null)
		{
			extendDirection = GetExtendDirection(extendDirection, isLocalDirection);
			for (int i2 = 0; i2 < level.levelParts.Count; i2 ++)
			{
				ProceduralLevelPart levelPart = level.levelParts[i2];
				if (AreOverlapping(this, levelPart, new _RectOffset(Vector2.one / 2 + (Vector2) extendDirection, (Vector2) extendDirection)))
					return false;
			}
			return true;
		}

		public void ExtendInDirection (Vector2? extendDirection = null, bool? isLocalDirection = null)
		{
			extendDirection = GetExtendDirection(extendDirection, isLocalDirection);
			Dictionary<Vector2, Transform> tileTrsPositionsDict = new Dictionary<Vector2, Transform>();
			for (int i = 0; i < colliders.Count; i ++)
			{
				Collider2D collider = colliders[i];
				if (collider == null)
				{
					colliders.RemoveAt(i);
					i --;
					continue;
				}
				// Vector2[] pointsInsideTile = collider.bounds.ToRect().GetPointsInside(Vector2.one, new _RectOffset(Vector2.one / 2, Vector2.zero));
				// for (int i2 = 0; i2 < pointsInsideTile.Length; i2 ++)
				// {
				// 	Vector2 pointInsideTile = pointsInsideTile[i2];
				// 	tileTrsPositionsDict.Add(pointInsideTile, collider.transform);
				// }
			}
			List<Collider2D> addedColliders = new List<Collider2D>();
			List<Collider2D> _lastAddedColliders = new List<Collider2D>(lastAddedColliders);
			// for (int i = 0; i < _lastAddedColliders.Count; i ++)
			// {
			// 	Collider2D collider = _lastAddedColliders[i];
			// 	Vector3[] pointsInsideTile = collider.meshRenderer.bounds.GetPointsInside(Vector2.one, new _RectOffset(Vector2.one / 2, Vector2.zero));
			// 	for (int i2 = 0; i2 < pointsInsideTile.Length; i2 ++)
			// 	{
			// 		Vector3 point = pointsInsideTile[i2] + (Vector3) extendDirection;
			// 		if (!tileTrsPositionsDict.ContainsKey(point))
			// 		{
			// 			ProceduralLevel.SpawnEntry spawnEntry = level.spawnEntriesDict[collider.name];
			// 			Transform newTrs = spawnEntry.Spawn(point);
			// 			Collider2D newTile = newTrs.GetComponent<Collider2D>();
			// 			tileTrsPositionsDict.Add(point, newTrs);
			// 			addedColliders.Add(newTile);
			// 			if (level.TryToMergeTransformWithNeighbors(newTrs, ref tileTrsPositionsDict, ref level.colliders))
			// 			{
			// 				int indexOfNull;
			// 				do
			// 				{
			// 					indexOfNull = _lastAddedColliders.IndexOf(null);
			// 					if (indexOfNull != -1)
			// 					{
			// 						if (indexOfNull <= i)
			// 							i --;
			// 						_lastAddedColliders.RemoveAt(indexOfNull);
			// 						colliders.Remove(null);
			// 						addedColliders.Remove(null);
			// 					}
			// 				} while (indexOfNull != -1);
			// 			}
			// 		}
			// 	}
			// }
			colliders.AddRange(addedColliders);
			level.colliders.AddRange(addedColliders);
			lastAddedColliders = addedColliders.ToArray();
			timesExtended ++;
		}
	}
}
using System;
using Extensions;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Random = UnityEngine.Random;
using Object = UnityEngine.Object;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace SlimeJump
{
	public class ProceduralLevel : SingletonMonoBehaviour<ProceduralLevel>
	{
		public Transform trs;
		public Vector2Int size;
		public List<Collider2D> colliders = new List<Collider2D>();
		public List<ProceduralLevelPart> levelParts = new List<ProceduralLevelPart>();
		[Header("Spawn Entries")]
		public SpawnEntry[] spawnEntries = new SpawnEntry[0];
		public LevelPartSpawnEntry[] levelPartSpawnEntries = new LevelPartSpawnEntry[0];
		public ExtendableSizeLevelPartSpawnEntry[] extendableSizeLevelPartSpawnEntries = new ExtendableSizeLevelPartSpawnEntry[0];
		public Dictionary<string, SpawnEntry> spawnEntriesDict = new Dictionary<string, SpawnEntry>();
		[Header("Make Rules")]
		public List<MakeRule> makeRules = new List<MakeRule>();
		public PartsCantOverlap partsCantOverlap;
		[HideInInspector]
		public List<SpawnEntry> _spawnEntries = new List<SpawnEntry>();
		public int alternateMaterialCount;
		[HideInInspector]
		public int alternateAreaIndex;
		public AudioSource musicAudioSource;
		public AudioClip[] alternateAreaMusics = new AudioClip[0];

		void Start ()
		{
#if UNITY_EDITOR
			if (!Application.isPlaying)
			{
				if (trs == null)
					trs = GetComponent<Transform>();
				return;
			}
#endif
			Init ();
			StartCoroutine(MakeLevel (size, spawnEntries));
		}

		public void Init ()
		{
			alternateAreaIndex = Random.Range(0, alternateMaterialCount);
			musicAudioSource.clip = alternateAreaMusics[alternateAreaIndex];
			spawnEntriesDict.Clear();
			_spawnEntries.Clear();
			for (int i = 0; i < extendableSizeLevelPartSpawnEntries.Length; i ++)
			{
				ExtendableSizeLevelPartSpawnEntry extendableSizeLevelPartSpawnEntry = extendableSizeLevelPartSpawnEntries[i];
				// if (extendableSizeLevelPartSpawnEntry.chance > 0)
				// {
					// _extendableSizeLevelPartSpawnEntries.Add(extendableSizeLevelPartSpawnEntry);
					// _levelPartSpawnEntries.Add(extendableSizeLevelPartSpawnEntry);
					_spawnEntries.Add(extendableSizeLevelPartSpawnEntry);
					spawnEntriesDict.Add(extendableSizeLevelPartSpawnEntry.trsPrefab.name, extendableSizeLevelPartSpawnEntry);
				// }
			}
			for (int i = 0; i < levelPartSpawnEntries.Length; i ++)
			{
				LevelPartSpawnEntry levelPartSpawnEntry = levelPartSpawnEntries[i];
				// if (levelPartSpawnEntry.chance > 0)
				// {
					// _levelPartSpawnEntries.Add(levelPartSpawnEntry);
					_spawnEntries.Add(levelPartSpawnEntry);
					spawnEntriesDict.Add(levelPartSpawnEntry.trsPrefab.name, levelPartSpawnEntry);
				// }
			}
			for (int i = 0; i < spawnEntries.Length; i ++)
			{
				SpawnEntry spawnEntry = spawnEntries[i];
				// if (spawnEntry.chance > 0)
				// {
					_spawnEntries.Add(spawnEntry);
					spawnEntriesDict.Add(spawnEntry.trsPrefab.name, spawnEntry);
				// }
			}
			makeRules.Clear();
			makeRules.Add(partsCantOverlap);
			colliders = new List<Collider2D>(GetComponentsInChildren<Collider2D>(true));
		}

		bool IsValid ()
		{
			makeRules.Sort(MakeRuleSorter);
			for (int i = 0; i < makeRules.Count; i ++)
			{
				MakeRule makeRule = makeRules[i];
				if (!makeRule.IsValid(this))
					return false;
			}
			return true;
		}

		int MakeRuleSorter (MakeRule m1, MakeRule m2)
		{
			if (m1.priority > m2.priority)
				return 1;
			else if (m1.priority < m2.priority)
				return -1;
			else
				return 0;
		}

		public static IEnumerator<ProceduralLevel> MakeLevel (Vector2Int size, SpawnEntry[] spawnEntries)
		{
			RectInt rectInt = new RectInt(0, 0, size.x, size.y);
			ProceduralLevel level = Instantiate(ProceduralLevel.Instance);
			level.enabled = false;
			level.spawnEntries = spawnEntries;
			level.Init ();
			Dictionary<Vector3, Transform> allTransforms = new Dictionary<Vector3, Transform>();
			List<Vector2> positions = new List<Vector2>(rectInt.ToRect().GetPointsInside(Vector2.one, new _RectOffset(Vector2.one / 2, Vector2.zero)));
			while (positions.Count > 0)
			{
				// int indexOfNull;
				// do
				// {
				// 	indexOfNull = level.colliders.IndexOf(null);
				// 	if (indexOfNull != -1)
				// 		level.colliders.RemoveAt(indexOfNull);
				// } while (indexOfNull != -1);
				int positionIndex = Random.Range(0, positions.Count);
				Vector3 position = positions[positionIndex];
				print(position);
				SpawnEntry spawnEntry;
				do
				{
					spawnEntry = level._spawnEntries[Random.Range(0, level._spawnEntries.Count)];
				} while (Random.value > spawnEntry.chance);
				Transform trs = spawnEntry.Spawn(position);
				level.trs.SetParent(trs);
				LevelPartSpawnEntry levelPartSpawnEntry = spawnEntry as LevelPartSpawnEntry;
				if (levelPartSpawnEntry != null)
				{
					ProceduralLevelPart levelPart = trs.GetComponent<ProceduralLevelPart>();
					levelPart.level = level;
					List<Collider2D> addedTiles = new List<Collider2D>(levelPart.colliders);
					List<ProceduralLevelPart> addedParts = new List<ProceduralLevelPart>();
					addedParts.Add(levelPart);
					List<ProceduralLevelPart> childrenParts = new List<ProceduralLevelPart>(levelPart.children);
					while (childrenParts.Count > 0)
					{
						ProceduralLevelPart childPart = childrenParts[0];
						childPart.level = level;
						addedTiles.AddRange(childPart.colliders);
						addedParts.Add(childPart);
						childrenParts.AddRange(childPart.children);
						childrenParts.RemoveAt(0);
					}
					level.colliders.AddRange(addedTiles);
					level.levelParts.AddRange(addedParts);
					if (!level.partsCantOverlap.IsValid(level))
					{
						DestroyImmediate(levelPart.gameObject);
						level.colliders = level.colliders.TrimEnd(addedTiles.Count);
						level.levelParts = level.levelParts.TrimEnd(addedParts.Count);
						continue;
					}
					HandleExtendLevelPart (levelPart);
					for (int i = 0; i < levelPart.colliders.Count; i ++)
					{
						Collider2D collider = levelPart.colliders[i];
						// if (collider == null)
						// {
						// 	levelPart.colliders.RemoveAt(i);
						// 	i --;
						// 	continue;
						// }
						allTransforms[collider.transform.position] = collider.transform;
						yield return null;
					}
				}
				else
				{
					Collider2D collider = trs.GetComponent<Collider2D>();
					if (collider != null)
						level.colliders.Add(collider);
					allTransforms[position] = trs;
				}
				positions.RemoveAt(positionIndex);
				yield return null;
			}
			yield return level;
		}

		static void HandleExtendLevelPart (ProceduralLevelPart levelPart)
		{
			ExtendableSizeProceduralLevelPart extendableSizeLevelPart = levelPart as ExtendableSizeProceduralLevelPart;
			if (extendableSizeLevelPart != null)
			{
				ExtendableSizeLevelPartSpawnEntry extendableSizeLevelPartSpawnEntry = (ExtendableSizeLevelPartSpawnEntry) levelPart.level.spawnEntriesDict[extendableSizeLevelPart.name];
				HandleExtendExtendableSizeLevelPart (extendableSizeLevelPart, extendableSizeLevelPartSpawnEntry.maxTimesToExtend);
			}
			for (int i = 0; i < levelPart.children.Length; i ++)
			{
				ProceduralLevelPart childPart = levelPart.children[i];
				HandleExtendLevelPart (childPart);
			}
		}

		static void HandleExtendExtendableSizeLevelPart (ExtendableSizeProceduralLevelPart extendableSizeLevelPart, int maxTimesToExtend)
		{
			for (int i = 0; i < maxTimesToExtend; i ++)
			{
				if (!extendableSizeLevelPart.CanExtendInDirection(extendableSizeLevelPart.GetExtendDirection() * (i + 1)))
				{
					for (int i2 = 0; i2 < i; i2 ++)
					{
						extendableSizeLevelPart.ExtendInDirection ();
						while (extendableSizeLevelPart.colliders.Contains(null))
						{
							extendableSizeLevelPart.colliders.Remove(null);
							extendableSizeLevelPart.lastAddedColliders.Remove(null);
							extendableSizeLevelPart.level.colliders.Remove(null);
						}
					}
					return;
				}
			}
		}

		public static IEnumerator<bool> IsGapTraversable (Vector3 gapVector)
		{
			// if (gapVector.y > Snake.instance.length.valueRange.max)
			// 	yield return false;
			// else if (gapVector.y >= 0)
			// {
			// 	if (gapVector.magnitude <= Snake.instance.length.valueRange.max)
			// 		yield return true;
			// 	else
			// 	{
			// 		throw new NotImplementedException();
			// 	}
			// }
			// else
			// {
			// 	throw new NotImplementedException();
			// }
			throw new NotImplementedException();
		}

		public static IEnumerator IsGapTraversable (Vector3 gapVector, float movementAngleFromGround, float duration = Mathf.Infinity)
		{
// 			Vector3 move = Vector3.RotateTowards(gapVector.GetXZ(), Vector3.up, movementAngleFromGround * Mathf.Deg2Rad, 0);
// 			Test test = Test.MakeTestForGap(gapVector);
// 			test.type = Test.Type.Movement;
// 			test.destination = gapVector;
// 			test.duration = duration;
// 			test.timeScale = 1;
// 			Test.InputEvent[] inputEvents = new Test.InputEvent[1] { new Test.InputEvent(move.normalized, 1) };
// #if UNITY_EDITOR
// 			EditorCoroutineWithData coroutineWithData = new EditorCoroutineWithData(Instance, test.TestRoutine(inputEvents));
// 			yield return new WaitForReturnedValueOfType_Editor<Test.Result>(coroutineWithData);
// #else
// 			CoroutineWithData coroutineWithData = new CoroutineWithData(Instance, test.TestRoutine(inputEvents));
// 			yield return new WaitForReturnedValueOfType<Test.Result>(coroutineWithData);
// #endif
// 			Test.Result result = (Test.Result) coroutineWithData.result;
// 			yield return result.endEvent == Test.Result.EndEvent.DidMovement;
			throw new NotImplementedException();
		}

		[Serializable]
		public class SpawnEntry
		{
			public Transform trsPrefab;
			public int prefabIndex;
			[Range(0, 1)]
			public float chance;

			public virtual Transform Spawn (Vector3 position, float? rotation = null)
			{
				if (rotation == null)
					rotation = trsPrefab.rotation.eulerAngles.z;
				Transform trs;
				if (prefabIndex == -1)
					trs = Instantiate(trsPrefab, position, Quaternion.Euler(0, 0, (float) rotation));
				else
					trs = ObjectPool.instance.SpawnComponent<Transform>(prefabIndex, position, Quaternion.Euler(0, 0, (float) rotation));
				trs.name = trs.name.RemoveStartAt("(Clone)");
				return trs;
			}
		}

		[Serializable]
		public class LevelPartSpawnEntry : SpawnEntry
		{
			public ProceduralLevelPart levelPartPrefab;
			public float[] allowedRotations = new float[0];

			public override Transform Spawn (Vector3 position, float? rotation = null)
			{
				if (rotation == null || !allowedRotations.Contains((float) rotation))
					rotation = allowedRotations[Random.Range(0, allowedRotations.Length)];
				return base.Spawn(position, rotation);
			}
		}

		[Serializable]
		public class ExtendableSizeLevelPartSpawnEntry : LevelPartSpawnEntry
		{
			public int maxTimesToExtend;
		}
		
		public class MakeRule
		{
			public int priority;

			public virtual bool IsValid (ProceduralLevel proceduralLevel)
			{
				throw new NotImplementedException();
			}
		}

		[Serializable]
		public class LevelMustBePossible : MakeRule
		{
			public override bool IsValid (ProceduralLevel proceduralLevel)
			{
				throw new NotImplementedException();
			}
		}

		[Serializable]
		public class LevelMustBeInDifficultyRange : MakeRule
		{
			public FloatRange difficultyRange;

			public override bool IsValid (ProceduralLevel proceduralLevel)
			{
				throw new NotImplementedException();
			}
		}

		[Serializable]
		public class LevelMustBeAboveCertainFairness : MakeRule
		{
			public float minFairness;

			public override bool IsValid (ProceduralLevel proceduralLevel)
			{
				throw new NotImplementedException();
			}
		}

		[Serializable]
		public class PartsCantOverlap : MakeRule
		{
			public override bool IsValid (ProceduralLevel proceduralLevel)
			{
				return IsValid(proceduralLevel.levelParts.ToArray());
			}

			public bool IsValid (ProceduralLevelPart[] levelParts)
			{
				for (int i = 0; i < levelParts.Length; i ++)
				{
					ProceduralLevelPart levelPart = levelParts[i];
					for (int i2 = i + 1; i2 < levelParts.Length; i2 ++)
					{
						ProceduralLevelPart levelPart2 = levelParts[i2];
						if (ProceduralLevelPart.AreOverlapping(levelPart, levelPart2))
							return false;
					}
				}
				return true;
			}
		}
	}
}
using System;
using Extensions;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Random = UnityEngine.Random;

namespace SlimeJump
{
	public class ProceduralLevelPart : MonoBehaviour
	{
		public Transform trs;
		public ProceduralLevel level;
		public List<Collider2D> colliders = new List<Collider2D>();
		public List<Path> paths = new List<Path>();
		public List<ProceduralLevelPart> neighbors = new List<ProceduralLevelPart>();
		public ProceduralLevelPart[] children = new ProceduralLevelPart[0];
		public FloatRange difficultyRange;
		public SerializableDictionary<Renderer, MaterialGroup> alternateMaterialGroupsDict = new SerializableDictionary<Renderer, MaterialGroup>();
		[HideInInspector]
		public bool isInitialized;

		public virtual void Start ()
		{
#if UNITY_EDITOR
			if (!Application.isPlaying)
			{
				Collider2D[] _colliders = GetComponentsInChildren<Collider2D>(true);
				if (_colliders.Length > 0)
					colliders = new List<Collider2D>(_colliders);
				children = GetComponentsInChildren<ProceduralLevelPart>().Remove(this);
				float[] pathDifficulties = new float[paths.Count];
				for (int i = 0; i < paths.Count; i ++)
				{
					Path path = paths[i];
					pathDifficulties[i] = path.difficulty;
				}
				difficultyRange = new FloatRange(Mathf.Min(pathDifficulties), Mathf.Max(pathDifficulties));
				return;
			}
#endif
		}

		public virtual void Init ()
		{
			alternateMaterialGroupsDict.Init ();
			foreach (KeyValuePair<Renderer, MaterialGroup> keyValuePair in alternateMaterialGroupsDict)
				keyValuePair.Key.sharedMaterial = keyValuePair.Value.materials[level.alternateAreaIndex];
			isInitialized = true;
		}

		public static bool AreOverlapping (ProceduralLevelPart levelPiece, ProceduralLevelPart levelPiece2, _RectOffset? levelPiece_RectOffset = null, _RectOffset? levelPiece2_RectOffset = null)
		{
			for (int i = 0; i < levelPiece.colliders.Count; i ++)
			{
				Collider2D collider = levelPiece.colliders[i];
				for (int i2 = 0; i2 < levelPiece2.colliders.Count; i2 ++)
				{
					Collider2D collider2 = levelPiece2.colliders[i2];
					if (collider != collider2 && collider.bounds.ToRect().Intersects(collider2.bounds.ToRect(), (_RectOffset) levelPiece_RectOffset, (_RectOffset) levelPiece2_RectOffset))
						return true;
				}
			}
			return false;
		}

		[Serializable]
		public struct Path
		{
			public Transform[] transforms;
			public bool isReversable;
			public float difficulty;
		}

		[Serializable]
		public struct MaterialGroup
		{
			public Material[] materials;
		}
	}
}
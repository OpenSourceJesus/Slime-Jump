#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace SlimeJump
{
	public class MakeBackgroundTilemaps : EditorScript
	{
		public Transform parent;
		public SerializableDictionary<byte, Transform> trsPrefabsDict = new SerializableDictionary<byte, Transform>();
		public Transform[] pathPointTransforms = new Transform[0];
		public byte heightRange;

		public override void Do ()
		{
			trsPrefabsDict.Init ();
			LineSegment2D[] lines = new LineSegment2D[pathPointTransforms.Length];
			Transform previousPathPointTrs = pathPointTransforms[0];
			float totalDistance = 0;
			for (int i = 1; i < lines.Length; i ++)
			{
				Transform pathPointTrs = pathPointTransforms[i];
				LineSegment2D line = new LineSegment2D(previousPathPointTrs.position, pathPointTrs.position);
				lines[i - 1] = line;
				previousPathPointTrs = pathPointTrs;
				totalDistance += line.GetLength();
			}
			float currentDistance = 0;
			int currentLineIndex = 0;
			LineSegment2D currentLine = lines[0];
			int trsPrefabIndex = Random.Range(0, trsPrefabsDict.Count);
			Transform trsPrefab = trsPrefabsDict.values[trsPrefabIndex];
			Transform trs = (Transform) PrefabUtility.InstantiatePrefab(trsPrefab);
			trs.SetParent(parent);
			trs.position = (Vector3) currentLine.start + new Vector3(0, Random.Range(-heightRange, heightRange), Random.Range(0f, 1f));
			float currentDistanceOnLine = 0;
			while (currentDistance < totalDistance)
			{
				float distanceDelta = (float) trsPrefabsDict.keys[trsPrefabIndex] / 2;
				distanceDelta = Random.Range(1, (int) distanceDelta);
				currentDistance += distanceDelta;
				currentDistanceOnLine += distanceDelta;
				if (currentDistanceOnLine >= currentLine.GetLength())
				{
					currentLineIndex ++;
					if (currentLineIndex == lines.Length)
						return;
					currentDistanceOnLine -= currentLine.GetLength();
					currentLine = lines[currentLineIndex];
				}
				Vector3 currentPosition = currentLine.GetPointWithDirectedDistance(currentDistanceOnLine);
				currentPosition.y += Random.Range(-heightRange, heightRange);
				currentPosition.z = Random.Range(0f, 1f);
				trsPrefabIndex = Random.Range(0, trsPrefabsDict.Count);
				trsPrefab = trsPrefabsDict.values[trsPrefabIndex];
				trs = (Transform) PrefabUtility.InstantiatePrefab(trsPrefab);
				trs.SetParent(parent);
				trs.position = currentPosition;
			}
		}
	}
}
#else
namespace SlimeJump
{
	public class MakeBackgroundTilemaps : EditorScript
	{
	}
}
#endif
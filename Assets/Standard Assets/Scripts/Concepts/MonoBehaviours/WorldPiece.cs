using Extensions;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace SlimeJump
{
	public class WorldPiece : MonoBehaviour
	{
		public Transform trs;
		[HideInInspector]
		public Vector2Int location;
		[HideInInspector]
		public Rect worldBoundsRect;
		[HideInInspector]
		public WorldPiece[] piecesToLoadAndUnloadWithMe = new WorldPiece[0];

		void OnDrawGizmosSelected ()
		{
			Gizmos.color = Color.green;
			Gizmos.DrawWireCube(worldBoundsRect.center, worldBoundsRect.size);
		}
	}
}
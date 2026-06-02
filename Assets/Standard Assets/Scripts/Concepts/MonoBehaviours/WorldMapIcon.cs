using System;
using Extensions;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace SlimeJump
{
	[ExecuteInEditMode]
	public class WorldMapIcon : MonoBehaviour
	{
		public Collider2D objectCollider;
		public Collider2D iconCollider;
		public bool onlyMakeIfExplored;
		[HideInInspector]
		public RectInt cellBoundsRect;
		[HideInInspector]
		public bool isActive;
		public GameObjectEntry[] goEntries = new GameObjectEntry[0];

		public virtual void Start ()
		{
#if UNITY_EDITOR
			if (!Application.isPlaying)
			{
				if (goEntries.Length == 0)
					goEntries = goEntries.Add(new GameObjectEntry(gameObject));
				else
				{
					foreach (GameObjectEntry goEntry in goEntries)
					{
						if (goEntry.go != null)
							goEntry.layer = goEntry.go.layer;
					}
				}
				return;
			}
			else
			{
				cellBoundsRect = new RectInt();
				cellBoundsRect.size = WorldMap.Instance.unexploredTilemap.WorldToCell(objectCollider.bounds.max).ToVec2Int() - WorldMap.Instance.unexploredTilemap.WorldToCell(objectCollider.bounds.min).ToVec2Int() + Vector2Int.one;
				cellBoundsRect.position = new Vector2Int(WorldMap.Instance.unexploredTilemap.WorldToCell(objectCollider.bounds.min).ToVec2Int().x, WorldMap.Instance.unexploredTilemap.WorldToCell(objectCollider.bounds.max).ToVec2Int().y);
				if (goEntries.Length == 0)
					goEntries = goEntries.Add(new GameObjectEntry(gameObject));
				else
				{
					foreach (GameObjectEntry goEntry in goEntries)
					{
						if (goEntry.go != null)
							goEntry.layer = goEntry.go.layer;
					}
				}
			}
#endif
			WorldMap.worldMapIcons = WorldMap.worldMapIcons.Add(this);
		}

		public virtual void OnDestroy ()
		{
#if UNITY_EDITOR
			if (!Application.isPlaying)
				return;
#endif
			WorldMap.worldMapIcons = WorldMap.worldMapIcons.Remove(this);
		}

		public virtual void MakeIcon ()
		{
			isActive = true;
			foreach (GameObjectEntry goEntry in goEntries)
				goEntry.go.layer = LayerMask.NameToLayer("World Map");
		}

		public virtual void DestroyIcon ()
		{
			foreach (GameObjectEntry goEntry in goEntries)
				goEntry.go.layer = goEntry.layer;
			isActive = false;
		}

		public virtual void Highlight ()
		{
			foreach (GameObjectEntry goEntry in goEntries)
			{
				if (goEntry.highlightedIndicator != null)
					goEntry.highlightedIndicator.SetActive(true);
			}
		}

		public virtual void Unhighlight ()
		{
			foreach (GameObjectEntry goEntry in goEntries)
			{
				if (goEntry.highlightedIndicator != null)
					goEntry.highlightedIndicator.SetActive(false);
			}
		}

		[Serializable]
		public class GameObjectEntry
		{
			public GameObject go;
			public int layer;
			public GameObject highlightedIndicator;

			public GameObjectEntry (GameObject go)
			{
				this.go = go;
				layer = go.layer;
			}
		}
	}
}
#if UNITY_EDITOR
using System;
using Extensions;
using UnityEngine;
using UnityEditor;
using UnityEngine.Tilemaps;
using System.Collections.Generic;
using UnityEditor.SceneManagement;

namespace SlimeJump
{
	public class WorldMakerWindow : EditorWindow
	{
		public static WorldMakerWindow instance;
		public static bool enableWorld;
		public static bool enablePieces;
		public static bool piecesAreActive;
		public static bool worldIsActive;
		public static bool showPieces;
		public static bool piecesAreShown;

		[MenuItem("Window/World %w")]
		public static void Init ()
		{
			instance = (WorldMakerWindow) EditorWindow.GetWindow(typeof(WorldMakerWindow));
			enableWorld = EditorPrefs.GetBool("Enable world", false);
			enablePieces = EditorPrefs.GetBool("Enable pieces", false);
			showPieces = EditorPrefs.GetBool("Show pieces", false);
			instance.Show();
		}

		public virtual void OnGUI ()
		{
			GUIContent guiContent = new GUIContent();
			guiContent.text = "Rebuild";
			bool rebuild = EditorGUILayout.DropdownButton(guiContent, FocusType.Passive);
			if (rebuild)
				Rebuild ();
			guiContent.text = "Make Pieces";
			bool makePieces = EditorGUILayout.DropdownButton(guiContent, FocusType.Passive);
			if (makePieces)
				MakePieces ();
			guiContent.text = "Remove Pieces";
			bool removePieces = EditorGUILayout.DropdownButton(guiContent, FocusType.Passive);
			if (removePieces)
				RemovePieces ();
			enableWorld = EditorGUILayout.Toggle("Enable World", enableWorld);
			if (enableWorld != worldIsActive)
				SetWorldActive (enableWorld);
			EditorPrefsExtensions.SetBool ("Enable world", enableWorld);
			enablePieces = EditorGUILayout.Toggle("Enable Pieces", enablePieces);
			if (enablePieces != piecesAreActive)
				SetPiecesActive (enablePieces);
			EditorPrefsExtensions.SetBool ("Enable pieces", enablePieces);
			showPieces = EditorGUILayout.Toggle("Show Pieces", showPieces);
			if (showPieces != piecesAreShown)
				ShowPieces (showPieces);
			EditorPrefsExtensions.SetBool ("Show pieces", showPieces);
		}

		[MenuItem("World/Rebuild %&r")]
		public static void Rebuild ()
		{
			RemovePieces ();
			MakePieces ();
			enablePieces = true;
			SetPiecesActive (true);
			EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetSceneByName("World"));
		}

		public static void ShowPieces (bool show)
		{
			World.Instance.SetPieces ();
			foreach (WorldPiece worldPiece in World.instance.piecesDict.Values)
				worldPiece.gameObject.SetActive(show);
			piecesAreShown = show;
		}

		public static void SetWorldActive (bool active)
		{
			for (int i = 0; i < World.Instance.worldObjects.Length; i ++)
			{
				ObjectInWorld worldObject = World.instance.worldObjects[i];
				if (worldObject != null && worldObject.enabled)
					worldObject.trs.gameObject.SetActive(active);
			}
			for (int i = 0; i < World.instance.tilemaps.Length; i ++)
			{
				Tilemap tilemap = World.instance.tilemaps[i];
				tilemap.gameObject.SetActive(active);
			}
			worldIsActive = active;
		}

		public static void SetPiecesActive (bool active)
		{
			World.Instance.piecesParent.gameObject.SetActive(active);
			piecesAreActive = active;
		}
		
		[MenuItem("World/Make pieces")]
		public static void MakePieces ()
		{
			Vector2Int pieceLocation = new Vector2Int();
			List<ObjectInWorld> worldObjects = new List<ObjectInWorld>(World.Instance.worldObjects);
			List<ObjectInWorld> newWorldObjects = new List<ObjectInWorld>();
			for (int x = WorldMap.Instance.cellBounds.min.x; x < WorldMap.instance.cellBounds.max.x; x += World.instance.sizeOfPieces.x)
			{
				pieceLocation.y = 0;
				for (int y = WorldMap.instance.cellBounds.min.y; y < WorldMap.instance.cellBounds.max.y; y += World.instance.sizeOfPieces.y)
				{
					WorldPiece piece = Instantiate(World.instance.piecePrefab, World.Instance.piecesParent);
					piece.location = pieceLocation;
					piece.name += "[" + pieceLocation + "]";
					Vector3Int cellBoundsMin = new Vector3Int(x, y);
					Vector3Int cellBoundsMax = cellBoundsMin + (Vector3Int) World.instance.sizeOfPieces;
					BoundsInt boundsInt = new BoundsInt();
					boundsInt.SetMinMax(cellBoundsMin, cellBoundsMax + Vector3Int.forward);
					Vector2 worldBoundsMin = WorldMap.instance.unexploredTilemap.CellToWorld(cellBoundsMin);
					Vector2 worldBoundsMax = WorldMap.instance.unexploredTilemap.CellToWorld(cellBoundsMax);
					piece.worldBoundsRect = Rect.MinMaxRect(worldBoundsMin.x, worldBoundsMin.y, worldBoundsMax.x, worldBoundsMax.y);
					piece.trs.position = piece.worldBoundsRect.center;
					for (int i = 0; i < worldObjects.Count; i ++)
					{
						ObjectInWorld originalWorldObject = worldObjects[i];
						if (piece.worldBoundsRect.Contains(originalWorldObject.trs.position.ToVec2Int()))
						{
							// if (PrefabUtility.GetPrefabInstanceStatus(originalWorldObject) == PrefabInstanceStatus.NotAPrefab)
								ObjectInWorld newWorldObject = Instantiate(originalWorldObject, piece.trs);
							// else
							// 	ObjectInWorld newWorldObject = PrefabExtensions.ClonePrefabInstance(originalWorldObject.gameObject).GetComponent<ObjectInWorld>();
							ObjectInWorld[] childWorldObjects = newWorldObject.GetComponentsInChildren<ObjectInWorld>();
							for (int i2 = 1; i2 < childWorldObjects.Length; i2 ++)
							{
								ObjectInWorld childWorldObject = childWorldObjects[i2];
								if (childWorldObject != null)
								{
									if (!childWorldObject.keepParented)
										DestroyImmediate(childWorldObject.gameObject);
									else
										childWorldObject.duplicate = newWorldObject.trs.FindEquivalentChild(childWorldObject.trs, originalWorldObject.trs).GetComponent<ObjectInWorld>();
								}
							}
							if (newWorldObject != null)
							{
								newWorldObject.trs.position = originalWorldObject.trs.position;
								newWorldObject.trs.rotation = originalWorldObject.trs.rotation;
								newWorldObject.trs.SetWorldScale(originalWorldObject.trs.lossyScale);
								newWorldObject.name = originalWorldObject.name;
								newWorldObject.duplicate = originalWorldObject;
								newWorldObject.pieceIAmIn = piece;
								newWorldObject.tag = "Untagged";
								newWorldObjects.Add(newWorldObject);
								originalWorldObject.duplicate = newWorldObject;
								originalWorldObject.tag = "EditorOnly";
								worldObjects.RemoveAt(i);
								i --;
							}
						}
					}
					for (int i = 0; i < World.instance.tilemaps.Length; i ++)
					{
						Tilemap tilemap = World.instance.tilemaps[i];
						foreach (Vector3Int cellPosition in boundsInt.allPositionsWithin)
						{
							Tile tile = (Tile) tilemap.GetTile(cellPosition);
							if (tile != null)
							{
								Vector2 position = tilemap.GetCellCenterWorld(cellPosition);
								if (piece.worldBoundsRect.Contains(position))
									Instantiate(tile.gameObject, position, Quaternion.identity, piece.trs);
							}
						}
					}
					piece.gameObject.SetActive(false);
					pieceLocation.y ++;
				}
				pieceLocation.x ++;
			}
			for (int i = 0; i < newWorldObjects.Count; i ++)
			{
				ObjectInWorld worldObject = newWorldObjects[i];
				if (worldObject != null)
				{
					FollowWaypoints followWaypoints = worldObject.GetComponent<FollowWaypoints>();
					if (followWaypoints != null)
					{
						if (followWaypoints.playerAttachParent != null)
							followWaypoints.playerAttachParent = worldObject.duplicate.trs.FindEquivalentChild(worldObject.duplicate.GetComponent<FollowWaypoints>().playerAttachParent, followWaypoints.trs);
					}
					else
					{
						ShooterTrap shooterTrap = worldObject.GetComponent<ShooterTrap>();
						if (shooterTrap != null)
						{
							for (int i2 = 0; i2 < shooterTrap.ignoreShotCollisionWithGos.Length; i2 ++)
							{
								GameObject ignoreShotCollisionWithGo = shooterTrap.ignoreShotCollisionWithGos[i2];
								shooterTrap.ignoreShotCollisionWithGos[i2] = ignoreShotCollisionWithGo.GetComponent<ObjectInWorld>().duplicate.gameObject;
							}
						}
						else
						{
							ChaserObject chaserOb = worldObject.GetComponent<ChaserObject>();
							if (chaserOb != null)
								chaserOb.OnValidate ();
						}
					}
					for (int i2 = 0; i2 < worldObject.objectsToLoadAndUnloadWithMe.Length; i2 ++)
					{
						ObjectInWorld worldObject2 = worldObject.objectsToLoadAndUnloadWithMe[i2];
						if (worldObject2 != null)
						{
							if (worldObject.name == worldObject2.name)
								worldObject2 = worldObject;
							else
								worldObject2 = worldObject2.duplicate;
							if (worldObject2 != null && worldObject2.pieceIAmIn != null && !worldObject.pieceIAmIn.piecesToLoadAndUnloadWithMe.Contains(worldObject2.pieceIAmIn))
								worldObject.pieceIAmIn.piecesToLoadAndUnloadWithMe = worldObject.pieceIAmIn.piecesToLoadAndUnloadWithMe.Add(worldObject2.pieceIAmIn);
							worldObject.objectsToLoadAndUnloadWithMe[i2] = worldObject2;
						}
					}
				}
			}
			World.instance.maxPieceLocation = pieceLocation - Vector2Int.one;
		}

		[MenuItem("World/Remove pieces")]
		public static void RemovePieces ()
		{
			for (int i = 0; i < World.Instance.piecesParent.childCount; i ++)
			{
				WorldPiece piece = World.instance.piecesParent.GetChild(i).GetComponent<WorldPiece>();
				DestroyImmediate(piece.gameObject);
				i --;
			}
		}
	}
}
#endif

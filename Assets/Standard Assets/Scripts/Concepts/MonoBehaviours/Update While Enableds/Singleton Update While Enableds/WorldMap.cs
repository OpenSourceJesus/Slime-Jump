using Extensions;
using UnityEngine;
using System.Collections;
using UnityEngine.Tilemaps;
using UnityEngine.InputSystem;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace SlimeJump
{
	[ExecuteInEditMode]
	public class WorldMap : SingletonUpdateWhileEnabled<WorldMap>
	{
		public Tilemap unexploredTilemap;
		public Transform spriteMaskTrs;
		public TileBase unexploredTile;
		public float cameraMoveSpeed;
		public float normalizedScreenBorder;
		public BoundsInt cellBounds;
		public RectInt offsetCellBounds;
		public static WorldMapIcon[] worldMapIcons = new WorldMapIcon[0];
		public static bool isOpen;
		public static SavePoint savePointPlayerIsAt;
		public static bool foundNewCellSinceSaved;
		public static bool[] exploredCellPositions = new bool[0];
		public static bool[] ExploredCellPositions
		{
			get
			{
				return SaveAndLoadManager.GetBoolArray("Explored cells " + _SceneManager.CurrentScene.name, new bool[0]);
			}
			set
			{
				SaveAndLoadManager.SetBoolArray ("Explored cells " + _SceneManager.CurrentScene.name, value);
			}
		}
		HashSet<Vector2Int> exploredCellPositionsAtLastTimeOpened = new HashSet<Vector2Int>();
		Vector2Int minCellPosition;
		Vector2Int maxCellPosition;
		Vector2Int prevMinCellPosition;
		Vector2 moveInput;
		Rect screenWithoutBorder;
		SavePoint fastTravelToSavePoint;

		public static void _Update ()
		{
			if (!Application.isPlaying)
			{
				Instance.unexploredTilemap.ClearAllTiles();
				instance.cellBounds = new BoundsInt();
				Collider2D[] colliders = FindObjectsOfType<Collider2D>();
				foreach (Collider2D collider in colliders)
				{
					Vector3Int min = instance.unexploredTilemap.WorldToCell(collider.bounds.min);
					Vector3Int max = instance.unexploredTilemap.WorldToCell(collider.bounds.max) + Vector3Int.one;
					instance.cellBounds.SetMinMax(Vector3Int.Min(min, instance.cellBounds.min), Vector3Int.Max(max, instance.cellBounds.max));
				}
				instance.cellBounds.SetMinMax(instance.cellBounds.min + instance.offsetCellBounds.position.ToVec3Int(), instance.cellBounds.max + instance.offsetCellBounds.size.ToVec3Int());
				Vector2 boundsMin = instance.unexploredTilemap.CellToWorld(instance.cellBounds.min);
				Vector2 boundsMax = instance.unexploredTilemap.CellToWorld(instance.cellBounds.max);
				instance.spriteMaskTrs.position = ((boundsMax + boundsMin) / 2).SetZ(-99);
				instance.spriteMaskTrs.localScale = boundsMax - boundsMin;
			}
			else
			{
				TileBase[] tiles = new TileBase[Instance.cellBounds.size.x * instance.cellBounds.size.y];
				for (int i = 0; i < tiles.Length; i ++)
					tiles[i] = instance.unexploredTile;
				instance.unexploredTilemap.SetTilesBlock(instance.cellBounds, tiles);
				if (Debug.isDebugBuild)
					instance.unexploredTilemap.color = instance.unexploredTilemap.color.SetAlpha(0);
			}
		}

		public override void Awake ()
		{
#if UNITY_EDITOR
			if (!Application.isPlaying)
				return;
#endif
			base.Awake ();
			_Update ();
			SaveAndLoadManager.Init ();
			GameCamera.Instance.Awake ();
			minCellPosition = (unexploredTilemap.WorldToCell(GameCamera.instance.viewRect.min) - unexploredTilemap.cellBounds.min).ToVec2Int();
			maxCellPosition = (unexploredTilemap.WorldToCell(GameCamera.instance.viewRect.max) - unexploredTilemap.cellBounds.min).ToVec2Int();
			maxCellPosition.Clamp(Vector2Int.zero, unexploredTilemap.cellBounds.size.ToVec2Int() - Vector2Int.one);
			exploredCellPositions = new bool[unexploredTilemap.cellBounds.size.x * unexploredTilemap.cellBounds.size.y];
			if (ExploredCellPositions.Length == 0)
			{
				for (int x = minCellPosition.x; x <= maxCellPosition.x; x ++)
				{
					for (int y = minCellPosition.y; y <= maxCellPosition.y; y ++)
						exploredCellPositions[x + y * unexploredTilemap.cellBounds.size.x] = true;
				}
			}
			else
				ExploredCellPositions.CopyTo(exploredCellPositions, 0);
			prevMinCellPosition = minCellPosition;
			screenWithoutBorder = new Rect();
			screenWithoutBorder.size = new Vector2(Screen.width - normalizedScreenBorder * Screen.width, Screen.height - normalizedScreenBorder * Screen.height);
			screenWithoutBorder.center = new Vector2(Screen.width / 2, Screen.height / 2);
			WorldMapCamera.Instance.HandleViewSize ();
			StartCoroutine(Init ());
		}
		
		public override void DoUpdate ()
		{
			if (!isOpen)
			{
				UpdateExplored ();
				if ((InputManager.UsingKeyboard && Keyboard.current.tabKey.wasPressedThisFrame) || (InputManager.UsingGamepad && Gamepad.current.selectButton.wasPressedThisFrame))
					Open ();
			}
			else
			{
				WorldMapCamera.instance.DoUpdate ();
				moveInput = Vector2.zero;
				// GameManager.activeCursorEntry.rectTrs.gameObject.SetActive(true);
				// if (InputManager.UsingGamepad)
				// {
				// 	GameManager.activeCursorEntry.rectTrs.position += (Vector3) moveInput * GameManager.cursorMoveSpeed * Time.unscaledDeltaTime;
				// 	GameManager.activeCursorEntry.rectTrs.position = GameManager.activeCursorEntry.rectTrs.position.ClampComponents(Vector3.zero, new Vector2(Screen.width, Screen.height));
				// }
				if (InputManager.UsingGamepad)
				{
					moveInput = Vector2.ClampMagnitude(Gamepad.current.leftStick.ReadValue(), 1);
					WorldMapCamera.instance.trs.position += (Vector3) moveInput * cameraMoveSpeed * Time.unscaledDeltaTime;
				}
				else if (InputManager.UsingMouse && !screenWithoutBorder.Contains(Mouse.current.position.ReadValue()))
				{
					moveInput = Mouse.current.position.ReadValue() - new Vector2(Screen.width / 2, Screen.height / 2);
					moveInput /= new Vector2(Screen.width / 2, Screen.height / 2).magnitude;
					WorldMapCamera.instance.trs.position += (Vector3) moveInput * cameraMoveSpeed * Time.unscaledDeltaTime;
					// if (GameManager.activeCursorEntry.name != "Arrow")
					// {
					// 	GameManager.cursorEntriesDict["Arrow"].SetAsActive ();
					// 	GameManager.activeCursorEntry.rectTrs.position = GameManager.cursorEntriesDict["Default"].rectTrs.position;
					// }
					// GameManager.activeCursorEntry.rectTrs.up = moveInput;
					// if (GameManager.Instance.worldMapTutorialConversation.currentDialog == GameManager.Instance.worldMapMoveViewTutorialDialog)
					// 	DialogManager.Instance.EndDialog (GameManager.Instance.worldMapMoveViewTutorialDialog);
				}
				if ((InputManager.UsingKeyboard && Keyboard.current.tabKey.wasPressedThisFrame) || (InputManager.UsingGamepad && Gamepad.current.selectButton.wasPressedThisFrame))
					Close ();
				if (savePointPlayerIsAt != null)
					HandleFastTravel ();
			}
			// else if (GameManager.activeCursorEntry.name != "Default")
			// {
			// 	GameManager.cursorEntriesDict["Default"].SetAsActive ();
			// 	GameManager.activeCursorEntry.rectTrs.position = GameManager.cursorEntriesDict["Arrow"].rectTrs.position;
			// }
		}

		IEnumerator Init ()
		{
			FollowWaypoints.instances = FindObjectsOfType<FollowWaypoints>(true);
			Open ();
			yield return new WaitForEndOfFrame();
			Close ();
		}

		void HandleFastTravel ()
		{
			if (InputManager.UsingGamepad)
			{
				SavePoint prevFastTravelToSavePoint = fastTravelToSavePoint;
				List<Transform> savePointTransforms = new List<Transform>();
				for (int i = 0; i < SavePoint.instances.Length; i ++)
				{
					SavePoint savePoint = SavePoint.instances[i];
					if ((savePoint.Touched || Debug.isDebugBuild) && savePoint != savePointPlayerIsAt)
						savePointTransforms.Add(savePoint.transform);
				}
				if (savePointTransforms.Count == 0)
					return;
				fastTravelToSavePoint = savePointTransforms.ToArray().GetClosestTransform_2D(WorldMapCamera.instance.trs.position).GetComponent<SavePoint>();
				if (fastTravelToSavePoint != prevFastTravelToSavePoint && prevFastTravelToSavePoint != null)
					prevFastTravelToSavePoint.worldMapIcon.Unhighlight ();
				fastTravelToSavePoint.worldMapIcon.Highlight ();
			}
			else if (InputManager.UsingMouse)
			{
				for (int i = 0; i < SavePoint.instances.Length; i ++)
				{
					SavePoint savePoint = SavePoint.instances[i];
					if (savePoint.Touched || Debug.isDebugBuild)
					{
						if (savePoint != savePointPlayerIsAt && savePoint.worldMapIcon.iconCollider.bounds.ToRect().Contains(WorldMapCamera.instance.camera.ScreenToWorldPoint(Mouse.current.position.ReadValue())))
						{
							if (fastTravelToSavePoint != null)
								fastTravelToSavePoint.worldMapIcon.Unhighlight ();
							fastTravelToSavePoint = savePoint;
							fastTravelToSavePoint.worldMapIcon.Highlight ();
							break;
						}
					}
				}
			}
			if (fastTravelToSavePoint != null && (InputManager.UsingMouse && Mouse.current.leftButton.wasPressedThisFrame) || (InputManager.UsingGamepad && Gamepad.current.aButton.wasPressedThisFrame))
			{
				Player.instance.SavedPosition = fastTravelToSavePoint.worldMapIcon.objectCollider.bounds.center;
				OneLifeAchievement.savePointsNamesTouchedInOneLife.Clear();
				if (SpeedAchievement.current != null)
					SpeedAchievement.current.SavePointsTouchedCntWithoutFastTraveling = 0;
				SaveAndLoadManager.Save ();
				Close ();
				isOpen = false;
				_SceneManager.Instance.RestartScene ();
			}
		}

		void Open ()
		{
			isOpen = true;
			GameManager.SetPaused (true);
			unexploredTilemap.gameObject.SetActive(true);
			foreach (WorldPiece worldPiece in World.Instance.piecesDict.Values)
				worldPiece.gameObject.SetActive(true);
			foreach (WorldMapIcon worldMapIcon in worldMapIcons)
			{
				foreach (Vector2Int position in worldMapIcon.cellBoundsRect.allPositionsWithin)
				{
					Vector2Int _position = position - (Vector2Int) unexploredTilemap.cellBounds.min;
					if (!worldMapIcon.onlyMakeIfExplored || Debug.isDebugBuild || exploredCellPositions[_position.x + _position.y * unexploredTilemap.cellBounds.size.x])
						worldMapIcon.MakeIcon ();
				}
			}
			HashSet<Vector3Int> exploredCellPositionsSinceLastTimeOpened = new HashSet<Vector3Int>();
			for (int i = 0; i < exploredCellPositions.Length; i ++)
			{
				bool exploredCellPosition = exploredCellPositions[i];
				if (exploredCellPosition)
					exploredCellPositionsSinceLastTimeOpened.Add(new Vector3Int(i % unexploredTilemap.cellBounds.size.x, i / unexploredTilemap.cellBounds.size.x) + unexploredTilemap.cellBounds.min);
			}
			foreach (Vector2Int exploredCellPositionAtLastTimeOpened in exploredCellPositionsAtLastTimeOpened)
				exploredCellPositionsSinceLastTimeOpened.Remove(exploredCellPositionAtLastTimeOpened.ToVec3Int());
			Vector3Int[] _exploredCellPositionsSinceLastTimeOpened = new Vector3Int[exploredCellPositionsSinceLastTimeOpened.Count];
			exploredCellPositionsSinceLastTimeOpened.CopyTo(_exploredCellPositionsSinceLastTimeOpened);
			unexploredTilemap.SetTiles(_exploredCellPositionsSinceLastTimeOpened, new TileBase[exploredCellPositionsSinceLastTimeOpened.Count]);
			WorldMapCamera.Instance.trs.position = Player.Instance.trs.position.SetZ(WorldMapCamera.instance.trs.position.z);
			WorldMapCamera.instance.gameObject.SetActive(true);
			// if (InputManager.UsingGamepad)
			// {
			// 	GameManager.cursorEntriesDict["Default"].SetAsActive ();
			// 	GameManager.activeCursorEntry.rectTrs.localPosition = Vector2.zero;
			// }
		}

		void Close ()
		{
			isOpen = false;
			GameManager.SetPaused (false);
			for (int i = 0; i < exploredCellPositions.Length; i ++)
			{
				bool exploredCellPosition = exploredCellPositions[i];
				if (exploredCellPosition)
					exploredCellPositionsAtLastTimeOpened.Add(new Vector2Int(i % unexploredTilemap.cellBounds.size.x, i / unexploredTilemap.cellBounds.size.x) + unexploredTilemap.cellBounds.min.ToVec2Int());
			}
			foreach (WorldMapIcon worldMapIcon in worldMapIcons)
			{
				worldMapIcon.DestroyIcon ();
				worldMapIcon.Unhighlight ();
			}
			fastTravelToSavePoint = null;
			WorldMapCamera.instance.gameObject.SetActive(false);
			// if (!InputManager.UsingGamepad)
			// 	GameManager.cursorEntriesDict["Default"].SetAsActive ();
			// else
			// 	GameManager.activeCursorEntry.rectTrs.gameObject.SetActive(false);
			unexploredTilemap.gameObject.SetActive(false);
			if (World.instance != null)
			{
				foreach (WorldPiece worldPiece in World.instance.piecesDict.Values)
					worldPiece.gameObject.SetActive(World.instance.activePieces.Contains(worldPiece));
			}
		}
		
		void UpdateExplored ()
		{
			int x;
			int y;
			minCellPosition = (unexploredTilemap.WorldToCell(GameCamera.Instance.viewRect.min) - unexploredTilemap.cellBounds.min).ToVec2Int();
			minCellPosition.Clamp(Vector2Int.zero, unexploredTilemap.cellBounds.size.ToVec2Int() - Vector2Int.one);
			maxCellPosition = (unexploredTilemap.WorldToCell(GameCamera.instance.viewRect.max) - unexploredTilemap.cellBounds.min).ToVec2Int();
			maxCellPosition.Clamp(Vector2Int.zero, unexploredTilemap.cellBounds.size.ToVec2Int() - Vector2Int.one);
			if (minCellPosition.x > prevMinCellPosition.x)
			{
				x = maxCellPosition.x;
				for (y = minCellPosition.y; y <= maxCellPosition.y; y ++)
				{
					if (!exploredCellPositions[x + y * unexploredTilemap.cellBounds.size.x])
					{
						exploredCellPositions[x + y * unexploredTilemap.cellBounds.size.x] = true;
						foundNewCellSinceSaved = true;
					}
				}
				if (minCellPosition.y > prevMinCellPosition.y)
				{
					y = maxCellPosition.y;
					for (x = minCellPosition.x; x <= maxCellPosition.x; x ++)
					{
						if (!exploredCellPositions[x + y * unexploredTilemap.cellBounds.size.x])
						{
							exploredCellPositions[x + y * unexploredTilemap.cellBounds.size.x] = true;
							foundNewCellSinceSaved = true;
						}
					}
				}
				else if (minCellPosition.y < prevMinCellPosition.y)
				{
					y = minCellPosition.y;
					for (x = minCellPosition.x; x <= maxCellPosition.x; x ++)
					{
						if (!exploredCellPositions[x + y * unexploredTilemap.cellBounds.size.x])
						{
							exploredCellPositions[x + y * unexploredTilemap.cellBounds.size.x] = true;
							foundNewCellSinceSaved = true;
						}
					}
				}
			}
			else if (minCellPosition.x < prevMinCellPosition.x)
			{
				x = minCellPosition.x;
				for (y = minCellPosition.y; y <= maxCellPosition.y; y ++)
				{
					if (!exploredCellPositions[x + y * unexploredTilemap.cellBounds.size.x])
					{
						exploredCellPositions[x + y * unexploredTilemap.cellBounds.size.x] = true;
						foundNewCellSinceSaved = true;
					}
				}
				if (minCellPosition.y > prevMinCellPosition.y)
				{
					y = maxCellPosition.y;
					for (x = minCellPosition.x; x <= maxCellPosition.x; x ++)
					{
						if (!exploredCellPositions[x + y * unexploredTilemap.cellBounds.size.x])
						{
							exploredCellPositions[x + y * unexploredTilemap.cellBounds.size.x] = true;
							foundNewCellSinceSaved = true;
						}
					}
				}
				else if (minCellPosition.y < prevMinCellPosition.y)
				{
					y = minCellPosition.y;
					for (x = minCellPosition.x; x <= maxCellPosition.x; x ++)
					{
						if (!exploredCellPositions[x + y * unexploredTilemap.cellBounds.size.x])
						{
							exploredCellPositions[x + y * unexploredTilemap.cellBounds.size.x] = true;
							foundNewCellSinceSaved = true;
						}
					}
				}
			}
			else if (minCellPosition.y > prevMinCellPosition.y)
			{
				y = maxCellPosition.y;
				for (x = minCellPosition.x; x <= maxCellPosition.x; x ++)
				{
					if (!exploredCellPositions[x + y * unexploredTilemap.cellBounds.size.x])
					{
						exploredCellPositions[x + y * unexploredTilemap.cellBounds.size.x] = true;
						foundNewCellSinceSaved = true;
					}
				}
				if (minCellPosition.x > prevMinCellPosition.x)
				{
					x = maxCellPosition.x;
					for (y = minCellPosition.y; y <= maxCellPosition.y; y ++)
					{
						if (!exploredCellPositions[x + y * unexploredTilemap.cellBounds.size.x])
						{
							exploredCellPositions[x + y * unexploredTilemap.cellBounds.size.x] = true;
							foundNewCellSinceSaved = true;
						}
					}
				}
				else if (minCellPosition.x < prevMinCellPosition.x)
				{
					x = minCellPosition.x;
					for (y = minCellPosition.y; y <= maxCellPosition.y; y ++)
					{
						if (!exploredCellPositions[x + y * unexploredTilemap.cellBounds.size.x])
						{
							exploredCellPositions[x + y * unexploredTilemap.cellBounds.size.x] = true;
							foundNewCellSinceSaved = true;
						}
					}
				}
			}
			else if (minCellPosition.y < prevMinCellPosition.y)
			{
				y = minCellPosition.y;
				for (x = minCellPosition.x; x <= maxCellPosition.x; x ++)
				{
					if (!exploredCellPositions[x + y * unexploredTilemap.cellBounds.size.x])
					{
						exploredCellPositions[x + y * unexploredTilemap.cellBounds.size.x] = true;
						foundNewCellSinceSaved = true;
					}
				}
				if (minCellPosition.x > prevMinCellPosition.x)
				{
					x = maxCellPosition.x;
					for (y = minCellPosition.y; y <= maxCellPosition.y; y ++)
					{
						if (!exploredCellPositions[x + y * unexploredTilemap.cellBounds.size.x])
						{
							exploredCellPositions[x + y * unexploredTilemap.cellBounds.size.x] = true;
							foundNewCellSinceSaved = true;
						}
					}
				}
				else if (minCellPosition.x < prevMinCellPosition.x)
				{
					x = minCellPosition.x;
					for (y = minCellPosition.y; y <= maxCellPosition.y; y ++)
					{
						if (!exploredCellPositions[x + y * unexploredTilemap.cellBounds.size.x])
						{
							exploredCellPositions[x + y * unexploredTilemap.cellBounds.size.x] = true;
							foundNewCellSinceSaved = true;
						}
					}
				}
			}
			prevMinCellPosition = minCellPosition;
		}
	}
}

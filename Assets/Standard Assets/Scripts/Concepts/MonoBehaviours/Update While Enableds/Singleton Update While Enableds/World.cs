using Extensions;
using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace SlimeJump
{
	public class World : SingletonUpdateWhileEnabled<World>
	{
		public ObjectInWorld[] worldObjects;
		public Vector2Int sizeOfPieces;
		public WorldPiece piecePrefab;
		public Dictionary<Vector2Int, WorldPiece> piecesDict = new Dictionary<Vector2Int, WorldPiece>();
		public WorldPiece[,] pieces;
		public Vector2Int maxPieceLocation;
		public Transform piecesParent;
		public Rect worldBoundsRect;
		public float loadPiecesRange;
		public List<WorldPiece> activePieces = new List<WorldPiece>();
		public AudioSource musicSource;
		public Zone2D[] areaZones = new Zone2D[0];
		public AudioClip[] areaMusics = new AudioClip[0];
		public float[] areaMusicsVolumes = new float[0];
		public float musicFadeDur;
		public float extraUnloadPiecesRange;
#if UNITY_EDITOR
		public Tilemap[] tilemaps = new Tilemap[0];
		public TileBase[] groundTiles = new TileBase[0];
		public TileBase[] hazardTiles = new TileBase[0];
		public bool update;
#endif
		AudioSource otherMusicSource;
		WorldPiece piecePlayerIsIn;
		Zone2D currAreaZone;
		float currAreaMusicVolume;
		float prevAreaMusicVolume;
		float timeAtAreaChange;
		bool isMusicFading;

		void Start ()
		{
// #if UNITY_EDITOR
// 			WorldMakerWindow.SetWorldActive (false);
// #endif
			SetPieces ();
			for (int i = 0; i < areaZones.Length; i ++)
			{
				Zone2D areaZone = areaZones[i];
				areaZones[i] = areaZone.Gen ();
			}
		}

		public void Init ()
		{
			isMusicFading = false;
			if (otherMusicSource == null)
			{
				otherMusicSource = musicSource.gameObject.AddComponent<AudioSource>();
				otherMusicSource.playOnAwake = false;
				otherMusicSource.loop = true;
			}
			else
				otherMusicSource.clip = null;
			for (int i = 0; i < areaZones.Length; i ++)
			{
				Zone2D areaZone = areaZones[i];
				if (areaZone != currAreaZone && areaZone.Contains_Polygon(Player.instance.trs.position))
				{
					currAreaZone = areaZone;
					musicSource.clip = areaMusics[i];
					currAreaMusicVolume = areaMusicsVolumes[i];
					musicSource.volume = currAreaMusicVolume;
					musicSource.Stop();
					musicSource.Play();
					return;
				}
			}
		}

#if UNITY_EDITOR
		void OnValidate ()
		{
			if (!update)
				return;
			update = false;
			worldObjects = FindObjectsOfType<ObjectInWorld>();
			for (int i = 0; i < worldObjects.Length; i ++)
			{
				ObjectInWorld worldObject = worldObjects[i];
				if (!worldObject.enabled || (worldObject.trs.parent != null && worldObject.trs.parent.GetComponent<ObjectInWorld>() != null && worldObject.keepParented))
				{
					worldObjects = worldObjects.RemoveAt(i);
					i --;
				}
			}
			Vector2 worldBoundsMin = WorldMap.Instance.unexploredTilemap.GetCellCenterWorld(WorldMap.instance.cellBounds.min) - (WorldMap.instance.unexploredTilemap.cellSize / 2);
			Vector2 worldBoundsMax = WorldMap.instance.unexploredTilemap.GetCellCenterWorld(WorldMap.instance.cellBounds.max) + (WorldMap.instance.unexploredTilemap.cellSize / 2);
			worldBoundsRect = Rect.MinMaxRect(worldBoundsMin.x, worldBoundsMin.y, worldBoundsMax.x, worldBoundsMax.y);
		}
#endif

		public virtual void SetPieces ()
		{
			pieces = new WorldPiece[maxPieceLocation.x + 1, maxPieceLocation.y + 1];
			piecesDict.Clear();
			piecePlayerIsIn = null;
			for (int i = 0; i < piecesParent.childCount; i ++)
			{
				WorldPiece piece = piecesParent.GetChild(i).GetComponent<WorldPiece>();
				piecesDict.Add(piece.location, piece);
				pieces[piece.location.x, piece.location.y] = piece;
				if (piecePlayerIsIn == null && piece.worldBoundsRect.Contains(Player.Instance.trs.position))
					piecePlayerIsIn = piece;
			}
		}

		public override void DoUpdate ()
		{
			if (WorldMap.isOpen)
				return;
			List<WorldPiece> prevActivePieces = new List<WorldPiece>(activePieces);
			activePieces.Clear();
			if (!piecePlayerIsIn.worldBoundsRect.Contains(Player.Instance.trs.position))
			{
				WorldPiece[] surroundingPieces = GetSurroundingPieces(piecePlayerIsIn);
				for (int i = 0; i < surroundingPieces.Length; i ++)
				{
					WorldPiece surroundingPiece = surroundingPieces[i];
					if (surroundingPiece.worldBoundsRect.Contains(Player.instance.trs.position))
					{
						piecePlayerIsIn = surroundingPiece;
						break;
					}
				}
			}
			LoadPiece (piecePlayerIsIn);
			WorldPiece[] surroundingPieces2 = GetSurroundingPieces(piecePlayerIsIn);
			for (int i = 0; i < surroundingPieces2.Length; i ++)
			{
				WorldPiece surroundingPiece = surroundingPieces2[i];
				Rect loadCheckRect = surroundingPiece.worldBoundsRect.Grow(Vector2.one * loadPiecesRange * 2);
				if (prevActivePieces.Contains(surroundingPiece))
					loadCheckRect = loadCheckRect.Grow(Vector2.one * extraUnloadPiecesRange * 2);
				if (GameCamera.Instance.viewRect.Intersects(loadCheckRect))
				{
					LoadPiece (surroundingPiece);
					for (int i2 = 0; i2 < surroundingPiece.piecesToLoadAndUnloadWithMe.Length; i2 ++)
						LoadPiece (surroundingPiece.piecesToLoadAndUnloadWithMe[i2]);
				}
			}
			for (int i = 0; i < piecePlayerIsIn.piecesToLoadAndUnloadWithMe.Length; i ++)
				LoadPiece (piecePlayerIsIn.piecesToLoadAndUnloadWithMe[i]);
			for (int i = 0; i < prevActivePieces.Count; i ++)
			{
				WorldPiece prevActivePiece = prevActivePieces[i];
				if (!activePieces.Contains(prevActivePiece))
					prevActivePiece.gameObject.SetActive(false);
			}
			if (isMusicFading)
			{
				if (Time.time - timeAtAreaChange >= musicFadeDur)
				{
					isMusicFading = false;
					otherMusicSource.volume = 0;
					AudioSource prevMusicSource = musicSource;
					musicSource = otherMusicSource;
					otherMusicSource = prevMusicSource;
					musicSource.volume = currAreaMusicVolume;
				}
				else
				{
					musicSource.volume = MathfExtensions.Remap(0, musicFadeDur, prevAreaMusicVolume, 0, Time.time - timeAtAreaChange);
					otherMusicSource.volume = MathfExtensions.Remap(0, musicFadeDur, 0, currAreaMusicVolume, Time.time - timeAtAreaChange);
				}
			}
			for (int i = 0; i < areaZones.Length; i ++)
			{
				Zone2D areaZone = areaZones[i];
				if (areaZone != currAreaZone && areaZone.Contains_Polygon(Player.instance.trs.position))
				{
					otherMusicSource.clip = areaMusics[i];
					otherMusicSource.volume = 0;
					otherMusicSource.Stop();
					otherMusicSource.Play();
					prevAreaMusicVolume = currAreaMusicVolume;
					currAreaMusicVolume = areaMusicsVolumes[i];
					isMusicFading = true;
					currAreaZone = areaZone;
					timeAtAreaChange = Time.time;
					return;
				}
			}
		}

		void LoadPiece (WorldPiece piece)
		{
			piece.gameObject.SetActive(true);
			activePieces.Add(piece);
		}

		public virtual WorldPiece[] GetSurroundingPieces (params WorldPiece[] innerPieces)
		{
			List<WorldPiece> output = new List<WorldPiece>();
			for (int i = 0; i < innerPieces.Length; i ++)
			{
				WorldPiece piece = innerPieces[i];
				bool hasPieceRight = piece.location.x < maxPieceLocation.x;
				bool hasPieceLeft = piece.location.x > 0;
				bool hasPieceUp = piece.location.y < maxPieceLocation.y;
				bool hasPieceDown = piece.location.y > 0;
				if (hasPieceUp)
				{
					output.Add(pieces[piece.location.x, piece.location.y + 1]);
					if (hasPieceRight)
						output.Add(pieces[piece.location.x + 1, piece.location.y + 1]);
					if (hasPieceLeft)
						output.Add(pieces[piece.location.x - 1, piece.location.y + 1]);
				}
				if (hasPieceDown)
				{
					output.Add(pieces[piece.location.x, piece.location.y - 1]);
					if (hasPieceRight)
						output.Add(pieces[piece.location.x + 1, piece.location.y - 1]);
					if (hasPieceLeft)
						output.Add(pieces[piece.location.x - 1, piece.location.y - 1]);
				}
				if (hasPieceRight)
					output.Add(pieces[piece.location.x + 1, piece.location.y]);
				if (hasPieceLeft)
					output.Add(pieces[piece.location.x - 1, piece.location.y]);
				for (int i2 = 0; i2 < innerPieces.Length; i2 ++)
				{
					WorldPiece piece2 = innerPieces[i2];
					output.Remove(piece2);
				}
			}
			return output.ToArray();
		}
	}
}

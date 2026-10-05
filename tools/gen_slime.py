#!/usr/bin/env python3
"""gen_slime.py - Slime Jump game logic as data, emitted as a Unity C# project.

WHY THIS EXISTS
    The original Slime Jump level data (Unity scenes) lives in Git LFS and is not
    available, so we re-express the *game* in a form that is easy to port:

      * CONFIG   - every gameplay tunable (values taken from the original prefabs)
      * LAYERS   - the physics layer table (same indices as the original project)
      * SPRITES  - pixel art authored as ASCII in gen_sprites.py (rendered with PIL)
      * *_CS     - the high level game logic, as C# source in triple quoted strings

    To port to another engine, read the *_CS strings (they are short, commented and
    deliberately engine-agnostic in structure) and the JSON this script emits.
    Everything engine specific is isolated behind a few seams:
      InputManager.cs  - input            Art.cs      - sprite creation
      Save.cs          - persistence      Layers.cs   - collision filtering
      LevelLoader.cs   - scene building   Player.Fracture() - death effect

WHAT IT DOES NOT DO
    It does not use Destructible2D, Unity UI, audio, achievements, the world map,
    cosmetics, items, or procedural/survival modes. Those are stand-ins or omitted;
    see the "PORT NOTE" comments in the C#.

USAGE
    python3 tools/gen_slime.py --level level.json --out /tmp/SlimeJumpCave
    python3 tools/gen_sprites.py               # ASCII art  -> PNGs / atlas / tileset / sprites.json
    python3 tools/gen_levels.py --unity DIR    # ASCII maps -> level.json / .tmj, verifies, writes Unity project
    python3 tools/gen_slime.py --level L.json --target prowl2d|stride2d --out DIR   # subset C# for the 2D engines
    python3 tools/run_2d.py --level L.json     # build + run + compare on both engines
    python3 tools/gen_cave_level.py            # older direct path: procedural cave straight to a project

NOTE: generated C# has been syntax-checked with tree-sitter (--check) but NOT
compiled against Unity in this environment.
"""
import argparse
import json
import os
import shutil
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

DEFAULT_UNITY_VERSION = "6000.6.0b5"
DEFAULT_UNITY_REVISION = "b5238eaafb35"  # only valid for DEFAULT_UNITY_VERSION

# --------------------------------------------------------------------------------------
# 1. CONFIG - gameplay tunables.
#    Player/lasso/world numbers are the original values (Player.prefab, Physics2D and
#    Time settings). Enemy numbers are based on the original Enemy prefab variants
#    ("Meelee": maxHp 33, moveSpeed 7, vision 9 / 90deg, patrolRange 5) but retuned for
#    worms and bats. Cave-specific additions are marked (new).
# --------------------------------------------------------------------------------------
CONFIG = {
    "world": {
        "gravity": -25.0,          # Physics2D gravity y
        "timeScale": 0.7,          # TimeManager m_TimeScale
        "fixedDeltaTime": 0.01,    # inferred from TimeManager (100 Hz)
        "cameraSize": 11.0,        # (new) orthographic half-height
        "killPlaneMargin": 12.0,   # (new) die this far below the level
    },
    "player": {
        "maxHp": 1,
        "moveSpeed": 17.5,
        "jumpSpeed": 17.5,
        "climbSpeed": 17.5,
        "climbFallSpeed": 2.0,
        "linearDamping": 1.0,
        "mass": 0.57,
        "respawnDelay": 0.5,
        "explodeSpeed": 20.0,
        "minCamMoveDistIfLassoHas0Slack": 0.05,
        "colliderW": 0.9,          # (new) no prefab-independent source for these
        "colliderH": 0.75,
        "fragmentCount": 14,       # (new) stands in for Destructible2D fracture
        "shootCooldown": 0.35,     # (new) simple blaster
        "bulletSpeed": 30.0,
        "bulletDamage": 1.0,
        "bulletLifetime": 1.2,
    },
    "lasso": {
        "maxLength": 7.0,
        "shootSpeed": 20.0,
        "swingSpeed": 5000.0,
        "changeLengthSpeed": 7.0,
        "startCollected": True,
    },
    "crumbly": {"dissolveTime": 1.0},      # DissolveOnHit.dissolveTime (Crumbly Wall.prefab)
    "arrow": {                             # Arrow.prefab + Arrow Shooter.prefab (the shoot animation is 1s long)
        "speed": 9.0, "lifetime": 10.0, "damage": 1.0, "cooldown": 1.0,
    },
    "worm": {
        "hp": 3.0, "isFlying": False, "moveSpeed": 3.5,
        "visionRange": 9.0, "visionAngle": 90.0,
        "patrolRange": 5.0, "patrolStopDist": 0.3,
        "minPatrolDestinationAngleDifference": 90.0,
        "patrolStopTimeMin": 0.25, "patrolStopTimeMax": 0.75,
        "lookToHurtDirectionDuration": 0.5,
        "attackDistMin": 3.0, "attackDistMax": 12.0,
        "chaseStopDistMin": 3.0, "chaseStopDistMax": 6.0,
        "shootInterval": 1.6, "bulletSpeed": 10.0, "bulletDamage": 1.0,
        "bulletLifetime": 3.0, "bulletSprite": "bullet_red",
        "contactDamage": 1.0, "activeRange": 45.0,
        "colliderW": 1.1, "colliderH": 0.6, "gravityScale": 1.0, "mass": 1.0,
        "sprite": "worm",
    },
    "bat": {
        "hp": 2.0, "isFlying": True, "moveSpeed": 7.5,
        "visionRange": 10.0, "visionAngle": 90.0,
        "patrolRange": 4.0, "patrolStopDist": 0.5,
        "minPatrolDestinationAngleDifference": 90.0,
        "patrolStopTimeMin": 0.25, "patrolStopTimeMax": 0.75,
        "lookToHurtDirectionDuration": 0.5,
        "attackDistMin": 0.0, "attackDistMax": 0.0,
        "chaseStopDistMin": 0.0, "chaseStopDistMax": 0.0,
        "shootInterval": 0.0, "bulletSpeed": 0.0, "bulletDamage": 0.0,
        "bulletLifetime": 0.0, "bulletSprite": "bullet_red",
        "contactDamage": 1.0, "activeRange": 45.0,
        "colliderW": 1.0, "colliderH": 0.6, "gravityScale": 0.0, "mass": 0.5,
        "sprite": "bat",
    },
}

# --------------------------------------------------------------------------------------
# 2. LAYERS - same indices/names as the original TagManager (only the ones we use).
# --------------------------------------------------------------------------------------
LAYERS = {
    "Spikes Collider": 3, "Player": 6, "Wall": 7, "Gem": 8, "Arrow": 9, "Hazard": 10,
    "Climbable": 15, "Player Sensor": 19, "Dead Player": 20, "Player Bullet": 25,
    "Enemy": 26,
}


# Which layer pairs do NOT collide. Everything not listed collides. Used by the Unity loader
# (Physics2D.IgnoreLayerCollision) and by the Prowl2D/Stride2D targets (SimCore2D.SetLayerMatrix).
LAYER_IGNORES = [
    ("Player Bullet", ["Player", "Player Bullet", "Gem", "Hazard", "Arrow", "Player Sensor", "Dead Player"]),
    ("Arrow", ["Enemy", "Arrow", "Gem", "Hazard", "Player Sensor", "Dead Player"]),
    ("Enemy", ["Enemy", "Gem", "Hazard", "Player Sensor", "Dead Player"]),
    ("Player Sensor", ["Gem", "Hazard", "Player"]),
]
DEAD_PLAYER_COLLIDES_WITH = ["Wall", "Climbable"]     # death fragments ignore every other layer


def ignored_pairs():
    """Sorted list of (layerA, layerB) index pairs, a < b, that must not collide."""
    pairs = set()
    for a, others in LAYER_IGNORES:
        for b in others:
            pairs.add(tuple(sorted((LAYERS[a], LAYERS[b]))))
    keep = {LAYERS[n] for n in DEAD_PLAYER_COLLIDES_WITH}
    dead = LAYERS["Dead Player"]
    for i in range(32):
        if i not in keep:
            pairs.add(tuple(sorted((dead, i))))
    return sorted(pairs)


def jump_capabilities(config=None):
    """Replicates the Player.Awake jump estimate. Returns (apex_height, air_time,
    flat_ground_jump_distance). Level generators use it so levels stay passable if
    the tunables change."""
    c = config or CONFIG
    g, dt = c["world"]["gravity"], c["world"]["fixedDeltaTime"]
    d, v = c["player"]["linearDamping"], c["player"]["jumpSpeed"]
    y = t = 0.0
    while v > 0:
        v += g * dt
        v *= 1 - d * dt
        y += v * dt
        t += dt
    apex, fall, v, yy = y, 0.0, 0.0, y
    while yy > 0:
        v += g * dt
        v *= 1 - d * dt
        yy += v * dt
        fall += dt
    air = t + fall
    return apex, air, air * c["player"]["moveSpeed"]


# --------------------------------------------------------------------------------------
# 3. SPRITES - authored as ASCII art in gen_sprites.py (one char = one pixel, shared palette)
#    and rendered with PIL. This module only needs the resulting sprites.json structure:
#    {"sprites": [{name, ppu, pivotX, pivotY, palette: ["g#7be07b", ...], rows: [...]}]}
# --------------------------------------------------------------------------------------
def build_sprites():
    """Built-in sprite sheet (from gen_sprites.py's ASCII art)."""
    import gen_sprites
    return gen_sprites.default_sprite_sheet()


# ======================================================================================
# 4. C# SOURCES
#    Raw triple-quoted strings. Files are written verbatim to Assets/Scripts/.
# ======================================================================================

CORE_CS = r'''using System.Collections.Generic;
using UnityEngine;

namespace SlimeJump
{
	// Anything that can take damage (original: IDestructable).
	public interface IDestructable
	{
		float Hp { get; }
		void TakeDamage (float amount, Vector2 direction);
		void Death ();
	}

	// Anything that must return to its initial state when the player respawns.
	// PORT NOTE: the original hard-coded one big reset loop inside Player.Respawn;
	// this registry (GameManager.resettables) is the same behaviour, decoupled.
	public interface IResettable
	{
		void ResetState ();
	}

	// Original: UpdateWhileEnabled. Objects register themselves while enabled and
	// GameManager.Update calls DoUpdate on all of them in one place. This gives a
	// single explicit update order, which matters for the player/lasso/camera
	// interplay. In another engine this is simply "the game loop's entity list".
	public abstract class UpdateWhileEnabled : MonoBehaviour
	{
		public static readonly List<UpdateWhileEnabled> updatables = new List<UpdateWhileEnabled>();

		public virtual void OnEnable ()
		{
			updatables.Add(this);
		}

		public virtual void OnDisable ()
		{
			updatables.Remove(this);
		}

		public abstract void DoUpdate ();
	}

	public static class Ext
	{
		public static Vector2 SetX (this Vector2 v, float x) { v.x = x; return v; }
		public static Vector2 SetY (this Vector2 v, float y) { v.y = y; return v; }
		public static Vector3 SetZ (this Vector3 v, float z) { v.z = z; return v; }
		public static Color SetAlpha (this Color c, float a) { c.a = a; return c; }
		public static float FacingAngle (this Vector2 v) { return Mathf.Atan2(v.y, v.x) * Mathf.Rad2Deg; }
		// Unlike Mathf.Sign, returns 0 for 0.
		public static int Sign (float f) { return f > 0 ? 1 : (f < 0 ? -1 : 0); }
	}
}
'''

DATA_CS = r'''using System;
using UnityEngine;

namespace SlimeJump
{
	// ---- game_config.json ----
	[Serializable] public class WorldConfig { public float gravity, timeScale, fixedDeltaTime, cameraSize, killPlaneMargin; }

	[Serializable]
	public class PlayerConfig
	{
		public int maxHp;
		public float moveSpeed, jumpSpeed, climbSpeed, climbFallSpeed, linearDamping, mass;
		public float respawnDelay, explodeSpeed, minCamMoveDistIfLassoHas0Slack;
		public float colliderW, colliderH;
		public int fragmentCount;
		public float shootCooldown, bulletSpeed, bulletDamage, bulletLifetime;
	}

	[Serializable] public class CrumblyConfig { public float dissolveTime; }
	[Serializable] public class ArrowConfig { public float speed, lifetime, damage, cooldown; }
	[Serializable] public class LassoConfig { public float maxLength, shootSpeed, swingSpeed, changeLengthSpeed; public bool startCollected; }

	[Serializable]
	public class EnemyConfig
	{
		public float hp; public bool isFlying; public float moveSpeed;
		public float visionRange, visionAngle, patrolRange, patrolStopDist, minPatrolDestinationAngleDifference;
		public float patrolStopTimeMin, patrolStopTimeMax, lookToHurtDirectionDuration;
		public float attackDistMin, attackDistMax, chaseStopDistMin, chaseStopDistMax;
		public float shootInterval, bulletSpeed, bulletDamage, bulletLifetime; public string bulletSprite;
		public float contactDamage, activeRange, colliderW, colliderH, gravityScale, mass;
		public string sprite;
	}

	[Serializable]
	public class Config
	{
		public WorldConfig world; public PlayerConfig player; public LassoConfig lasso;
		public EnemyConfig worm; public EnemyConfig bat;
		public CrumblyConfig crumbly; public ArrowConfig arrow;
		static Config cached;

		public static Config Cfg
		{
			get
			{
				if (cached == null)
					cached = JsonUtility.FromJson<Config>(Resources.Load<TextAsset>("game_config").text);
				return cached;
			}
		}

		public EnemyConfig Enemy (string type)
		{
			return type == "bat" ? bat : worm;
		}
	}

	// ---- level.json (engine-agnostic: axis aligned rects + points, y up) ----
	[Serializable] public class RectDef { public float x, y, w, h; }   // x,y = min corner
	[Serializable] public class PointDef { public string name; public float x, y; }
	[Serializable] public class EnemyDef { public string type; public float x, y; }  // x,y = body centre
	[Serializable] public class ShooterDef { public float x, y, dx, dy; }             // tile centre + firing direction

	[Serializable]
	public class LevelData
	{
		public string name;
		public string background;      // "#rrggbb"
		public float[] bounds;         // xmin, ymin, xmax, ymax
		public float[] spawn;          // player centre
		public RectDef[] walls;        // solid, layer Wall
		public RectDef[] climbables;   // solid + climbable, layer Climbable
		public RectDef[] spikes;       // kill on touch
		public RectDef[] crumbly;      // solid tiles that fade 1s after the player first touches them
		public ShooterDef[] shooters;  // arrow shooters (the tile is also in walls)
		public PointDef[] savepoints;  // x,y = base of the checkpoint
		public PointDef[] gems;
		public EnemyDef[] enemies;
		public PointDef goal;
	}
}
'''

SAVE_CS = r'''using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;

namespace SlimeJump
{
	// Original: SaveAndLoadManager (BinaryFormatter + typed dictionaries).
	// Replaced by a tiny string->string store persisted as JSON. Keys keep the
	// original convention, e.g. "Position Cave", "Collected Cave Gem 3".
	public static class Save
	{
		[Serializable] class SaveFile { public List<string> keys = new List<string>(); public List<string> values = new List<string>(); }

		static readonly Dictionary<string, string> data = new Dictionary<string, string>();
		static bool loaded;

		static string PathOnDisk { get { return Path.Combine(Application.persistentDataPath, "slime_cave_save.json"); } }

		static void EnsureLoaded ()
		{
			if (loaded) return;
			loaded = true;
			try
			{
				if (!File.Exists(PathOnDisk)) return;
				SaveFile f = JsonUtility.FromJson<SaveFile>(File.ReadAllText(PathOnDisk));
				for (int i = 0; i < f.keys.Count; i ++) data[f.keys[i]] = f.values[i];
			}
			catch (Exception e) { Debug.LogWarning("Save load failed: " + e.Message); }
		}

		public static void Flush ()
		{
			EnsureLoaded ();
			SaveFile f = new SaveFile();
			foreach (KeyValuePair<string, string> kv in data) { f.keys.Add(kv.Key); f.values.Add(kv.Value); }
			try { File.WriteAllText(PathOnDisk, JsonUtility.ToJson(f)); }
			catch (Exception e) { Debug.LogWarning("Save write failed: " + e.Message); }
		}

		public static void Clear ()
		{
			EnsureLoaded ();
			data.Clear();
			Flush ();
		}

		static string Get (string key) { EnsureLoaded (); string v; return data.TryGetValue(key, out v) ? v : null; }
		static void Set (string key, string v) { EnsureLoaded (); data[key] = v; }

		public static bool GetBool (string key, bool def) { string v = Get(key); return v == null ? def : v == "1"; }
		public static void SetBool (string key, bool v) { Set(key, v ? "1" : "0"); }
		public static int GetInt (string key, int def) { string v = Get(key); return v == null ? def : int.Parse(v, CultureInfo.InvariantCulture); }
		public static void SetInt (string key, int v) { Set(key, v.ToString(CultureInfo.InvariantCulture)); }
		public static float GetFloat (string key, float def) { string v = Get(key); return v == null ? def : float.Parse(v, CultureInfo.InvariantCulture); }
		public static void SetFloat (string key, float v) { Set(key, v.ToString("R", CultureInfo.InvariantCulture)); }

		public static Vector2 GetVector2 (string key, Vector2 def)
		{
			string v = Get(key);
			if (v == null) return def;
			string[] p = v.Split(',');
			return new Vector2(float.Parse(p[0], CultureInfo.InvariantCulture), float.Parse(p[1], CultureInfo.InvariantCulture));
		}

		public static void SetVector2 (string key, Vector2 v)
		{
			Set(key, v.x.ToString("R", CultureInfo.InvariantCulture) + "," + v.y.ToString("R", CultureInfo.InvariantCulture));
		}
	}
}
'''

INPUT_CS = r'''using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace SlimeJump
{
	// Original: InputManager. Bindings are the original keyboard+mouse ones:
	//   A/D or arrows = move, W/Up = jump (hold to climb), S/Down + W/Up = lasso length
	//   Left click = attack, Right click = lasso.
	// Works with either Unity input backend. This whole file is the input seam.
	public enum Btn { Left, Right, Up, Down, Pause, ResetProgress }

	public static class InputManager
	{
		public static float MoveInput
		{
			get
			{
				float o = 0;
				if (Held(Btn.Left)) o --;
				if (Held(Btn.Right)) o ++;
				return o;
			}
		}

		public static bool JumpInput { get { return Held(Btn.Up); } }

		// Original: W = -1 (reel in), S = +1 (let out).
		public static int ChangeLassoLengthInput
		{
			get
			{
				if (Held(Btn.Up)) return -1;
				if (Held(Btn.Down)) return 1;
				return 0;
			}
		}

#if ENABLE_INPUT_SYSTEM
		public static bool Held (Btn b)
		{
			Keyboard k = Keyboard.current;
			if (k == null) return false;
			switch (b)
			{
				case Btn.Left: return k.aKey.isPressed || k.leftArrowKey.isPressed;
				case Btn.Right: return k.dKey.isPressed || k.rightArrowKey.isPressed;
				case Btn.Up: return k.wKey.isPressed || k.upArrowKey.isPressed;
				case Btn.Down: return k.sKey.isPressed || k.downArrowKey.isPressed;
			}
			return false;
		}

		public static bool Pressed (Btn b)
		{
			Keyboard k = Keyboard.current;
			if (k == null) return false;
			if (b == Btn.Pause) return k.escapeKey.wasPressedThisFrame;
			if (b == Btn.ResetProgress) return k.deleteKey.wasPressedThisFrame;
			return false;
		}

		public static bool AttackInput { get { return Mouse.current != null && Mouse.current.leftButton.isPressed; } }
		public static bool LassoInput { get { return Mouse.current != null && Mouse.current.rightButton.isPressed; } }
		public static Vector2 AimScreenPosition { get { return Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero; } }
#elif ENABLE_LEGACY_INPUT_MANAGER
		public static bool Held (Btn b)
		{
			switch (b)
			{
				case Btn.Left: return Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow);
				case Btn.Right: return Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow);
				case Btn.Up: return Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow);
				case Btn.Down: return Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow);
			}
			return false;
		}

		public static bool Pressed (Btn b)
		{
			if (b == Btn.Pause) return Input.GetKeyDown(KeyCode.Escape);
			if (b == Btn.ResetProgress) return Input.GetKeyDown(KeyCode.Delete);
			return false;
		}

		public static bool AttackInput { get { return Input.GetMouseButton(0); } }
		public static bool LassoInput { get { return Input.GetMouseButton(1); } }
		public static Vector2 AimScreenPosition { get { return Input.mousePosition; } }
#else
		public static bool Held (Btn b) { return false; }
		public static bool Pressed (Btn b) { return false; }
		public static bool AttackInput { get { return false; } }
		public static bool LassoInput { get { return false; } }
		public static Vector2 AimScreenPosition { get { return Vector2.zero; } }
#endif
	}
}
'''

ART_CS = r'''using System;
using System.Collections.Generic;
using UnityEngine;

namespace SlimeJump
{
	[Serializable] public class SpriteDef { public string name; public int ppu; public float pivotX, pivotY; public string[] palette; public string[] rows; }
	[Serializable] public class SpriteSheet { public SpriteDef[] sprites; }

	// Builds every sprite at runtime from sprites.json (ASCII rows + palette).
	// PORT NOTE: replace with your engine's texture loader; the data is trivial.
	public static class Art
	{
		static Dictionary<string, Sprite> cache;
		static Material spriteMaterial;
		static PhysicsMaterial2D noFriction;

		public static Sprite Get (string name)
		{
			if (cache == null) Load ();
			Sprite s;
			if (!cache.TryGetValue(name, out s)) { Debug.LogError("Missing sprite " + name); return null; }
			return s;
		}

		static void Load ()
		{
			cache = new Dictionary<string, Sprite>();
			SpriteSheet sheet = JsonUtility.FromJson<SpriteSheet>(Resources.Load<TextAsset>("sprites").text);
			for (int i = 0; i < sheet.sprites.Length; i ++)
				cache[sheet.sprites[i].name] = Build(sheet.sprites[i]);
		}

		static Sprite Build (SpriteDef d)
		{
			Dictionary<char, Color32> colors = new Dictionary<char, Color32>();
			for (int i = 0; i < d.palette.Length; i ++)
			{
				Color c;
				ColorUtility.TryParseHtmlString(d.palette[i].Substring(1), out c);
				colors[d.palette[i][0]] = c;
			}
			int h = d.rows.Length, w = d.rows[0].Length;
			Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
			tex.filterMode = FilterMode.Point;
			tex.wrapMode = TextureWrapMode.Repeat;
			Color32[] px = new Color32[w * h];
			for (int row = 0; row < h; row ++)
				for (int x = 0; x < w; x ++)
				{
					Color32 col;
					px[(h - 1 - row) * w + x] = colors.TryGetValue(d.rows[row][x], out col) ? col : new Color32(0, 0, 0, 0);
				}
			tex.SetPixels32(px);
			tex.Apply();
			return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(d.pivotX, d.pivotY), d.ppu, 0, SpriteMeshType.FullRect);
		}

		// Used for the lasso LineRenderer. Falls back across pipelines.
		public static Material SpriteMaterial ()
		{
			if (spriteMaterial == null)
			{
				Shader s = Shader.Find("Sprites/Default");
				if (s == null) s = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
				if (s == null) s = Shader.Find("Unlit/Color");
				spriteMaterial = new Material(s);
			}
			return spriteMaterial;
		}

		public static PhysicsMaterial2D NoFriction ()
		{
			if (noFriction == null)
			{
				noFriction = new PhysicsMaterial2D("NoFriction");
				noFriction.friction = 0;
				noFriction.bounciness = 0;
			}
			return noFriction;
		}
	}
}
'''

EDITOR_CS = r'''using UnityEditor;
using UnityEngine;

namespace SlimeJump
{
	// Cosmetic: registers the layer names in the TagManager so they show up in the
	// inspector. The game logic only uses layer INDICES (see Layers.cs), so it also
	// works if this never runs.
	public static class EditorSetup
	{
		[InitializeOnLoadMethod]
		static void EnsureLayerNames ()
		{
			Object[] assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
			if (assets == null || assets.Length == 0) return;
			SerializedObject tagManager = new SerializedObject(assets[0]);
			SerializedProperty layers = tagManager.FindProperty("layers");
			bool dirty = false;
			foreach (System.Collections.Generic.KeyValuePair<int, string> kv in Layers.Names)
			{
				SerializedProperty p = layers.GetArrayElementAtIndex(kv.Key);
				if (p.stringValue != kv.Value) { p.stringValue = kv.Value; dirty = true; }
			}
			if (dirty) { tagManager.ApplyModifiedProperties(); AssetDatabase.SaveAssets(); }
		}
	}
}
'''


def unity_layer_ignores_cs():
    lines = ["\t\t// GENERATED from gen_slime.LAYER_IGNORES. Everything not listed collides.",
             "\t\tstatic void SetupLayerCollisions ()", "\t\t{"]
    for a, b in ignored_pairs():
        lines.append("\t\t\tPhysics2D.IgnoreLayerCollision(%d, %d, true);" % (a, b))
    lines.append("\t\t}")
    return "\n".join(lines)


def layers_cs():
    ident = lambda n: n.replace(" ", "")
    lines = ["using System.Collections.Generic;", "using UnityEngine;", "",
             "namespace SlimeJump", "{",
             "\t// GENERATED from gen_slime.LAYERS. Same indices as the original project.",
             "\tpublic static class Layers", "\t{"]
    for n, i in sorted(LAYERS.items(), key=lambda kv: kv[1]):
        lines.append("\t\tpublic const int %s = %d;" % (ident(n), i))
    lines.append("\t\tpublic static readonly Dictionary<int, string> Names = new Dictionary<int, string>")
    lines.append("\t\t{")
    for n, i in sorted(LAYERS.items(), key=lambda kv: kv[1]):
        lines.append('\t\t\t{ %d, "%s" },' % (i, n))
    lines.append("\t\t};")
    lines += ["", "\t\tpublic static int Mask (params int[] layers)", "\t\t{",
              "\t\t\tint m = 0;", "\t\t\tfor (int i = 0; i < layers.Length; i ++) m |= 1 << layers[i];",
              "\t\t\treturn m;", "\t\t}", "\t}", "}", ""]
    return "\n".join(lines)


# --------------------------------------------------------------------------------------
# Gameplay logic. These are the files to read when porting.
# --------------------------------------------------------------------------------------

PLAYER_CS = r'''using System.Collections.Generic;
using UnityEngine;

namespace SlimeJump
{
	// Ported from the original Player.cs. Kept: movement, variable-height jump,
	// wall climbing, lasso swing coupling, death/respawn, kill-plane.
	// Dropped (PORT NOTE): items/weapon prefabs, blaster launch, vortex, no-gravity
	// zones, mobile controls, achievements, cosmetics, sounds, squash/stretch,
	// Destructible2D fracture (see Fracture()).
	public class Player : UpdateWhileEnabled, IDestructable
	{
		public static Player instance;
		public static uint respawnedOnFrame;

		public Transform trs;
		public Transform graphicsTrs;
		public Rigidbody2D rigid;
		public SpriteRenderer spriteRenderer;
		public Collider2D col;
		public Collider2D climbableSensor;
		public bool invulnerable;
		[HideInInspector] public bool isGrounded;
		[HideInInspector] public bool isJumping;
		[HideInInspector] public bool lassoHasSlack;
		[HideInInspector] public float swingAngularVelocity;
		[HideInInspector] public float swingAngle;
		[HideInInspector] public Vector2 lastMovement;
		[HideInInspector] public float respawnTimer;
		[HideInInspector] public int whatICollideWith;
		public float Hp { get { return hp; } }

		PlayerConfig cfg;
		float hp;
		float maxJumpDuration;
		float timeSinceJump;
		bool isClimbing;
		bool climbedSinceJumped;
		bool isHittingWall;
		float jumpVel;
		float moveInput;
		bool jumpInput;
		float xSize = 1;
		Vector2 prevPosition;
		float shootTimer;
		readonly List<GameObject> fragments = new List<GameObject>();

		public static Vector2 SavedPosition
		{
			get { return Save.GetVector2("Position " + GameManager.levelName, GameManager.SpawnPosition); }
			set { Save.SetVector2("Position " + GameManager.levelName, value); }
		}

		public void Init (PlayerConfig cfg)
		{
			instance = this;
			this.cfg = cfg;
			// What the player treats as a "wall" for the side check. Hazards/enemies are
			// excluded on purpose so chasing something from behind doesn't stop short.
			whatICollideWith = Layers.Mask(Layers.Wall, Layers.Climbable);
			// How long a full-height jump lasts (same estimate as the original Awake).
			float yVelocity = cfg.jumpSpeed;
			maxJumpDuration = 0;
			while (yVelocity > 0)
			{
				yVelocity += Physics2D.gravity.y * Time.fixedDeltaTime;
				yVelocity *= 1f - rigid.linearDamping * Time.fixedDeltaTime;
				maxJumpDuration += Time.fixedDeltaTime;
			}
			hp = cfg.maxHp;
		}

		public void Spawn ()
		{
			Respawn ();
		}

		public override void DoUpdate ()
		{
			if (GameManager.paused)
				return;
			GameManager.Timer += Time.deltaTime;
			if (respawnTimer > 0)
			{
				respawnTimer -= Time.deltaTime;
				if (respawnTimer <= 0)
					Respawn ();
				else
					return;
			}
			if (trs.position.y < GameManager.level.bounds[1] - Config.Cfg.world.killPlaneMargin)
			{
				Death ();
				return;
			}
			HandleMoving ();
			HandleJumping ();
			HandleClimbing ();
			HandleAttacking ();
			rigid.gravityScale = isClimbing ? 0 : 1;
			// While the rope is taut the camera stops following until the player moves
			// far enough, so the swing doesn't feel like it drags the screen around.
			if (!lassoHasSlack && Lasso.instance.isAttached && Lasso.instance.changeLengthInput == 0)
			{
				GameCamera.instance.followPlayer = false;
				if (((Vector2) (GameCamera.instance.trs.position - trs.position)).sqrMagnitude >= cfg.minCamMoveDistIfLassoHas0Slack * cfg.minCamMoveDistIfLassoHas0Slack)
				{
					GameCamera.instance.followPlayer = true;
					GameCamera.instance.HandlePosition ();
				}
			}
			else
				GameCamera.instance.followPlayer = true;
			// Nominal jump velocity decays like the rigidbody's does; StopJump subtracts it.
			jumpVel += Physics2D.gravity.y * Time.deltaTime;
			jumpVel *= 1f - rigid.linearDamping * Time.deltaTime;
			spriteRenderer.flipX = xSize < 0;
			lastMovement = (Vector2) trs.position - prevPosition;
			prevPosition = trs.position;
		}

		void HandleMoving ()
		{
			moveInput = InputManager.MoveInput;
			if (moveInput != 0)
			{
				// Probe for a wall just beyond the collider on the side we're moving to.
				Bounds b = col.bounds;
				float x = b.center.x + (b.extents.x + Physics2D.defaultContactOffset * 2) * Mathf.Sign(moveInput);
				isHittingWall = Physics2D.Linecast(new Vector2(x, b.max.y), new Vector2(x, b.min.y), whatICollideWith).collider != null;
				xSize = Mathf.Sign(moveInput);
			}
			else
				isHittingWall = false;
			Move (isHittingWall ? 0 : moveInput);
		}

		public void Move (float speed)
		{
			lassoHasSlack = true;
			if (Lasso.instance.isAttached)
			{
				Vector2 toHitPoint = Lasso.instance.hookTrs.position - trs.position;
				float lengthRemaining = Lasso.instance.currentLength - toHitPoint.magnitude;
				if (lengthRemaining <= 0)
				{
					// Rope is taut: pull back onto the circle and run a pendulum. The hook
					// transform is rotated and the player (its child) rides along.
					lassoHasSlack = false;
					trs.position += (Vector3) toHitPoint.normalized * -lengthRemaining;
					float swingAngularAcceleration = Lasso.instance.swingSpeed * Mathf.Cos(swingAngle * Mathf.Deg2Rad);
					swingAngularVelocity += swingAngularAcceleration * Time.deltaTime;
					swingAngularVelocity *= 1f - rigid.linearDamping * Time.deltaTime;
					swingAngle += swingAngularVelocity / Lasso.instance.currentLength * Time.deltaTime;
					Lasso.instance.hookTrs.eulerAngles += Vector3.forward * swingAngularVelocity / Lasso.instance.currentLength * Time.deltaTime;
					trs.eulerAngles = Vector3.zero;
				}
			}
			rigid.linearVelocity = rigid.linearVelocity.SetX(speed * cfg.moveSpeed);
		}

		void HandleJumping ()
		{
			jumpInput = InputManager.JumpInput;
			if (isClimbing)
				return;
			if (jumpInput && isGrounded && !isJumping)
				StartJump ();
			else if (isJumping)
			{
				timeSinceJump += Time.deltaTime;
				// Releasing jump early cuts the jump short (variable height).
				if (!jumpInput && timeSinceJump < maxJumpDuration)
					StopJump ();
				else if (rigid.linearVelocity.y <= 0)
				{
					isJumping = false;
					climbedSinceJumped = false;
				}
			}
		}

		void HandleClimbing ()
		{
			bool wasClimbing = isClimbing;
			isClimbing = climbableSensor.IsTouchingLayers(Layers.Mask(Layers.Climbable));
			if (isClimbing)
			{
				climbedSinceJumped = true;
				if (jumpInput)
				{
					isJumping = true;
					rigid.linearVelocity = rigid.linearVelocity.SetY(cfg.climbSpeed);
					timeSinceJump = 0;
				}
				else
					rigid.linearVelocity = rigid.linearVelocity.SetY(-cfg.climbFallSpeed);
			}
			else if (wasClimbing && jumpInput)
				rigid.linearVelocity = rigid.linearVelocity.SetY(cfg.climbSpeed);   // pop over the lip
		}

		public void StartJump ()
		{
			isJumping = true;
			rigid.linearVelocity += Vector2.up * cfg.jumpSpeed;
			jumpVel = cfg.jumpSpeed;
			timeSinceJump = 0;
		}

		public void StopJump ()
		{
			if (climbedSinceJumped)
				rigid.linearVelocity = rigid.linearVelocity.SetY(0);
			else
				rigid.linearVelocity -= Vector2.up * jumpVel;
			isJumping = false;
			climbedSinceJumped = false;
			jumpVel = 0;
		}

		// (new) Minimal blaster so the cave's worms and bats can be killed. The original
		// has a weapon/animation system plus blaster launch; this is the bare idea.
		void HandleAttacking ()
		{
			shootTimer -= Time.deltaTime;
			if (!InputManager.AttackInput || shootTimer > 0)
				return;
			Vector3 world = GameCamera.instance.cam.ScreenToWorldPoint(InputManager.AimScreenPosition);
			Vector2 dir = (Vector2) world - (Vector2) trs.position;
			if (dir == Vector2.zero)
				dir = Vector2.right;
			shootTimer = cfg.shootCooldown;
			Bullet.Spawn(trs.position, dir, cfg.bulletSpeed, cfg.bulletDamage, cfg.bulletLifetime, Layers.PlayerBullet, "bullet_green");
		}

		public void TakeDamage (float amount, Vector2 direction)
		{
			if (invulnerable || respawnTimer > 0)
				return;
			hp = Mathf.Clamp(hp - amount, 0, cfg.maxHp);
			if (hp == 0)
				Death ();
		}

		public void Death ()
		{
			if (respawnTimer > 0)
				return;
			respawnTimer = cfg.respawnDelay;
			GameCamera.instance.followPlayer = false;
			spriteRenderer.enabled = false;
			if (Lasso.instance.lineRenderer.enabled)
				Lasso.instance.Release ();
			Lasso.instance.enabled = false;
			Fracture ();
			trs.SetParent(null);
			isGrounded = false;
			StopJump ();
			rigid.linearVelocity = Vector2.zero;
			rigid.simulated = false;
			GameManager.DeathCount ++;
			Save.Flush ();
		}

		// PORT NOTE: the original uses Destructible2D's D2dFracturer to shatter the sprite
		// into physics chunks. Stand-in: a burst of small square bodies.
		void Fracture ()
		{
			for (int i = 0; i < cfg.fragmentCount; i ++)
			{
				GameObject go = new GameObject("Fragment");
				go.layer = Layers.DeadPlayer;
				go.transform.position = trs.position + (Vector3) Random.insideUnitCircle * 0.3f;
				SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
				sr.sprite = Art.Get("fragment");
				sr.sortingOrder = 10;
				Rigidbody2D rb = go.AddComponent<Rigidbody2D>();
				rb.gravityScale = 1;
				BoxCollider2D bc = go.AddComponent<BoxCollider2D>();
				bc.size = Vector2.one * 0.25f;
				Vector2 dir = Random.insideUnitCircle.normalized;
				rb.linearVelocity = dir * cfg.explodeSpeed * Random.Range(0.4f, 1f);
				fragments.Add(go);
			}
		}

		void Respawn ()
		{
			hp = cfg.maxHp;
			trs.SetParent(null);
			trs.rotation = Quaternion.identity;
			trs.position = SavedPosition;
			for (int i = 0; i < fragments.Count; i ++)
				if (fragments[i] != null)
					Destroy(fragments[i]);
			fragments.Clear();
			lastMovement = Vector2.zero;
			prevPosition = trs.position;
			swingAngularVelocity = 0;
			Lasso.instance.enabled = true;
			spriteRenderer.enabled = true;
			rigid.simulated = true;
			rigid.linearVelocity = Vector2.zero;
			GameManager.instance.ResetWorld ();
			GameCamera.instance.followPlayer = true;
			Physics2D.SyncTransforms();
			GameCamera.instance.HandlePosition ();
			respawnedOnFrame = GameManager.framesSinceLevelLoaded;
		}

		public void ForceRespawn ()
		{
			respawnTimer = 0;
			Respawn ();
		}

		void OnCollisionEnter2D (Collision2D coll)
		{
			if (coll.contactCount == 0)
				return;
			ContactPoint2D contactPnt = coll.GetContact(0);
			// Ground = a contact below our centre whose normal points mostly up.
			if (contactPnt.normal.y > Vector2.one.normalized.x * .75f && contactPnt.point.y < col.bounds.center.y)
			{
				isGrounded = true;
				// Kill upward velocity picked up from slopes/steps when not jumping.
				if (!isJumping && rigid.linearVelocity.y > 0)
					rigid.linearVelocity = rigid.linearVelocity.SetY(-rigid.linearVelocity.y);
			}
		}

		void OnCollisionStay2D (Collision2D coll)
		{
			OnCollisionEnter2D (coll);
		}

		void OnCollisionExit2D (Collision2D coll)
		{
			if (isGrounded && !isJumping && rigid.linearVelocity.y > 0)
				rigid.linearVelocity = rigid.linearVelocity.SetY(-rigid.linearVelocity.y);
			isGrounded = false;
		}
	}
}
'''

LASSO_CS = r'''using UnityEngine;

namespace SlimeJump
{
	// Ported from the original Lasso.cs: right-click shoots a hook toward the cursor;
	// it travels until it hits a Wall/Climbable surface, then the player swings.
	// The swing itself lives in Player.Move (hook is rotated, player is its child).
	// Dropped: sounds, the line collider, the camera re-render hack, mobile slider.
	public class Lasso : UpdateWhileEnabled
	{
		public static Lasso instance;

		public Transform trs;                 // sits on the player
		public LineRenderer lineRenderer;     // local space: (0,0) is the player
		public Transform hookTrs;
		public Transform hookGraphicsTrs;
		public SpriteRenderer hookSpriteRenderer;
		[HideInInspector] public Transform hitTrs;
		[HideInInspector] public bool isAttached;
		[HideInInspector] public int changeLengthInput;
		[HideInInspector] public float currentLength;
		public float swingSpeed { get { return cfg.swingSpeed; } }

		LassoConfig cfg;
		bool previousLassoInput;
		Vector2 shootVector;
		Vector2 previousHitTrsPosition;
		int whatIHit;

		public void Init (LassoConfig cfg)
		{
			instance = this;
			this.cfg = cfg;
			whatIHit = Layers.Mask(Layers.Wall, Layers.Climbable);
			hookTrs.SetParent(null);
			hookTrs.gameObject.SetActive(false);
			enabled = cfg.startCollected;
		}

		public override void DoUpdate ()
		{
			bool lassoInput = InputManager.LassoInput;
			if (lassoInput && !previousLassoInput)
			{
				lineRenderer.SetPositions(new Vector3[] { Vector3.zero, Vector3.zero });
				lineRenderer.enabled = true;
				Vector3 aim = GameCamera.instance.cam.ScreenToWorldPoint(InputManager.AimScreenPosition);
				shootVector = (Vector2) aim - (Vector2) trs.position;
				if (shootVector == Vector2.zero)
					shootVector = Vector2.up;
				hookTrs.position = trs.position;
				hookTrs.up = shootVector;
				hookTrs.gameObject.SetActive(true);
			}
			else if (!lassoInput && previousLassoInput && lineRenderer.enabled)
				Release ();
			if (lineRenderer.enabled)
			{
				if (!isAttached)
				{
					// Flying: advance the tip, raycast for a surface along this step.
					Vector2 endPosition = lineRenderer.GetPosition(0);
					Vector2 newEndPosition = endPosition + shootVector.normalized * cfg.shootSpeed * Time.deltaTime;
					Vector2 toNewEndPosition = newEndPosition - endPosition;
					float lengthRemaining = Mathf.Min(cfg.maxLength - endPosition.magnitude, toNewEndPosition.magnitude);
					RaycastHit2D hit = Physics2D.Raycast((Vector2) trs.position + endPosition, toNewEndPosition, lengthRemaining, whatIHit);
					if (hit.collider != null)
					{
						Vector2 toHitPoint = hit.point - (Vector2) trs.position;
						lineRenderer.SetPosition(0, toHitPoint);
						currentLength = toHitPoint.magnitude;
						hookTrs.position = hit.point;
						hookTrs.up = toHitPoint;
						float toHitPointAngle = toHitPoint.FacingAngle() * Mathf.Deg2Rad;
						Rigidbody2D prb = Player.instance.rigid;
						Player.instance.swingAngularVelocity = (prb.linearVelocity.x * Mathf.Cos(toHitPointAngle) + prb.linearVelocity.y * Mathf.Sin(toHitPointAngle)) / currentLength;
						Player.instance.swingAngle = toHitPoint.FacingAngle();
						hitTrs = hit.collider.transform;
						previousHitTrsPosition = hitTrs.position;
						isAttached = true;
						Player.instance.trs.SetParent(hookTrs);
					}
					else
					{
						hookTrs.position = Player.instance.trs.position + (Vector3) newEndPosition;
						lineRenderer.SetPositions(new Vector3[] { newEndPosition, Vector2.zero });
						currentLength = newEndPosition.magnitude;
						if (lengthRemaining <= 0)
							Release ();
					}
				}
				else
				{
					// Attached: follow a moving anchor, allow reeling in/out.
					hookTrs.position += (Vector3) ((Vector2) hitTrs.position - previousHitTrsPosition);
					previousHitTrsPosition = hitTrs.position;
					Vector2 toHitPoint = hookTrs.position - trs.position;
					hookGraphicsTrs.up = toHitPoint;
					changeLengthInput = InputManager.ChangeLassoLengthInput;
					if (changeLengthInput != 0)
					{
						ContactFilter2D contactFilter = new ContactFilter2D();
						contactFilter.useLayerMask = true;
						contactFilter.layerMask = Player.instance.whatICollideWith;
						float changeLengthAmount = changeLengthInput * cfg.changeLengthSpeed * Time.deltaTime;
						// Reeling IN is blocked if the player would be pulled into a wall.
						if (changeLengthInput > 0 || Player.instance.col.Cast(toHitPoint, contactFilter, new RaycastHit2D[1], Mathf.Abs(changeLengthAmount) * 6) == 0)
							currentLength = Mathf.Clamp(currentLength + changeLengthAmount, 0, cfg.maxLength);
					}
					lineRenderer.SetPosition(0, toHitPoint);
					if (!hitTrs.gameObject.activeInHierarchy)
						Release ();
					GameCamera.instance.HandlePosition ();
				}
			}
			previousLassoInput = lassoInput;
		}

		public void Release ()
		{
			lineRenderer.enabled = false;
			hookTrs.gameObject.SetActive(false);
			if (isAttached)
			{
				Player.instance.trs.SetParent(null);
				hookGraphicsTrs.localEulerAngles = Vector3.zero;
				isAttached = false;
				// Keep the swing momentum.
				Player.instance.rigid.linearVelocity = Player.instance.lastMovement / Time.deltaTime;
			}
		}
	}
}
'''

ENEMY_CS = r'''using System.Collections.Generic;
using UnityEngine;

namespace SlimeJump
{
	// Ported from the original Enemy.cs: patrol -> (vision cone + line of sight) ->
	// chase -> attack, with hit-reaction, hp and a simple freeze when far away.
	// One class covers both enemy kinds through EnemyConfig:
	//   worm = ground walker that spits bullets; bat = flyer that dive-bombs by contact.
	// Dropped: weapon animation entries, blaster knockback, patrol zones, onDied event.
	public class Enemy : UpdateWhileEnabled, IDestructable, IResettable
	{
		public Transform trs;
		public Rigidbody2D rigid;
		public Collider2D col;
		public CircleCollider2D visionSensor;
		public SpriteRenderer spriteRenderer;
		[HideInInspector] public Vector2 initPosition;
		public float Hp { get { return hp; } }

		EnemyConfig cfg;
		float hp;
		bool chasePlayer;
		Vector2 currentPatrolDestination;
		float patrolStopTimeRemaining;
		bool previousAtCurrentPatrolDestination;
		Vector2 toPreviousPatrolDestination;
		float lookToHurtDirectionTimer;
		Vector2 facing = Vector2.right;
		float shootTimer;
		int whatBlocksVision;
		int whatBlocksMovement;
		bool initialized;
		readonly RaycastHit2D[] castBuffer = new RaycastHit2D[4];
		static readonly Vector2[] sightTargets = new Vector2[5];

		public void Init (EnemyConfig cfg, Vector2 position)
		{
			this.cfg = cfg;
			initPosition = position;
			whatBlocksVision = Layers.Mask(Layers.Wall, Layers.Climbable, Layers.Player);
			whatBlocksMovement = Layers.Mask(Layers.Wall, Layers.Climbable);
			visionSensor.radius = cfg.visionRange;
			initialized = true;
			GameManager.resettables.Add(this);
			ResetState ();
		}

		public override void OnEnable ()
		{
			base.OnEnable ();
		}

		public void ResetState ()
		{
			if (!initialized)
				return;
			if (!gameObject.activeSelf)
				gameObject.SetActive(true);
			hp = cfg.hp;
			chasePlayer = false;
			visionSensor.enabled = true;
			lookToHurtDirectionTimer = 0;
			shootTimer = cfg.shootInterval;
			trs.position = initPosition + Vector2.up * Physics2D.defaultContactOffset;
			rigid.simulated = true;
			rigid.linearVelocity = Vector2.zero;
			currentPatrolDestination = cfg.isFlying ? initPosition : initPosition + Vector2.down * (cfg.colliderH * 0.5f);
			previousAtCurrentPatrolDestination = false;
			toPreviousPatrolDestination = Vector2.zero;
			patrolStopTimeRemaining = 0;
		}

		public override void DoUpdate ()
		{
			// PORT NOTE: stands in for the original World piece streaming - far-away
			// enemies are simply not simulated.
			float sqrDist = ((Vector2) trs.position - (Vector2) Player.instance.trs.position).sqrMagnitude;
			bool near = sqrDist <= cfg.activeRange * cfg.activeRange;
			if (rigid.simulated != near)
				rigid.simulated = near;
			if (!near)
				return;
			HandleMoving ();
			if (chasePlayer)
				HandleAttacking ();
		}

		Vector2 Foot { get { return (Vector2) trs.position + Vector2.down * (cfg.colliderH * 0.5f); } }

		void HandleMoving ()
		{
			if (chasePlayer)
			{
				Vector2 toPlayer = Player.instance.trs.position - trs.position;
				float dist = toPlayer.magnitude;
				if (dist >= cfg.chaseStopDistMin && dist <= cfg.chaseStopDistMax)
					Move (Vector2.zero);
				else if (dist < cfg.chaseStopDistMin)
					Move (-toPlayer);       // too close: back off
				else
					Move (toPlayer);
				// Lost line of sight (checked against 5 points of the player's bounds) -> stop chasing.
				if (!CanSeePlayer(trs.position, Mathf.Infinity))
				{
					chasePlayer = false;
					visionSensor.enabled = true;
				}
				return;
			}
			if (lookToHurtDirectionTimer > 0)
			{
				lookToHurtDirectionTimer -= Time.deltaTime;
				Move (Vector2.zero);
				return;
			}
			Vector2 toCurrentPatrolDestination = cfg.isFlying ? currentPatrolDestination - (Vector2) trs.position : currentPatrolDestination - Foot;
			bool atDestination = toCurrentPatrolDestination.sqrMagnitude <= cfg.patrolStopDist * cfg.patrolStopDist;
			if (atDestination)
			{
				if (!previousAtCurrentPatrolDestination)
					patrolStopTimeRemaining = Random.Range(cfg.patrolStopTimeMin, cfg.patrolStopTimeMax);
				else
				{
					patrolStopTimeRemaining -= Time.deltaTime;
					if (patrolStopTimeRemaining <= 0)
						PickNewPatrolDestination (ref toCurrentPatrolDestination);
				}
				Move (Vector2.zero);
			}
			else
				Move (toCurrentPatrolDestination);
			previousAtCurrentPatrolDestination = atDestination;
		}

		// Pick a destination that is unobstructed and turns us around by at least
		// minPatrolDestinationAngleDifference. (The original loops forever; bounded here.)
		void PickNewPatrolDestination (ref Vector2 toDest)
		{
			for (int tries = 0; tries < 8; tries ++)
			{
				if (cfg.isFlying)
				{
					currentPatrolDestination = initPosition + Random.insideUnitCircle.normalized * cfg.patrolRange;
					toDest = currentPatrolDestination - (Vector2) trs.position;
				}
				else
				{
					currentPatrolDestination.x = initPosition.x + Random.Range(-1f, 1f) * cfg.patrolRange;
					toDest = currentPatrolDestination - Foot;
				}
				if ((toPreviousPatrolDestination == Vector2.zero || Vector2.Angle(toDest, toPreviousPatrolDestination) >= cfg.minPatrolDestinationAngleDifference) && !Blocked(toDest))
					break;
			}
			toPreviousPatrolDestination = toDest;
		}

		bool Blocked (Vector2 dir)
		{
			if (dir == Vector2.zero)
				return false;
			ContactFilter2D filter = new ContactFilter2D();
			filter.useLayerMask = true;
			filter.layerMask = whatBlocksMovement;
			filter.useTriggers = false;
			int n = rigid.Cast(dir, filter, castBuffer, dir.magnitude);
			for (int i = 0; i < n; i ++)
				if (castBuffer[i].normal.y < 0.5f)   // ignore the floor we're standing on
					return true;
			return false;
		}

		void Move (Vector2 move)
		{
			move = Vector2.ClampMagnitude(move, 1);
			if (cfg.isFlying)
			{
				rigid.linearVelocity = move * cfg.moveSpeed;
				if (move != Vector2.zero)
					facing = move;
			}
			else
			{
				int moveSign = Ext.Sign(move.x);
				rigid.linearVelocity = rigid.linearVelocity.SetX(moveSign * cfg.moveSpeed);
				if (moveSign != 0)
					facing = new Vector2(moveSign, 0);
			}
			if (facing.x != 0)
				spriteRenderer.flipX = facing.x < 0;
		}

		void HandleAttacking ()
		{
			shootTimer -= Time.deltaTime;
			if (cfg.shootInterval <= 0 || shootTimer > 0)
				return;
			Vector2 toPlayer = Player.instance.trs.position - trs.position;
			float dist = toPlayer.magnitude;
			if (dist >= cfg.attackDistMin && dist <= cfg.attackDistMax)
			{
				shootTimer = cfg.shootInterval;
				Bullet.Spawn(trs.position, toPlayer, cfg.bulletSpeed, cfg.bulletDamage, cfg.bulletLifetime, Layers.Arrow, cfg.bulletSprite);
			}
		}

		// Raycast to the player's centre and four bounds corners; true if any reaches the
		// player's collider before a wall.
		bool CanSeePlayer (Vector2 from, float range)
		{
			Collider2D pc = Player.instance.col;
			Bounds b = pc.bounds;
			sightTargets[0] = Player.instance.trs.position;
			sightTargets[1] = b.min;
			sightTargets[2] = b.max;
			sightTargets[3] = new Vector2(b.min.x, b.max.y);
			sightTargets[4] = new Vector2(b.max.x, b.min.y);
			for (int i = 0; i < sightTargets.Length; i ++)
				if (Physics2D.Raycast(from, sightTargets[i] - from, range, whatBlocksVision).collider == pc)
					return true;
			return false;
		}

		public void TakeDamage (float amount, Vector2 direction)
		{
			hp = Mathf.Clamp(hp - amount, 0, cfg.hp);
			if (hp == 0)
				Death ();
			else if (!chasePlayer)
			{
				// Turn toward whatever hit us for a moment.
				facing = cfg.isFlying ? direction : new Vector2(Mathf.Sign(direction.x), 0);
				lookToHurtDirectionTimer = cfg.lookToHurtDirectionDuration;
			}
		}

		public void Death ()
		{
			gameObject.SetActive(false);   // comes back via ResetState on player respawn
		}

		public void ChasePlayer ()
		{
			chasePlayer = true;
			visionSensor.enabled = false;
			lookToHurtDirectionTimer = 0;
		}

		// The vision circle is a trigger on this body. Stay-events fire every physics step
		// while the player is inside it; we then apply the cone + line-of-sight test.
		void OnTriggerStay2D (Collider2D other)
		{
			if (!initialized || other != Player.instance.col)
				return;
			Vector2 eye = trs.position;
			Vector2 toPlayer = (Vector2) Player.instance.trs.position - eye;
			if (Vector2.Angle(facing, toPlayer) <= cfg.visionAngle && (col.bounds.Intersects(Player.instance.col.bounds) || CanSeePlayer(eye, cfg.visionRange)))
				ChasePlayer ();
		}

		// Touching an enemy hurts (original enemies hurt via their weapon).
		void OnCollisionEnter2D (Collision2D coll)
		{
			if (initialized && cfg.contactDamage > 0 && coll.collider == Player.instance.col)
				Player.instance.TakeDamage(cfg.contactDamage, Player.instance.trs.position - trs.position);
		}

		void OnCollisionStay2D (Collision2D coll)
		{
			OnCollisionEnter2D (coll);
		}
	}
}
'''

WORLD_OBJECTS_CS = {
"Bullet.cs": r'''using System.Collections.Generic;
using UnityEngine;

namespace SlimeJump
{
	// Ported from the original Bullet.cs, minus the object pool, retargeting and
	// despawn modes: a trigger body that flies straight, damages the first
	// IDestructable it hits, and despawns on walls or after its lifetime.
	public class Bullet : MonoBehaviour
	{
		public static readonly List<Bullet> instances = new List<Bullet>();
		public float damage;

		public static Bullet Spawn (Vector2 pos, Vector2 dir, float speed, float damage, float lifetime, int layer, string spriteName)
		{
			GameObject go = new GameObject("Bullet");
			go.layer = layer;
			go.transform.position = pos;
			go.transform.right = dir;      // sprites point right: arrows must face their flight
			SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
			sr.sprite = Art.Get(spriteName);
			sr.sortingOrder = 8;
			Rigidbody2D rb = go.AddComponent<Rigidbody2D>();
			rb.gravityScale = 0;
			rb.linearDamping = 0;
			CircleCollider2D cc = go.AddComponent<CircleCollider2D>();
			cc.radius = 0.2f;
			cc.isTrigger = true;
			Bullet b = go.AddComponent<Bullet>();
			b.damage = damage;
			rb.linearVelocity = dir.normalized * speed;
			instances.Add(b);
			Destroy(go, lifetime);
			return b;
		}

		void OnTriggerEnter2D (Collider2D other)
		{
			if (other.isTrigger)
				return;
			IDestructable destructable = other.GetComponentInParent<IDestructable>();
			if (destructable != null)
			{
				if (other == Player.instance.col && Player.instance.invulnerable)
					return;
				destructable.TakeDamage(damage, transform.position - other.transform.position);
			}
			Destroy(gameObject);
		}

		void OnDestroy ()
		{
			instances.Remove(this);
		}

		public static void DespawnAll ()
		{
			for (int i = instances.Count - 1; i >= 0; i --)
				if (instances[i] != null)
					Destroy(instances[i].gameObject);
			instances.Clear();
		}
	}
}
''',
"Hazard.cs": r'''using UnityEngine;

namespace SlimeJump
{
	// Ported from the original Hazard.cs (used by spikes): damages the player on
	// contact. Only the player's real collider counts - the player's sensor triggers
	// are ignored, as in the original.
	public class Hazard : MonoBehaviour
	{
		public float damage = 1;

		void OnTriggerEnter2D (Collider2D other)
		{
			if (other.isTrigger || Player.instance == null || other != Player.instance.col)
				return;
			Player.instance.TakeDamage(damage, other.transform.position - transform.position);
		}

		void OnTriggerStay2D (Collider2D other)
		{
			OnTriggerEnter2D (other);
		}
	}
}
''',
"Gem.cs": r'''using System.Collections.Generic;
using UnityEngine;

namespace SlimeJump
{
	// Ported from the original Gem.cs. Picking a gem hides it ("pending"); it only
	// counts once a save point is touched (SavePoint -> GameManager.CommitGems).
	// Dying before that puts it back. Committed gems stay as faint ghosts
	// (new: ghosts are inert, the original lets you re-touch them).
	public class Gem : MonoBehaviour, IResettable
	{
		public static readonly List<Gem> all = new List<Gem>();
		public SpriteRenderer spriteRenderer;
		public Collider2D trigger;
		[HideInInspector] public bool pending;

		public bool Collected
		{
			get { return Save.GetBool("Collected " + GameManager.levelName + " " + name, false); }
			set { Save.SetBool("Collected " + GameManager.levelName + " " + name, value); }
		}

		public void Init ()
		{
			all.Add(this);
			GameManager.resettables.Add(this);
			ResetState ();
		}

		public void ResetState ()
		{
			pending = false;
			gameObject.SetActive(true);
			bool collected = Collected;
			spriteRenderer.color = collected ? Color.white.SetAlpha(0.25f) : Color.white;
			trigger.enabled = !collected;
		}

		void OnTriggerEnter2D (Collider2D other)
		{
			if (other != Player.instance.col)
				return;
			pending = true;
			gameObject.SetActive(false);
		}
	}
}
''',
"SavePoint.cs": r'''using UnityEngine;

namespace SlimeJump
{
	// Ported from the original SavePoint.cs: touching it stores the respawn position,
	// commits pending gems and writes the save. (Speed/one-life achievements and the
	// world map hooks from the original are dropped.)
	public class SavePoint : MonoBehaviour
	{
		public SpriteRenderer spriteRenderer;
		public Vector2 respawnPosition;

		public bool Touched
		{
			get { return Save.GetBool("Touched " + GameManager.levelName + " " + name, false); }
			set { Save.SetBool("Touched " + GameManager.levelName + " " + name, value); }
		}

		public void Init ()
		{
			if (Touched)
				spriteRenderer.sprite = Art.Get("savepoint_on");
		}

		void OnTriggerEnter2D (Collider2D other)
		{
			if (other != Player.instance.col)
				return;
			Player.SavedPosition = respawnPosition;
			GameManager.instance.CommitGems ();
			Touched = true;
			spriteRenderer.sprite = Art.Get("savepoint_on");
			Save.SetFloat("Timer " + GameManager.levelName, GameManager.Timer);
			Save.SetInt("Death count " + GameManager.levelName, GameManager.DeathCount);
			Save.Flush ();
			GameManager.instance.Notify("Checkpoint saved");
		}
	}
}
''',
"Crumbly.cs": r"""using UnityEngine;

namespace SlimeJump
{
	// Ported from the original DissolveOnHit (Crumbly Wall prefab): the first collision starts a
	// timer; the wall fades over dissolveTime and then deactivates. Comes back on respawn.
	public class Crumbly : UpdateWhileEnabled, IResettable
	{
		public SpriteRenderer spriteRenderer;
		float dissolveTimer;
		bool wasHit;

		public void Init ()
		{
			GameManager.resettables.Add(this);
		}

		void OnCollisionEnter2D (Collision2D coll)
		{
			if (wasHit)
				return;
			dissolveTimer = Config.Cfg.crumbly.dissolveTime;
			wasHit = true;
		}

		public override void DoUpdate ()
		{
			if (!wasHit)
				return;
			dissolveTimer -= Time.deltaTime;
			if (dissolveTimer <= 0)
				gameObject.SetActive(false);
			else
				spriteRenderer.color = spriteRenderer.color.SetAlpha(dissolveTimer / Config.Cfg.crumbly.dissolveTime);
		}

		public void ResetState ()
		{
			wasHit = false;
			spriteRenderer.color = Color.white;
			gameObject.SetActive(true);
		}
	}
}
""",
"ShooterTrap.cs": r"""using UnityEngine;

namespace SlimeJump
{
	// Ported from the original ShooterTrap + Arrow Shooter prefab: every step it casts a ray along its
	// facing; when the first thing hit is the player and a second has passed since the last shot (the
	// 1s shoot animation), it fires an arrow ("Aim Where Facing" pattern: one arrow along its up).
	public class ShooterTrap : UpdateWhileEnabled, IResettable
	{
		public Vector2 dir;
		int whatBlocksMyShots;
		float sinceShot;

		public void Init (Vector2 dir)
		{
			this.dir = dir;
			whatBlocksMyShots = Layers.Mask(Layers.Wall, Layers.Climbable, Layers.Player);
			GameManager.resettables.Add(this);
			ResetState ();
		}

		public void ResetState ()
		{
			sinceShot = Config.Cfg.arrow.cooldown;      // ready to fire at once (the original re-runs Awake on respawn)
		}

		public override void DoUpdate ()
		{
			sinceShot += Time.deltaTime;
			// start just outside our own tile, which is solid
			Vector2 origin = (Vector2) transform.position + dir * 0.55f;
			RaycastHit2D hit = Physics2D.Raycast(origin, dir, Mathf.Infinity, whatBlocksMyShots);
			if (hit.collider != null && hit.collider == Player.instance.col && sinceShot >= Config.Cfg.arrow.cooldown)
			{
				sinceShot = 0;
				ArrowConfig a = Config.Cfg.arrow;
				Bullet.Spawn(origin, dir, a.speed, a.damage, a.lifetime, Layers.Arrow, "arrow");
			}
		}
	}
}
""",
"Goal.cs": r'''using UnityEngine;

namespace SlimeJump
{
	// Replaces the original End.cs / WinScreen: reaching it wins the level.
	public class Goal : MonoBehaviour
	{
		void OnTriggerEnter2D (Collider2D other)
		{
			if (other == Player.instance.col)
				GameManager.instance.Win ();
		}
	}
}
''',
}

GAMECAMERA_CS = r'''using UnityEngine;

namespace SlimeJump
{
	// Ported from the original GameCamera: follows the player (instant), with a
	// followPlayer switch that Player/Lasso use while the rope is taut or on death.
	// (new) Clamped to the level bounds when the level is bigger than the view.
	public class GameCamera : MonoBehaviour
	{
		public static GameCamera instance;
		public Transform trs;
		public Camera cam;
		public bool followPlayer = true;
		public Rect viewRect;
		Rect bounds;

		public void Init (Camera cam, LevelData level)
		{
			instance = this;
			this.cam = cam;
			trs = cam.transform;
			bounds = Rect.MinMaxRect(level.bounds[0], level.bounds[1], level.bounds[2], level.bounds[3]);
		}

		public void HandlePosition ()
		{
			float halfH = cam.orthographicSize;
			float halfW = halfH * cam.aspect;
			if (followPlayer && Player.instance != null)
			{
				Vector3 p = trs.position;
				Vector2 target = Player.instance.trs.position;
				p.x = bounds.width > halfW * 2 ? Mathf.Clamp(target.x, bounds.xMin + halfW, bounds.xMax - halfW) : target.x;
				p.y = bounds.height > halfH * 2 ? Mathf.Clamp(target.y, bounds.yMin + halfH, bounds.yMax - halfH) : target.y;
				trs.position = p;
			}
			viewRect = new Rect(trs.position.x - halfW, trs.position.y - halfH, halfW * 2, halfH * 2);
		}
	}
}
'''

GAMEMANAGER_CS = r'''using System.Collections.Generic;
using UnityEngine;

namespace SlimeJump
{
	// Ported (heavily trimmed) from the original GameManager: owns the frame loop
	// (calls DoUpdate on every enabled UpdateWhileEnabled in registration order),
	// pause, timer, death/gem counters, notifications and a debug HUD.
	// PORT NOTE: the HUD is IMGUI text in place of the original Unity UI canvas.
	public class GameManager : MonoBehaviour
	{
		public static GameManager instance;
		public static bool paused;
		public static bool won;
		public static uint framesSinceLevelLoaded;
		public static LevelData level;
		public static string levelName;
		public static float Timer;
		public static int DeathCount;
		public static int Gems;
		public static readonly List<IResettable> resettables = new List<IResettable>();

		public static Vector2 SpawnPosition { get { return new Vector2(level.spawn[0], level.spawn[1]); } }

		readonly List<UpdateWhileEnabled> buffer = new List<UpdateWhileEnabled>();
		string notification;
		float notificationUntil;
		GUIStyle style;
		GUIStyle bigStyle;

		public void Init (LevelData lvl)
		{
			instance = this;
			level = lvl;
			levelName = lvl.name;
			paused = false;
			won = false;
			framesSinceLevelLoaded = 0;
			resettables.Clear();
			Gem.all.Clear();
			Bullet.instances.Clear();
			UpdateWhileEnabled.updatables.Clear();
			Timer = Save.GetFloat("Timer " + levelName, 0);
			DeathCount = Save.GetInt("Death count " + levelName, 0);
			Gems = Save.GetInt("Gems " + levelName, 0);
		}

		void Update ()
		{
			Physics2D.SyncTransforms();
			if (InputManager.Pressed(Btn.Pause))
				SetPaused(!paused);
			if (InputManager.Pressed(Btn.ResetProgress))
				ResetProgress ();
			if (!paused)
			{
				buffer.Clear();
				buffer.AddRange(UpdateWhileEnabled.updatables);
				for (int i = 0; i < buffer.Count; i ++)
				{
					UpdateWhileEnabled u = buffer[i];
					if (u != null && u.isActiveAndEnabled)
						u.DoUpdate ();
				}
			}
			GameCamera.instance.HandlePosition ();
			framesSinceLevelLoaded ++;
		}

		public static void SetPaused (bool pause)
		{
			paused = pause;
			Time.timeScale = pause ? 0 : Config.Cfg.world.timeScale;
		}

		// Puts every registered object back to its initial state (called on respawn).
		public void ResetWorld ()
		{
			Bullet.DespawnAll ();
			for (int i = 0; i < resettables.Count; i ++)
				resettables[i].ResetState ();
		}

		// Called by SavePoint: pending gems become permanent.
		public void CommitGems ()
		{
			for (int i = 0; i < Gem.all.Count; i ++)
			{
				Gem gem = Gem.all[i];
				if (gem.pending)
				{
					gem.Collected = true;
					Gems ++;
					Save.SetInt("Gems " + levelName, Gems);
					gem.ResetState ();   // now shown as a ghost
				}
			}
		}

		public void Win ()
		{
			if (won)
				return;
			won = true;
			Save.SetFloat("Timer " + levelName, Timer);
			Save.SetBool("Won " + levelName, true);
			Save.Flush ();
		}

		void ResetProgress ()
		{
			Save.Clear ();
			Timer = 0;
			DeathCount = 0;
			Gems = 0;
			won = false;
			Player.instance.ForceRespawn ();
			Notify("Progress reset");
		}

		public void Notify (string text)
		{
			notification = text;
			notificationUntil = Time.unscaledTime + 2f;
		}

		void OnApplicationQuit ()
		{
			Save.SetFloat("Timer " + levelName, Timer);
			Save.SetInt("Death count " + levelName, DeathCount);
			Save.Flush ();
		}

		void OnGUI ()
		{
			if (style == null)
			{
				style = new GUIStyle(GUI.skin.label);
				style.fontSize = 18;
				style.normal.textColor = Color.white;
				bigStyle = new GUIStyle(style);
				bigStyle.fontSize = 36;
				bigStyle.alignment = TextAnchor.MiddleCenter;
			}
			GUI.Label(new Rect(10, 8, 700, 30), "Gems " + Gems + "/" + level.gems.Length + "    Deaths " + DeathCount + "    Time " + Timer.ToString("F1"), style);
			if (Time.unscaledTime < notificationUntil)
				GUI.Label(new Rect(10, 34, 700, 30), notification, style);
			if (framesSinceLevelLoaded < 900)
				GUI.Label(new Rect(10, Screen.height - 34, 1200, 30), "A/D move   W jump (hold to climb)   Left click shoot   Right click lasso (W/S reel)   Esc pause   Del reset save", style);
			if (paused)
				GUI.Label(new Rect(0, 0, Screen.width, Screen.height), "PAUSED", bigStyle);
			else if (won)
				GUI.Label(new Rect(0, 0, Screen.width, Screen.height), "You escaped the cave!\n" + Timer.ToString("F1") + "s   " + DeathCount + " deaths", bigStyle);
		}
	}
}
'''

LEVELLOADER_CS = r'''using UnityEngine;

namespace SlimeJump
{
	// Builds the whole game at startup from Resources/level.json + game_config.json,
	// so no scene content is required: open any scene and press Play.
	// This is the "scene" seam - in another engine, this file is the level importer.
	public static class LevelLoader
	{
		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
		static void Bootstrap ()
		{
			if (Resources.Load<TextAsset>("level") == null)
			{
				Debug.LogError("SlimeJump: Resources/level.json not found");
				return;
			}
			Build ();
		}

		public static void Build ()
		{
			Config cfg = Config.Cfg;
			LevelData level = JsonUtility.FromJson<LevelData>(Resources.Load<TextAsset>("level").text);

			// Global physics, matching the original ProjectSettings.
			Physics2D.gravity = new Vector2(0, cfg.world.gravity);
			Physics2D.queriesHitTriggers = false;
			Time.fixedDeltaTime = cfg.world.fixedDeltaTime;
			Time.timeScale = cfg.world.timeScale;
			SetupLayerCollisions ();

			new GameObject("GameManager").AddComponent<GameManager>().Init(level);
			BuildCamera (level, cfg);
			Transform root = new GameObject("Level").transform;

			foreach (RectDef r in level.walls) MakeRect(root, "Wall", r, "rock", Layers.Wall, false, 0);
			foreach (RectDef r in level.climbables) MakeRect(root, "Climbable Wall", r, "moss", Layers.Climbable, false, 1);
			foreach (RectDef r in level.spikes) MakeSpikes(root, r);
			if (level.crumbly != null) foreach (RectDef r in level.crumbly) BuildCrumbly(root, r);
			if (level.shooters != null) foreach (ShooterDef sh in level.shooters) BuildShooter(root, sh);

			Player player = BuildPlayer(cfg, root);
			BuildLasso (cfg, player);
			foreach (PointDef p in level.savepoints) BuildSavePoint(root, p, cfg);
			foreach (PointDef p in level.gems) BuildGem(root, p);
			foreach (EnemyDef e in level.enemies) BuildEnemy(cfg, e, root);
			if (level.goal != null) BuildGoal(root, level.goal);

			player.gameObject.SetActive(true);
			player.Spawn ();
		}

		//@@LAYER_IGNORES@@

		static GameObject Make (string name, Transform parent, Vector2 pos, int layer)
		{
			GameObject go = new GameObject(name);
			go.layer = layer;
			go.transform.SetParent(parent, false);
			go.transform.position = pos;
			return go;
		}

		static Vector2 Centre (RectDef r) { return new Vector2(r.x + r.w / 2, r.y + r.h / 2); }

		static void MakeRect (Transform root, string name, RectDef r, string sprite, int layer, bool trigger, int order)
		{
			GameObject go = Make(name, root, Centre(r), layer);
			SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
			sr.sprite = Art.Get(sprite);
			sr.drawMode = SpriteDrawMode.Tiled;
			sr.size = new Vector2(r.w, r.h);
			sr.sortingOrder = order;
			BoxCollider2D bc = go.AddComponent<BoxCollider2D>();
			bc.size = new Vector2(r.w, r.h);
			bc.sharedMaterial = Art.NoFriction();
			bc.isTrigger = trigger;
		}

		static void MakeSpikes (Transform root, RectDef r)
		{
			GameObject go = Make("Spikes", root, Centre(r), Layers.Hazard);
			SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
			sr.sprite = Art.Get("spike");
			sr.drawMode = SpriteDrawMode.Tiled;
			sr.size = new Vector2(r.w, r.h);
			sr.sortingOrder = 2;
			BoxCollider2D bc = go.AddComponent<BoxCollider2D>();
			bc.isTrigger = true;
			bc.size = new Vector2(r.w, r.h * 0.6f);
			bc.offset = new Vector2(0, -r.h * 0.2f);
			go.AddComponent<Hazard>();
		}

		static void BuildCrumbly (Transform root, RectDef r)
		{
			GameObject go = Make("Crumbly Wall", root, Centre(r), Layers.Wall);
			SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
			sr.sprite = Art.Get("crumbly");
			sr.sortingOrder = 1;
			BoxCollider2D bc = go.AddComponent<BoxCollider2D>();
			bc.size = new Vector2(r.w + 0.04f, r.h);      // neighbouring tiles overlap: flush boxes make the player catch on the seam
			bc.sharedMaterial = Art.NoFriction();
			Crumbly c = go.AddComponent<Crumbly>();
			c.spriteRenderer = sr;
			c.Init ();
		}

		// The shooter's tile is already part of the walls; this adds the socket picture and the trap.
		static void BuildShooter (Transform root, ShooterDef sh)
		{
			GameObject go = Make("Arrow Shooter", root, new Vector2(sh.x, sh.y), Layers.Wall);
			go.transform.right = new Vector2(sh.dx, sh.dy);
			SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
			sr.sprite = Art.Get("shooter");
			sr.sortingOrder = 2;
			go.AddComponent<ShooterTrap>().Init(new Vector2(sh.dx, sh.dy));
		}

		static Camera BuildCamera (LevelData level, Config cfg)
		{
			Camera cam = Camera.main;
			if (cam == null)
			{
				GameObject g = new GameObject("Main Camera");
				g.tag = "MainCamera";
				cam = g.AddComponent<Camera>();
				g.AddComponent<AudioListener>();
			}
			cam.orthographic = true;
			cam.orthographicSize = cfg.world.cameraSize;
			cam.clearFlags = CameraClearFlags.SolidColor;
			Color bg;
			ColorUtility.TryParseHtmlString(level.background, out bg);
			cam.backgroundColor = bg;
			cam.transform.position = new Vector3(level.spawn[0], level.spawn[1], -10);
			cam.transform.rotation = Quaternion.identity;
			cam.gameObject.AddComponent<GameCamera>().Init(cam, level);
			return cam;
		}

		static Player BuildPlayer (Config cfg, Transform root)
		{
			PlayerConfig pc = cfg.player;
			GameObject go = new GameObject("Player");
			go.SetActive(false);   // activated at the end of Build
			go.layer = Layers.Player;
			go.transform.SetParent(root, false);
			Rigidbody2D rb = go.AddComponent<Rigidbody2D>();
			rb.gravityScale = 1;
			rb.mass = pc.mass;
			rb.linearDamping = pc.linearDamping;
			rb.angularDamping = 0.05f;
			rb.constraints = RigidbodyConstraints2D.FreezeRotation;
			rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
			rb.sleepMode = RigidbodySleepMode2D.NeverSleep;
			BoxCollider2D bc = go.AddComponent<BoxCollider2D>();
			bc.size = new Vector2(pc.colliderW, pc.colliderH);
			bc.sharedMaterial = Art.NoFriction();

			GameObject gfx = new GameObject("Graphics");
			gfx.transform.SetParent(go.transform, false);
			SpriteRenderer sr = gfx.AddComponent<SpriteRenderer>();
			sr.sprite = Art.Get("slime");
			sr.sortingOrder = 10;

			// Slightly wider than the body: detects Climbable walls we are pressed against.
			GameObject sensor = new GameObject("ClimbableSensor");
			sensor.layer = Layers.PlayerSensor;
			sensor.transform.SetParent(go.transform, false);
			BoxCollider2D cb = sensor.AddComponent<BoxCollider2D>();
			cb.isTrigger = true;
			cb.size = new Vector2(pc.colliderW + 0.3f, pc.colliderH * 0.6f);

			Player p = go.AddComponent<Player>();
			p.trs = go.transform;
			p.graphicsTrs = gfx.transform;
			p.rigid = rb;
			p.spriteRenderer = sr;
			p.col = bc;
			p.climbableSensor = cb;
			p.Init(pc);
			return p;
		}

		static void BuildLasso (Config cfg, Player player)
		{
			GameObject go = new GameObject("Lasso");
			go.transform.SetParent(player.trs, false);
			LineRenderer lr = go.AddComponent<LineRenderer>();
			lr.useWorldSpace = false;
			lr.positionCount = 2;
			lr.startWidth = lr.endWidth = 0.12f;
			lr.material = Art.SpriteMaterial();
			lr.startColor = lr.endColor = new Color(0.95f, 0.85f, 0.55f);
			lr.sortingOrder = 9;
			lr.enabled = false;

			GameObject hook = new GameObject("Hook");
			hook.transform.position = player.trs.position;
			GameObject hookGfx = new GameObject("HookGraphics");
			hookGfx.transform.SetParent(hook.transform, false);
			SpriteRenderer hsr = hookGfx.AddComponent<SpriteRenderer>();
			hsr.sprite = Art.Get("bullet_green");
			hsr.sortingOrder = 9;

			Lasso l = go.AddComponent<Lasso>();
			l.trs = go.transform;
			l.lineRenderer = lr;
			l.hookTrs = hook.transform;
			l.hookGraphicsTrs = hookGfx.transform;
			l.hookSpriteRenderer = hsr;
			l.Init(cfg.lasso);
		}

		static void BuildSavePoint (Transform root, PointDef p, Config cfg)
		{
			GameObject go = Make(p.name, root, new Vector2(p.x, p.y), Layers.Gem);
			SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
			sr.sprite = Art.Get("savepoint");
			sr.sortingOrder = 3;
			BoxCollider2D bc = go.AddComponent<BoxCollider2D>();
			bc.isTrigger = true;
			bc.size = new Vector2(1.2f, 1.4f);
			bc.offset = new Vector2(0, 0.7f);
			SavePoint sp = go.AddComponent<SavePoint>();
			sp.spriteRenderer = sr;
			sp.respawnPosition = new Vector2(p.x, p.y + cfg.player.colliderH * 0.5f + 0.2f);
			sp.Init ();
		}

		static void BuildGem (Transform root, PointDef p)
		{
			GameObject go = Make(p.name, root, new Vector2(p.x, p.y), Layers.Gem);
			SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
			sr.sprite = Art.Get("gem");
			sr.sortingOrder = 3;
			CircleCollider2D cc = go.AddComponent<CircleCollider2D>();
			cc.isTrigger = true;
			cc.radius = 0.45f;
			Gem g = go.AddComponent<Gem>();
			g.spriteRenderer = sr;
			g.trigger = cc;
			g.Init ();
		}

		static void BuildGoal (Transform root, PointDef p)
		{
			GameObject go = Make("Goal", root, new Vector2(p.x, p.y), Layers.Gem);
			SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
			sr.sprite = Art.Get("goal");
			sr.sortingOrder = 3;
			BoxCollider2D bc = go.AddComponent<BoxCollider2D>();
			bc.isTrigger = true;
			bc.size = new Vector2(1.4f, 2f);
			go.AddComponent<Goal>();
		}

		static void BuildEnemy (Config cfg, EnemyDef def, Transform root)
		{
			EnemyConfig ec = cfg.Enemy(def.type);
			Vector2 pos = new Vector2(def.x, def.y);
			GameObject go = Make("Enemy " + def.type, root, pos, Layers.Enemy);
			go.SetActive(false);   // Enemy.Init -> ResetState activates it
			Rigidbody2D rb = go.AddComponent<Rigidbody2D>();
			rb.gravityScale = ec.gravityScale;
			rb.mass = ec.mass;
			rb.linearDamping = 0;
			rb.constraints = RigidbodyConstraints2D.FreezeRotation;
			rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
			rb.sleepMode = RigidbodySleepMode2D.NeverSleep;
			BoxCollider2D bc = go.AddComponent<BoxCollider2D>();
			bc.size = new Vector2(ec.colliderW, ec.colliderH);
			bc.sharedMaterial = Art.NoFriction();
			CircleCollider2D vision = go.AddComponent<CircleCollider2D>();
			vision.isTrigger = true;
			SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
			sr.sprite = Art.Get(ec.sprite);
			sr.sortingOrder = 5;
			Enemy e = go.AddComponent<Enemy>();
			e.trs = go.transform;
			e.rigid = rb;
			e.col = bc;
			e.visionSensor = vision;
			e.spriteRenderer = sr;
			e.Init(ec, pos);
		}
	}
}
'''

SOURCES = {
    "Core.cs": CORE_CS, "Data.cs": DATA_CS, "Save.cs": SAVE_CS,
    "InputManager.cs": INPUT_CS, "Art.cs": ART_CS,
    "Player.cs": PLAYER_CS, "Lasso.cs": LASSO_CS, "Enemy.cs": ENEMY_CS,
    "GameCamera.cs": GAMECAMERA_CS, "GameManager.cs": GAMEMANAGER_CS,
    "LevelLoader.cs": LEVELLOADER_CS,
}
SOURCES.update(WORLD_OBJECTS_CS)
EDITOR_SOURCES = {"EditorSetup.cs": EDITOR_CS}

README_TEMPLATE = """# {name} (generated by tools/gen_slime.py)

Open this folder with Unity {version}, open/create any scene, press Play.
The game is built at runtime from `Assets/Resources/level.json`,
`game_config.json` and `sprites.json` (see `Assets/Scripts/LevelLoader.cs`).

Controls: A/D move, W jump (hold to climb mossy walls), left click shoot,
right click lasso (W/S reel in/out), Esc pause, Del reset save.

Input: uses the legacy Input Manager unless the Input System package is
installed and enabled (re-run gen_slime.py with --input-system to add it).
Do not edit these files by hand - regenerate them.
"""

GITIGNORE = "[Ll]ibrary/\n[Tt]emp/\n[Oo]bj/\n[Bb]uild*/\n[Ll]ogs/\n[Uu]ser[Ss]ettings/\n*.csproj\n*.sln\n"
MARKER = ".gen_slime"


# ======================================================================================
# 5. PROJECT WRITER
# ======================================================================================
def validate_level(level):
    for key in ("name", "background", "bounds", "spawn", "walls", "climbables", "spikes",
                "savepoints", "gems", "enemies", "goal"):
        if key not in level:
            raise ValueError("level is missing '%s'" % key)
    assert len(level["bounds"]) == 4 and len(level["spawn"]) == 2
    for key in ("walls", "climbables", "spikes"):
        for r in level[key]:
            assert set(r) >= {"x", "y", "w", "h"} and r["w"] > 0 and r["h"] > 0, (key, r)
    for e in level["enemies"]:
        assert e["type"] in ("worm", "bat"), e
    names = [p["name"] for p in level["savepoints"] + level["gems"]]
    assert len(names) == len(set(names)), "savepoint/gem names must be unique (used as save keys)"


def _write(path, text):
    os.makedirs(os.path.dirname(path), exist_ok=True)
    with open(path, "w", encoding="utf-8", newline="\n") as f:
        f.write(text)


def write_project(out, level, unity_version=DEFAULT_UNITY_VERSION, config=None,
                  input_system=False, data_only=False, force=False, sprites=None):
    """Write the Unity project (or, with data_only, just the three JSON files).
    sprites: a sprites.json structure (see gen_sprites.sprite_sheet); default = built-in art."""
    validate_level(level)
    config = config or CONFIG
    if os.path.exists(out) and os.listdir(out):
        if not (os.path.exists(os.path.join(out, MARKER)) or force):
            raise SystemExit("%s exists and was not created by gen_slime (use --force)" % out)
        for entry in os.listdir(out):
            p = os.path.join(out, entry)
            shutil.rmtree(p) if os.path.isdir(p) else os.remove(p)
    os.makedirs(out, exist_ok=True)
    _write(os.path.join(out, MARKER), "generated by tools/gen_slime.py\n")

    res = os.path.join(out, "Assets", "Resources")
    _write(os.path.join(res, "game_config.json"), json.dumps(config, indent=1))
    _write(os.path.join(res, "level.json"), json.dumps(level, indent=1))
    _write(os.path.join(res, "sprites.json"), json.dumps(sprites or build_sprites(), indent=1))
    written = []
    if not data_only:
        for name, text in SOURCES.items():
            if name == "LevelLoader.cs":
                text = text.replace("\t\t//@@LAYER_IGNORES@@", unity_layer_ignores_cs())
            _write(os.path.join(out, "Assets", "Scripts", name), text)
            written.append(os.path.join(out, "Assets", "Scripts", name))
        _write(os.path.join(out, "Assets", "Scripts", "Layers.cs"), layers_cs())
        written.append(os.path.join(out, "Assets", "Scripts", "Layers.cs"))
        for name, text in EDITOR_SOURCES.items():
            _write(os.path.join(out, "Assets", "Editor", name), text)
            written.append(os.path.join(out, "Assets", "Editor", name))
        deps = {"com.unity.modules.physics2d": "1.0.0", "com.unity.modules.imgui": "1.0.0",
                "com.unity.modules.jsonserialize": "1.0.0", "com.unity.modules.audio": "1.0.0",
                "com.unity.modules.ui": "1.0.0"}
        if input_system:
            deps["com.unity.inputsystem"] = "1.19.0"
        _write(os.path.join(out, "Packages", "manifest.json"),
               json.dumps({"dependencies": deps}, indent=2))
        ver = "m_EditorVersion: %s\n" % unity_version
        if unity_version == DEFAULT_UNITY_VERSION:
            ver += "m_EditorVersionWithRevision: %s (%s)\n" % (unity_version, DEFAULT_UNITY_REVISION)
        _write(os.path.join(out, "ProjectSettings", "ProjectVersion.txt"), ver)
        _write(os.path.join(out, ".gitignore"), GITIGNORE)
        _write(os.path.join(out, "README.md"),
               README_TEMPLATE.format(name=level["name"], version=unity_version))
    return written


# ======================================================================================
# 6. SYNTAX CHECK (tree-sitter; NOT a compile against UnityEngine)
# ======================================================================================
def check_csharp(paths):
    """Returns a list of 'file:line: problem' strings, or None if tree-sitter is missing."""
    try:
        import tree_sitter_c_sharp as tscs
        from tree_sitter import Language, Parser
    except ImportError:
        return None
    parser = Parser(Language(tscs.language()))
    problems = []

    def walk(node, path):
        if node.type == "ERROR" or node.is_missing:
            problems.append("%s:%d: syntax %s" % (path, node.start_point[0] + 1,
                                                  "error" if node.type == "ERROR" else "missing " + node.type))
            return
        for ch in node.children:
            if ch.has_error or ch.is_missing:
                walk(ch, path)

    for path in paths:
        with open(path, "rb") as f:
            tree = parser.parse(f.read())
        if tree.root_node.has_error:
            walk(tree.root_node, path)
    return problems


# ======================================================================================
# 6b. SUBSET-C# TARGETS: Prowl2D and Stride2D
#     Both engines run games written in the C# subset that CCSharp translates to C
#     (no interfaces, virtual dispatch, delegates, lambdas, exceptions, LINQ; scripts are
#     pooled arena classes; arrays are values; output is Console.WriteLine only), and both
#     expose the same Scene2D/Node/[Script] API. So ONE source serves both: the engine
#     namespace is the only difference (@NS@ / @PB2NS@ tokens below).
#
#     PORT NOTES (differences from the Unity target, and why):
#       * No input exists in either engine: InputState is the seam; a bot (BotScript) fills it.
#       * Gravity is applied by PlayerScript with the body's GravityScale = 0. Engines cannot
#         change a live body's gravity scale, and climbing needs gravity off.
#       * Ground / wall / climb sensing use OverlapBox probes instead of collision normals:
#         the engines disagree on the sign of the contact normal (Prowl2D: other->self,
#         Stride2D: self->other), probes do not depend on it.
#       * Death teleports to the savepoint after a lock delay (no fracture effect, no
#         simulated=false); collected gems stay hidden (no ghost tint).
#       * Layer collision matrix comes from LAYER_IGNORES via Sim.SetLayerMatrix.
# ======================================================================================
ENGINES = {
    "prowl2d": {"ns": "Prowl.Core2D", "pb2": "Prowl.Native.Box2D", "player": "prowl2d-player"},
    "stride2d": {"ns": "Stride2D", "pb2": "Stride2D.Native.Box2D", "player": "stride2d-player"},
}

SUBSET_INPUT_CS = r'''// The input seam. Neither engine has input yet: a platform layer (window, keyboard)
// or a bot writes these once per fixed step, before PlayerScript runs.
static class InputState
{
    public static float Move;      // -1 .. 1
    public static bool Jump;       // held
    public static bool Attack;     // held: fire the blaster
    public static float AimX;      // world position of the cursor
    public static float AimY;
    public static bool Lasso;      // held: shoot the lasso, release to let go
    public static int ChangeLassoLength;   // -1 reel in (W), +1 let out (S)
}
'''

SUBSET_SHARED_CS = r'''using System;
using @NS@;
using @PB2NS@;

// All state that scripts share. Scripts write to it and read from it; it never refers to a
// script type, so the translator has no dependency cycle to order (a static variable must be
// declared before use in the generated C). Static arrays are only touched through helpers
// that take the array as a parameter (CC# cannot subscript a static array field directly).
//
// Data flow is one way: a script that wants to change something it does not own (damage an
// enemy, kill the player) writes a request here; the owner reads and applies it next step.
static class Shared
{
    public const int TagPlayer = 1;
    public const int TagWall = 2;
    public const int TagHazard = 3;
    public const int TagSavePoint = 4;
    public const int TagGem = 5;
    public const int TagGoal = 6;
    public const int TagBulletPlayer = 7;
    public const int TagBulletEnemy = 8;
    public const int TagArrow = 9;
    public const int TagEnemyBase = 100;        // enemy i has Tag TagEnemyBase + i

    public static bool Won;
    public static int Deaths;
    public static int Gems;
    public static bool DeathRequested;          // set by hazards, enemies, bullets; PlayerScript acts on it next step
    public static float SaveX;
    public static float SaveY;
    public static Node PlayerNode;
    public static int PlayerColliderIndex;
    public static bool PlayerGrounded;          // published by PlayerScript for the bot / renderer
    public static bool PlayerClimbing;
    public static bool PlayerJumping;
    public static float PlayerFacing = 1f;
    // ---- lasso: written by LassoScript, read by PlayerScript (the swing) and the renderer ----
    public static bool LassoActive;             // a rope is out (flying or attached)
    public static bool LassoAttached;
    public static float LassoOffX;              // while flying: the tip's offset from the player (as in the Unity Lasso)
    public static float LassoOffY;
    public static float LassoDirX;
    public static float LassoDirY;
    public static float LassoTipX;              // the hook's world position
    public static float LassoTipY;
    public static float LassoLength;
    public static float SwingW;                 // swingAngularVelocity of the Unity Player
    public static float SwingAngle;             // degrees
    public static float PlayerLastMoveX;        // displacement over the last step, for the momentum on release
    public static float PlayerLastMoveY;
    public static void LassoDetach() { LassoActive = false; LassoAttached = false; }

    static Node[] gemNodes;
    static bool[] gemPending;
    static bool[] gemCollected;
    static bool[] saveTouched;
    static int gemCount;

    static Node[] crumblyNodes;
    static float[] crumblyAlpha;
    static bool[] crumblyReset;
    static int crumblyCount;

    static Node[] enemyNodes;
    static float[] enemyX;
    static float[] enemyY;
    static float[] enemyFace;
    static float[] enemyDamage;
    static float[] enemyHitDX;
    static float[] enemyHitDY;
    static float[] enemyInitX;
    static float[] enemyInitY;
    static int[] enemyKind;
    static bool[] enemyAlive;
    static bool[] enemyChase;
    static bool[] enemyReset;
    static int enemyCount;
    static uint rng = 2463534242u;

    static void SetB(bool[] a, int i, bool v) { a[i] = v; }
    static bool GetB(bool[] a, int i) { return a[i]; }
    static void SetN(Node[] a, int i, Node v) { a[i] = v; }
    static Node GetN(Node[] a, int i) { return a[i]; }
    static void SetF(float[] a, int i, float v) { a[i] = v; }
    static float GetF(float[] a, int i) { return a[i]; }
    static void SetI(int[] a, int i, int v) { a[i] = v; }
    static int GetI(int[] a, int i) { return a[i]; }

    // deterministic random numbers (xorshift32): the same on .NET and in C
    public static float Rand01()
    {
        rng ^= rng << 13;
        rng ^= rng >> 17;
        rng ^= rng << 5;
        return (float)(rng & 0xFFFFFFu) / 16777216f;
    }

    // ---- gems / savepoints ----
    public static void InitGems(int count)
    {
        gemCount = count;
        gemNodes = new Node[count + 1];
        gemPending = new bool[count + 1];
        gemCollected = new bool[count + 1];
    }

    public static void RegisterGem(int i, Node n) { SetN(gemNodes, i, n); }

    public static void InitSaves(int count) { saveTouched = new bool[count + 1]; }
    public static void TouchSave(int i) { SetB(saveTouched, i, true); }
    public static bool SaveTouched(int i) { return GetB(saveTouched, i); }

    // 0 = on the map, 1 = picked up but not yet saved, 2 = saved (drawn as a faint ghost)
    public static int GemState(int i)
    {
        if (GetB(gemCollected, i)) return 2;
        if (GetB(gemPending, i)) return 1;
        return 0;
    }

    // Picking a gem up hides it; it only counts once a savepoint is touched.
    public static void PickUpGem(int i)
    {
        SetB(gemPending, i, true);
        Scene2D.Current.SetActive(GetN(gemNodes, i), false);
    }

    public static void CommitGems()
    {
        for (int i = 0; i < gemCount; i++)
            if (GetB(gemPending, i))
            {
                SetB(gemPending, i, false);
                SetB(gemCollected, i, true);
                Gems++;
            }
    }

    // On death: gems picked up since the last savepoint come back.
    public static void ResetGems()
    {
        for (int i = 0; i < gemCount; i++)
            if (GetB(gemPending, i))
            {
                SetB(gemPending, i, false);
                Scene2D.Current.SetActive(GetN(gemNodes, i), true);
            }
    }

    // ---- crumbly walls ----
    public static void InitCrumbly(int count)
    {
        crumblyCount = count;
        crumblyNodes = new Node[count + 1];
        crumblyAlpha = new float[count + 1];
        crumblyReset = new bool[count + 1];
        for (int i = 0; i < count; i++) SetF(crumblyAlpha, i, 1f);
    }

    public static void RegisterCrumbly(int i, Node n) { SetN(crumblyNodes, i, n); }
    public static float CrumblyAlpha(int i) { return GetF(crumblyAlpha, i); }
    public static void SetCrumblyAlpha(int i, float a) { SetF(crumblyAlpha, i, a); }
    public static bool CrumblyResetFlag(int i) { return GetB(crumblyReset, i); }
    public static void ClearCrumblyReset(int i) { SetB(crumblyReset, i, false); }

    public static int CrumbledCount()
    {
        int n = 0;
        for (int i = 0; i < crumblyCount; i++)
            if (GetF(crumblyAlpha, i) <= 0f) n++;
        return n;
    }

    // On respawn every crumbly wall comes back whole.
    public static void ResetCrumbly()
    {
        for (int i = 0; i < crumblyCount; i++)
        {
            SetB(crumblyReset, i, true);
            SetF(crumblyAlpha, i, 1f);
            Scene2D.Current.SetActive(GetN(crumblyNodes, i), true);
        }
    }

    // ---- enemies ----
    public static void InitEnemies(int count)
    {
        enemyCount = count;
        enemyNodes = new Node[count + 1];
        enemyX = new float[count + 1];
        enemyY = new float[count + 1];
        enemyFace = new float[count + 1];
        enemyDamage = new float[count + 1];
        enemyHitDX = new float[count + 1];
        enemyHitDY = new float[count + 1];
        enemyInitX = new float[count + 1];
        enemyInitY = new float[count + 1];
        enemyKind = new int[count + 1];
        enemyAlive = new bool[count + 1];
        enemyChase = new bool[count + 1];
        enemyReset = new bool[count + 1];
    }

    public static void RegisterEnemy(int i, Node n, int kind, float x, float y)
    {
        SetN(enemyNodes, i, n);
        SetI(enemyKind, i, kind);
        SetF(enemyInitX, i, x);
        SetF(enemyInitY, i, y);
        SetF(enemyX, i, x);
        SetF(enemyY, i, y);
        SetF(enemyFace, i, 1f);
        SetB(enemyAlive, i, true);
    }

    public static int EnemyCount() { return enemyCount; }
    public static bool EnemyAlive(int i) { return GetB(enemyAlive, i); }
    public static void SetEnemyAlive(int i, bool a) { SetB(enemyAlive, i, a); }
    public static int EnemyKind(int i) { return GetI(enemyKind, i); }
    public static float EnemyX(int i) { return GetF(enemyX, i); }
    public static float EnemyY(int i) { return GetF(enemyY, i); }
    public static float EnemyFace(int i) { return GetF(enemyFace, i); }
    public static bool EnemyChase(int i) { return GetB(enemyChase, i); }
    public static float EnemyHitDX(int i) { return GetF(enemyHitDX, i); }
    public static float EnemyHitDY(int i) { return GetF(enemyHitDY, i); }

    public static void PublishEnemy(int i, float x, float y, float face, bool chase)
    {
        SetF(enemyX, i, x);
        SetF(enemyY, i, y);
        SetF(enemyFace, i, face);
        SetB(enemyChase, i, chase);
    }

    // A bullet hit enemy i: the enemy applies it on its next step.
    public static void DamageEnemy(int i, float dmg, float dx, float dy)
    {
        SetF(enemyDamage, i, GetF(enemyDamage, i) + dmg);
        SetF(enemyHitDX, i, dx);
        SetF(enemyHitDY, i, dy);
    }

    public static float TakeEnemyDamage(int i)
    {
        float d = GetF(enemyDamage, i);
        SetF(enemyDamage, i, 0f);
        return d;
    }

    public static bool EnemyResetFlag(int i) { return GetB(enemyReset, i); }
    public static void ClearEnemyReset(int i) { SetB(enemyReset, i, false); }

    // On player respawn every enemy returns to its start, dead ones come back.
    public static void ResetEnemies()
    {
        for (int i = 0; i < enemyCount; i++)
        {
            SetB(enemyReset, i, true);
            SetF(enemyDamage, i, 0f);
            if (!GetB(enemyAlive, i))
            {
                Node n = GetN(enemyNodes, i);
                n.SetPosition(GetF(enemyInitX, i), GetF(enemyInitY, i));
                SetB(enemyAlive, i, true);
                Scene2D.Current.SetActive(n, true);
            }
        }
    }

    // ---- bullets ----
    public static void ResetBullets()
    {
        Scene2D scene = Scene2D.Current;
        for (int i = 0; i < scene.NodeHighWater; i++)
        {
            Node b = scene.NodeAt(i);
            if (b != null && b.Alive && !b.Destroyed && (b.Tag == TagBulletPlayer || b.Tag == TagBulletEnemy || b.Tag == TagArrow))
                scene.Destroy(b);
        }
    }

    public static void ResetWorld()
    {
        ResetGems();
        ResetEnemies();
        ResetBullets();
        ResetCrumbly();
    }
}
'''

SUBSET_PLAYER_CS = r'''using System;
using @NS@;
using @PB2NS@;

// Ported from the Unity Player: move, variable-height jump, wall climbing, kill plane,
// death/respawn. (Lasso, blaster, enemies come in later steps.)
[Script(Order = 10), MaxInstances(1)]
class PlayerScript
{
    public Component Self;
    public bool IsGrounded;
    public bool IsJumping;
    public bool IsClimbing;
    public bool IsHittingWall;
    public float Facing;
    bool climbedSinceJumped;
    float prevX;
    float prevY;
    float shootTimer;
    float timeSinceJump;
    float jumpVel;
    float maxJumpDuration;
    float lockTimer;
    Rigidbody2D body;
    Node node;
    Scene2D scene;
    uint solidMask;
    uint climbMask;

    public void Start()
    {
        scene = Scene2D.Current;
        node = Self.Node;
        Shared.PlayerNode = node;
        body = scene.Find(node, ComponentKind.Rigidbody2D).Body;
        solidMask = (1u << Layers.Wall) | (1u << Layers.Climbable);
        climbMask = 1u << Layers.Climbable;
        Shared.SaveX = Level.SpawnX();
        Shared.SaveY = Level.SpawnY();
        prevX = node.WorldX();
        prevY = node.WorldY();
        Facing = 1f;
        // How long a full-height jump lasts (same estimate as the Unity Awake).
        float v = Cfg.JumpSpeed;
        maxJumpDuration = 0f;
        while (v > 0f)
        {
            v += Cfg.Gravity * Cfg.Dt;
            v *= 1f - Cfg.LinearDamping * Cfg.Dt;
            maxJumpDuration += Cfg.Dt;
        }
    }

    bool Probe(float cx, float cy, float halfW, float halfH, uint mask)
    {
        return scene.Physics.Sim.OverlapBox(cx, cy, halfW, halfH, 0f, mask, false) > 0;
    }

    public void FixedUpdate()
    {
        float x = node.WorldX();
        float y = node.WorldY();
        if (lockTimer > 0f)
        {
            lockTimer -= Cfg.Dt;
            Shared.DeathRequested = false;
            body.SetVelocity(0f, 0f);
            prevX = x;
            prevY = y;
            return;
        }
        if (Shared.DeathRequested || y < Level.MinY() - Cfg.KillPlaneMargin)
        {
            Death();
            return;
        }
        float halfW = Cfg.ColliderW * 0.5f;
        float halfH = Cfg.ColliderH * 0.5f;

        // Lasso: while the rope is taut the player swings on it. The Unity code makes the player a
        // child of the hook and rotates the hook; rotating the player's position about the hook point
        // by the same angle is the same motion, without parenting a physics body.
        if (Shared.LassoAttached)
        {
            float toX = Shared.LassoTipX - x;
            float toY = Shared.LassoTipY - y;
            float dist = MathF.Sqrt(toX * toX + toY * toY);
            float remaining = Shared.LassoLength - dist;
            if (remaining <= 0f && dist > 0.0001f && Shared.LassoLength > 0.0001f)
            {
                x += toX / dist * -remaining;                       // pull back onto the circle
                y += toY / dist * -remaining;
                float acc = Cfg.LassoSwingSpeed * MathF.Cos(Shared.SwingAngle * 0.017453292f);
                Shared.SwingW += acc * Cfg.Dt;
                Shared.SwingW *= 1f - Cfg.LinearDamping * Cfg.Dt;
                float dAngle = Shared.SwingW / Shared.LassoLength * Cfg.Dt;      // degrees
                Shared.SwingAngle += dAngle;
                float rx = x - Shared.LassoTipX;
                float ry = y - Shared.LassoTipY;
                float ca = MathF.Cos(dAngle * 0.017453292f);
                float sa = MathF.Sin(dAngle * 0.017453292f);
                x = Shared.LassoTipX + rx * ca - ry * sa;
                y = Shared.LassoTipY + rx * sa + ry * ca;
                node.SetPosition(x, y);
            }
        }
        float vy = body.VelocityY();

        // blaster: fires toward the aim point (a stand-in for the Unity weapon; no knockback)
        shootTimer -= Cfg.Dt;
        if (InputState.Attack && shootTimer <= 0f)
        {
            float ax = InputState.AimX - x;
            float ay = InputState.AimY - y;
            float al = MathF.Sqrt(ax * ax + ay * ay);
            if (al < 0.001f) { ax = Facing; ay = 0f; al = 1f; }
            shootTimer = Cfg.ShootCooldown;
            BulletScript.Spawn(x, y, ax / al * Cfg.BulletSpeed, ay / al * Cfg.BulletSpeed, Cfg.BulletDamage, Cfg.BulletLifetime, true);
        }

        // sense the world (probes, not contact normals)
        IsGrounded = Probe(x, y - halfH - 0.03f, halfW * 0.9f, 0.05f, solidMask);

        // moving
        float moveInput = InputState.Move;
        bool jumpInput = InputState.Jump;
        IsHittingWall = false;
        if (moveInput != 0f)
        {
            float side = moveInput > 0f ? 1f : -1f;
            IsHittingWall = Probe(x + side * (halfW + 0.03f), y, 0.03f, halfH * 0.9f, solidMask);
            Facing = side;
        }
        float vx = 0f;
        if (!IsHittingWall) vx = moveInput * Cfg.MoveSpeed;

        // jumping (releasing early cuts the jump short)
        if (!IsClimbing)
        {
            if (jumpInput && IsGrounded && !IsJumping)
            {
                IsJumping = true;
                vy += Cfg.JumpSpeed;
                jumpVel = Cfg.JumpSpeed;
                timeSinceJump = 0f;
            }
            else if (IsJumping)
            {
                timeSinceJump += Cfg.Dt;
                if (!jumpInput && timeSinceJump < maxJumpDuration)
                    vy = StopJump(vy);
                else if (vy <= 0f)
                {
                    IsJumping = false;
                    climbedSinceJumped = false;
                }
            }
        }

        // climbing: touching a Climbable surface; jump climbs, otherwise slide down slowly
        bool wasClimbing = IsClimbing;
        IsClimbing = Probe(x, y, (Cfg.ColliderW + 0.3f) * 0.5f, Cfg.ColliderH * 0.3f, climbMask);
        if (IsClimbing)
        {
            climbedSinceJumped = true;
            if (jumpInput)
            {
                IsJumping = true;
                vy = Cfg.ClimbSpeed;
                timeSinceJump = 0f;
            }
            else
                vy = -Cfg.ClimbFallSpeed;
        }
        else if (wasClimbing && jumpInput)
            vy = Cfg.ClimbSpeed;                     // pop over the lip

        // gravity is applied here (the body has GravityScale 0) so climbing can switch it off
        if (!IsClimbing) vy += Cfg.Gravity * Cfg.Dt;
        jumpVel += Cfg.Gravity * Cfg.Dt;
        jumpVel *= 1f - Cfg.LinearDamping * Cfg.Dt;
        body.SetVelocity(vx, vy);
        Shared.PlayerGrounded = IsGrounded;
        Shared.PlayerClimbing = IsClimbing;
        Shared.PlayerJumping = IsJumping;
        Shared.PlayerFacing = Facing;
        float cx = node.WorldX();
        float cy = node.WorldY();
        Shared.PlayerLastMoveX = cx - prevX;
        Shared.PlayerLastMoveY = cy - prevY;
        prevX = cx;
        prevY = cy;
    }

    float StopJump(float vy)
    {
        if (climbedSinceJumped) vy = 0f;
        else vy -= jumpVel;
        IsJumping = false;
        climbedSinceJumped = false;
        jumpVel = 0f;
        return vy;
    }

    void Death()
    {
        Shared.DeathRequested = false;
        Shared.Deaths++;
        Console.WriteLine("death " + Shared.Deaths + " step=" + scene.FixedIndex + " x*1000=" + (int)(node.WorldX() * 1000f) + " y*1000=" + (int)(node.WorldY() * 1000f));
        lockTimer = Cfg.RespawnDelay;
        IsJumping = false;
        IsClimbing = false;
        climbedSinceJumped = false;
        jumpVel = 0f;
        Shared.LassoDetach();
        node.SetPosition(Shared.SaveX, Shared.SaveY);
        body.SetVelocity(0f, 0f);
        prevX = Shared.SaveX;
        prevY = Shared.SaveY;
        Shared.ResetWorld();
    }
}
'''

SUBSET_ENEMY_CS = r'''
using System;
using @NS@;
using @PB2NS@;

// Ported from the Unity Enemy: patrol -> (vision cone + line of sight) -> chase -> attack, with
// hit reaction and hp. One script serves both kinds (EnemyCfg): worm = ground walker that spits,
// bat = flyer that hurts by contact. Differences: vision is evaluated directly (no sensor
// trigger), "blocked" uses a ray (no Rigidbody2D.Cast), randomness is Shared.Rand01, and a dead
// enemy is deactivated and brought back by Shared.ResetEnemies when the player respawns.
[Script(Order = 5), MaxInstances(24)]
class EnemyScript
{
    public Component Self;
    public int Index;
    public int Kind;
    public float InitX;
    public float InitY;
    Scene2D scene;
    Node node;
    Rigidbody2D body;
    uint wallMask;
    uint visionMask;
    bool started;
    float hp;
    bool chase;
    float destX;
    float destY;
    float stopTimeRemaining;
    bool prevAtDest;
    float prevToX;
    float prevToY;
    float hurtTimer;
    float faceX;
    float faceY;
    float shootTimer;
    float ex;
    float ey;

    public void Start()
    {
        scene = Scene2D.Current;
        node = Self.Node;
        body = scene.Find(node, ComponentKind.Rigidbody2D).Body;
        wallMask = (1u << Layers.Wall) | (1u << Layers.Climbable);
        visionMask = wallMask | (1u << Layers.Player);
        started = true;
        Restart();
    }

    // Brought back to life (SetActive(true)): back to the start state.
    public void OnEnable()
    {
        if (started) Restart();
    }

    void Restart()
    {
        hp = EnemyCfg.Hp(Kind);
        chase = false;
        hurtTimer = 0f;
        shootTimer = EnemyCfg.ShootInterval(Kind);
        faceX = 1f;
        faceY = 0f;
        destX = InitX;
        destY = InitY;
        if (!EnemyCfg.IsFlying(Kind)) destY = InitY - EnemyCfg.ColliderH(Kind) * 0.5f;
        prevAtDest = false;
        prevToX = 0f;
        prevToY = 0f;
        stopTimeRemaining = 0f;
    }

    public void FixedUpdate()
    {
        if (Shared.EnemyResetFlag(Index))
        {
            Shared.ClearEnemyReset(Index);
            node.SetPosition(InitX, InitY + 0.01f);
            body.SetVelocity(0f, 0f);
            Restart();
        }
        ex = node.WorldX();
        ey = node.WorldY();
        float dmg = Shared.TakeEnemyDamage(Index);
        if (dmg > 0f)
        {
            hp -= dmg;
            if (hp <= 0f)
            {
                Shared.SetEnemyAlive(Index, false);
                scene.SetActive(node, false);
                return;
            }
            if (!chase)
            {
                // turn toward whatever hit us for a moment
                faceX = Shared.EnemyHitDX(Index);
                faceY = Shared.EnemyHitDY(Index);
                if (!EnemyCfg.IsFlying(Kind)) { faceX = faceX > 0f ? 1f : -1f; faceY = 0f; }
                hurtTimer = EnemyCfg.LookToHurt(Kind);
            }
        }
        if (Shared.PlayerNode != null)
        {
            HandleMoving();
            if (chase) HandleAttacking();
        }
        Shared.PublishEnemy(Index, ex, ey, faceX, chase);
    }

    // touching an enemy hurts (the Unity enemies hurt through their weapon)
    public void OnCollisionBegin2D(Collision2D hit)
    {
        if (hit.Other.Self.Node.Tag == Shared.TagPlayer) Shared.DeathRequested = true;
    }

    static float Angle(float ax, float ay, float bx, float by)
    {
        float la = MathF.Sqrt(ax * ax + ay * ay);
        float lb = MathF.Sqrt(bx * bx + by * by);
        if (la < 0.0001f || lb < 0.0001f) return 0f;
        float c = (ax * bx + ay * by) / (la * lb);
        if (c > 1f) c = 1f;
        if (c < -1f) c = -1f;
        return MathF.Acos(c) * 57.29578f;
    }

    // Rays to the player's centre and four bounds corners: true if any reaches the player first.
    bool CanSeePlayer(float fx, float fy, float range)
    {
        float px = Shared.PlayerNode.WorldX();
        float py = Shared.PlayerNode.WorldY();
        float hw = Cfg.ColliderW * 0.5f;
        float hh = Cfg.ColliderH * 0.5f;
        for (int k = 0; k < 5; k++)
        {
            float tx = px;
            float ty = py;
            if (k == 1) { tx = px - hw; ty = py - hh; }
            if (k == 2) { tx = px + hw; ty = py + hh; }
            if (k == 3) { tx = px - hw; ty = py + hh; }
            if (k == 4) { tx = px + hw; ty = py - hh; }
            float dx = tx - fx;
            float dy = ty - fy;
            float d = MathF.Sqrt(dx * dx + dy * dy);
            if (d < 0.0001f) return true;
            if (scene.Physics.Sim.Raycast(fx, fy, dx / d, dy / d, range, visionMask, false) == Shared.PlayerColliderIndex) return true;
        }
        return false;
    }

    bool CheckVision()
    {
        float px = Shared.PlayerNode.WorldX();
        float py = Shared.PlayerNode.WorldY();
        float dx = px - ex;
        float dy = py - ey;
        float range = EnemyCfg.VisionRange(Kind);
        float hw = (Cfg.ColliderW + EnemyCfg.ColliderW(Kind)) * 0.5f;
        float hh = (Cfg.ColliderH + EnemyCfg.ColliderH(Kind)) * 0.5f;
        if (MathF.Abs(dx) < hw && MathF.Abs(dy) < hh) return true;              // touching
        if (MathF.Sqrt(dx * dx + dy * dy) > range + 0.5f) return false;          // outside the vision circle
        if (Angle(faceX, faceY, dx, dy) > EnemyCfg.VisionAngle(Kind)) return false;
        return CanSeePlayer(ex, ey, range);
    }

    bool Blocked(float dx, float dy)
    {
        float m = MathF.Sqrt(dx * dx + dy * dy);
        if (m < 0.0001f) return false;
        int hit = scene.Physics.Sim.Raycast(ex, ey, dx / m, dy / m, m + EnemyCfg.ColliderW(Kind) * 0.5f, wallMask, false);
        if (hit < 0) return false;
        if (EnemyCfg.IsFlying(Kind)) return true;
        return scene.Physics.Sim.HitNY < 0.5f;                                   // ignore the floor we stand on
    }

    void Move(float mx, float my)
    {
        float len = MathF.Sqrt(mx * mx + my * my);
        if (len > 1f) { mx /= len; my /= len; }
        float speed = EnemyCfg.MoveSpeed(Kind);
        if (EnemyCfg.IsFlying(Kind))
        {
            body.SetVelocity(mx * speed, my * speed);
            if (mx != 0f || my != 0f) { faceX = mx; faceY = my; }
        }
        else
        {
            float sgn = 0f;
            if (mx > 0f) sgn = 1f;
            if (mx < 0f) sgn = -1f;
            body.SetVelocity(sgn * speed, body.VelocityY());
            if (sgn != 0f) { faceX = sgn; faceY = 0f; }
        }
    }

    void HandleMoving()
    {
        bool flying = EnemyCfg.IsFlying(Kind);
        float halfH = EnemyCfg.ColliderH(Kind) * 0.5f;
        if (chase)
        {
            float tx = Shared.PlayerNode.WorldX() - ex;
            float ty = Shared.PlayerNode.WorldY() - ey;
            float dist = MathF.Sqrt(tx * tx + ty * ty);
            if (dist >= EnemyCfg.ChaseStopMin(Kind) && dist <= EnemyCfg.ChaseStopMax(Kind)) Move(0f, 0f);
            else if (dist < EnemyCfg.ChaseStopMin(Kind)) Move(-tx, -ty);          // too close: back off
            else Move(tx, ty);
            // lost line of sight: stop chasing
            if (!CanSeePlayer(ex, ey, 200f)) chase = false;
            return;
        }
        if (CheckVision())
        {
            chase = true;
            hurtTimer = 0f;
            return;
        }
        if (hurtTimer > 0f)
        {
            hurtTimer -= Cfg.Dt;
            Move(0f, 0f);
            return;
        }
        float footY = ey - halfH;
        float toX = destX - ex;
        float toY = destY - ey;
        if (!flying) toY = destY - footY;
        float stop = EnemyCfg.PatrolStopDist(Kind);
        bool atDest = toX * toX + toY * toY <= stop * stop;
        if (atDest)
        {
            if (!prevAtDest) stopTimeRemaining = EnemyCfg.PatrolStopMin(Kind) + Shared.Rand01() * (EnemyCfg.PatrolStopMax(Kind) - EnemyCfg.PatrolStopMin(Kind));
            else
            {
                stopTimeRemaining -= Cfg.Dt;
                if (stopTimeRemaining <= 0f) PickDestination(flying, footY);
            }
            Move(0f, 0f);
        }
        else
            Move(toX, toY);
        prevAtDest = atDest;
    }

    // An unobstructed destination that turns us around by at least the configured angle
    // (the Unity code loops until it finds one; bounded here).
    void PickDestination(bool flying, float footY)
    {
        float toX = 0f;
        float toY = 0f;
        float range = EnemyCfg.PatrolRange(Kind);
        for (int tries = 0; tries < 8; tries++)
        {
            if (flying)
            {
                float a = Shared.Rand01() * 6.2831853f;
                destX = InitX + MathF.Cos(a) * range;
                destY = InitY + MathF.Sin(a) * range;
                toX = destX - ex;
                toY = destY - ey;
            }
            else
            {
                destX = InitX + (Shared.Rand01() * 2f - 1f) * range;
                toX = destX - ex;
                toY = destY - footY;
            }
            float minAngle = EnemyCfg.MinPatrolAngle(Kind);
            bool turned = true;
            if (prevToX != 0f || prevToY != 0f) turned = Angle(toX, toY, prevToX, prevToY) >= minAngle;
            if (!turned) continue;
            bool blocked = Blocked(toX, toY);
            if (!blocked) break;
        }
        prevToX = toX;
        prevToY = toY;
    }

    void HandleAttacking()
    {
        shootTimer -= Cfg.Dt;
        float interval = EnemyCfg.ShootInterval(Kind);
        if (interval <= 0f || shootTimer > 0f) return;
        float tx = Shared.PlayerNode.WorldX() - ex;
        float ty = Shared.PlayerNode.WorldY() - ey;
        float d = MathF.Sqrt(tx * tx + ty * ty);
        if (d >= EnemyCfg.AttackMin(Kind) && d <= EnemyCfg.AttackMax(Kind) && d > 0.001f)
        {
            shootTimer = interval;
            float sp = EnemyCfg.BulletSpeed(Kind);
            BulletScript.Spawn(ex, ey, tx / d * sp, ty / d * sp, EnemyCfg.BulletDamage(Kind), EnemyCfg.BulletLifetime(Kind), false);
        }
    }
}

// A bullet is just a node that moves itself: each step it rays ahead for walls and overlaps its
// targets (no rigidbody or trigger events, so it works against static geometry and uses no
// physics slots). Player bullets hurt enemies, enemy bullets hurt the player.
[Script(Order = 20), MaxInstances(48)]
class BulletScript
{
    public Component Self;
    float vx;
    float vy;
    float damage;
    float life;
    bool fromPlayer;
    Scene2D scene;
    Node node;
    uint wallMask;
    uint targetMask;

    public static void Spawn(float x, float y, float vx, float vy, float damage, float life, bool fromPlayer)
    {
        int tag = Shared.TagBulletEnemy;
        if (fromPlayer) tag = Shared.TagBulletPlayer;
        SpawnTagged(x, y, vx, vy, damage, life, fromPlayer, tag);
    }

    // An arrow from a shooter trap: an enemy bullet that is drawn as an arrow.
    public static void SpawnArrow(float x, float y, float vx, float vy, float damage, float life)
    {
        SpawnTagged(x, y, vx, vy, damage, life, false, Shared.TagArrow);
    }

    static void SpawnTagged(float x, float y, float vx, float vy, float damage, float life, bool fromPlayer, int tag)
    {
        Scene2D sc = Scene2D.Current;
        Node n = sc.NewNode(null);
        if (n == null) return;
        n.SetPosition(x, y);
        n.SetAngle(MathF.Atan2(vy, vx));                // the renderer draws the bullet along its flight
        n.Tag = tag;
        BulletScript b = Scripts.AddBulletScript(n);
        if (b == null)
        {
            sc.Destroy(n);
            return;
        }
        b.vx = vx;
        b.vy = vy;
        b.damage = damage;
        b.life = life;
        b.fromPlayer = fromPlayer;
    }

    public void Start()
    {
        scene = Scene2D.Current;
        node = Self.Node;
        wallMask = (1u << Layers.Wall) | (1u << Layers.Climbable);
        if (fromPlayer) targetMask = 1u << Layers.Enemy;
        else targetMask = 1u << Layers.Player;
    }

    public void FixedUpdate()
    {
        float x = node.WorldX();
        float y = node.WorldY();
        float stepX = vx * Cfg.Dt;
        float stepY = vy * Cfg.Dt;
        float step = MathF.Sqrt(stepX * stepX + stepY * stepY);
        life -= Cfg.Dt;
        if (life <= 0f || step < 0.00001f)
        {
            scene.Destroy(node);
            return;
        }
        if (scene.Physics.Sim.Raycast(x, y, stepX / step, stepY / step, step + 0.15f, wallMask, false) >= 0)
        {
            scene.Destroy(node);
            return;
        }
        x += stepX;
        y += stepY;
        node.SetPosition(x, y);
        if (scene.Physics.Sim.OverlapCircle(x, y, 0.2f, targetMask, false) > 0)
        {
            Collider2D c = scene.Physics.ColliderByIndex(scene.Physics.Sim.OverlapResult(0));
            if (c != null)
            {
                int tag = c.Self.Node.Tag;
                if (tag == Shared.TagPlayer) Shared.DeathRequested = true;
                else if (tag >= Shared.TagEnemyBase) Shared.DamageEnemy(tag - Shared.TagEnemyBase, damage, vx, vy);
            }
            scene.Destroy(node);
        }
    }
}
'''

SUBSET_LASSO_CS = r'''
using System;
using @NS@;
using @PB2NS@;

// Ported from the Unity Lasso: right-click shoots a hook toward the cursor; it flies (carried along
// with the player, as in the original) until it hits a Wall/Climbable surface, then the player swings
// (see PlayerScript). While attached W/S reel the rope in/out; letting go of the button releases it and
// keeps the swing's momentum. Dropped: moving anchors (walls here are static), Slippery walls.
[Script(Order = 11), MaxInstances(1)]
class LassoScript
{
    public Component Self;
    Scene2D scene;
    Node node;
    Rigidbody2D body;
    uint wallMask;
    bool prevInput;

    public void Start()
    {
        scene = Scene2D.Current;
        node = Self.Node;
        body = scene.Find(node, ComponentKind.Rigidbody2D).Body;
        wallMask = (1u << Layers.Wall) | (1u << Layers.Climbable);
    }

    public void FixedUpdate()
    {
        float px = node.WorldX();
        float py = node.WorldY();
        bool input = InputState.Lasso;
        if (input && !prevInput)
        {
            float ax = InputState.AimX - px;
            float ay = InputState.AimY - py;
            float al = MathF.Sqrt(ax * ax + ay * ay);
            if (al < 0.001f) { ax = 0f; ay = 1f; al = 1f; }
            Shared.LassoDirX = ax / al;
            Shared.LassoDirY = ay / al;
            Shared.LassoOffX = 0f;
            Shared.LassoOffY = 0f;
            Shared.LassoTipX = px;
            Shared.LassoTipY = py;
            Shared.LassoLength = 0f;
            Shared.LassoAttached = false;
            Shared.LassoActive = true;
        }
        else if (!input && prevInput && Shared.LassoActive)
            Release();
        if (Shared.LassoActive)
        {
            if (!Shared.LassoAttached) Fly(px, py);
            else Reel(px, py);
        }
        prevInput = input;
    }

    // The tip advances relative to the player; a surface along this step's path attaches it.
    void Fly(float px, float py)
    {
        float endX = Shared.LassoOffX;
        float endY = Shared.LassoOffY;
        float dx = Shared.LassoDirX;
        float dy = Shared.LassoDirY;
        float toNew = Cfg.LassoShootSpeed * Cfg.Dt;
        float lengthRemaining = Cfg.LassoMaxLength - MathF.Sqrt(endX * endX + endY * endY);
        if (toNew < lengthRemaining) lengthRemaining = toNew;
        int hit = -1;
        if (lengthRemaining > 0f) hit = scene.Physics.Sim.Raycast(px + endX, py + endY, dx, dy, lengthRemaining, wallMask, false);
        if (hit >= 0)
        {
            float hx = scene.Physics.Sim.HitX;
            float hy = scene.Physics.Sim.HitY;
            float toX = hx - px;
            float toY = hy - py;
            float len = MathF.Sqrt(toX * toX + toY * toY);
            float a = MathF.Atan2(toY, toX);
            Shared.LassoTipX = hx;
            Shared.LassoTipY = hy;
            Shared.LassoLength = len;
            Shared.SwingW = 0f;
            if (len > 0.0001f) Shared.SwingW = (body.VelocityX() * MathF.Cos(a) + body.VelocityY() * MathF.Sin(a)) / len;
            Shared.SwingAngle = a * 57.29578f;
            Shared.LassoAttached = true;
        }
        else
        {
            endX += dx * toNew;
            endY += dy * toNew;
            Shared.LassoOffX = endX;
            Shared.LassoOffY = endY;
            Shared.LassoTipX = px + endX;
            Shared.LassoTipY = py + endY;
            Shared.LassoLength = MathF.Sqrt(endX * endX + endY * endY);
            if (lengthRemaining <= 0f) Release();
        }
    }

    void Reel(float px, float py)
    {
        int change = InputState.ChangeLassoLength;
        if (change == 0) return;
        float amount = change * Cfg.LassoChangeLengthSpeed * Cfg.Dt;
        bool allowed = true;
        if (change < 0)
        {
            // reeling in is blocked if it would pull the player into a wall
            float toX = Shared.LassoTipX - px;
            float toY = Shared.LassoTipY - py;
            float d = MathF.Sqrt(toX * toX + toY * toY);
            if (d > 0.001f && scene.Physics.Sim.Raycast(px, py, toX / d, toY / d, MathF.Abs(amount) * 6f + Cfg.ColliderW * 0.5f, wallMask, false) >= 0)
                allowed = false;
        }
        if (!allowed) return;
        float len = Shared.LassoLength + amount;
        if (len < 0f) len = 0f;
        if (len > Cfg.LassoMaxLength) len = Cfg.LassoMaxLength;
        Shared.LassoLength = len;
    }

    // Letting go keeps the momentum of the swing: the displacement over the last step.
    void Release()
    {
        Shared.LassoActive = false;
        if (Shared.LassoAttached)
        {
            Shared.LassoAttached = false;
            body.SetVelocity(Shared.PlayerLastMoveX / Cfg.Dt, Shared.PlayerLastMoveY / Cfg.Dt);
        }
    }
}
'''

SUBSET_TRAPS_CS = r'''
using System;
using @NS@;
using @PB2NS@;

// Ported from the original DissolveOnHit (Crumbly Wall prefab): the first collision starts a timer;
// the wall fades over CrumblyDissolveTime and then deactivates. It comes back on respawn.
[Script(Order = 6), MaxInstances(96)]
class CrumblyScript
{
    public Component Self;
    public int Index;
    Scene2D scene;
    Node node;
    bool hit;
    float timer;

    public void Start()
    {
        scene = Scene2D.Current;
        node = Self.Node;
    }

    public void OnCollisionBegin2D(Collision2D contact)
    {
        if (hit) return;
        hit = true;
        timer = Cfg.CrumblyDissolveTime;
    }

    public void FixedUpdate()
    {
        if (Shared.CrumblyResetFlag(Index))
        {
            Shared.ClearCrumblyReset(Index);
            hit = false;
            timer = 0f;
        }
        if (!hit) return;
        timer -= Cfg.Dt;
        if (timer <= 0f)
        {
            Shared.SetCrumblyAlpha(Index, 0f);
            scene.SetActive(node, false);
        }
        else
            Shared.SetCrumblyAlpha(Index, timer / Cfg.CrumblyDissolveTime);
    }
}

// Ported from the original ShooterTrap + Arrow Shooter prefab: every step it casts a ray along its
// facing; when the first thing hit is the player and a second has passed since the last shot (the
// 1s shoot animation) it fires an arrow ("Aim Where Facing": one arrow along its facing).
[Script(Order = 7), MaxInstances(32)]
class ShooterScript
{
    public Component Self;
    public float DirX;
    public float DirY;
    Scene2D scene;
    Node node;
    uint mask;
    float sinceShot;

    public void Start()
    {
        scene = Scene2D.Current;
        node = Self.Node;
        mask = (1u << Layers.Wall) | (1u << Layers.Climbable) | (1u << Layers.Player);
        sinceShot = Cfg.ShooterCooldown;
    }

    public void FixedUpdate()
    {
        sinceShot += Cfg.Dt;
        if (Shared.DeathRequested) sinceShot = Cfg.ShooterCooldown;      // ready again after a respawn
        // start just outside our own tile, which is solid
        float ox = node.WorldX() + DirX * 0.55f;
        float oy = node.WorldY() + DirY * 0.55f;
        int hit = scene.Physics.Sim.Raycast(ox, oy, DirX, DirY, 500f, mask, false);
        if (hit == Shared.PlayerColliderIndex && sinceShot >= Cfg.ShooterCooldown)
        {
            sinceShot = 0f;
            BulletScript.SpawnArrow(ox, oy, DirX * Cfg.ArrowSpeed, DirY * Cfg.ArrowSpeed, Cfg.ArrowDamage, Cfg.ArrowLifetime);
        }
    }
}
'''

SUBSET_OBJECTS_CS = r'''using System;
using @NS@;
using @PB2NS@;

// Spikes: touching the player kills.
[Script, MaxInstances(64)]
class HazardScript
{
    public Component Self;
    public void OnTriggerEnter2D(Collider2D other)
    {
        if (other.Self.Node.Tag == Shared.TagPlayer) Shared.DeathRequested = true;
    }
}

// Savepoint: stores the respawn position and makes picked-up gems permanent.
[Script, MaxInstances(32)]
class SavePointScript
{
    public Component Self;
    public float X;
    public float Y;
    public int Index;
    public void OnTriggerEnter2D(Collider2D other)
    {
        if (other.Self.Node.Tag != Shared.TagPlayer) return;
        Shared.TouchSave(Index);
        Shared.SaveX = X;
        Shared.SaveY = Y;
        Shared.CommitGems();
    }
}

// Gem: picking it up hides it; it only counts once a savepoint is touched.
[Script, MaxInstances(64)]
class GemScript
{
    public Component Self;
    public int Index;
    public void OnTriggerEnter2D(Collider2D other)
    {
        if (other.Self.Node.Tag != Shared.TagPlayer) return;
        Shared.PickUpGem(Index);
    }
}

// Goal: reaching it wins.
[Script, MaxInstances(1)]
class GoalScript
{
    public Component Self;
    public void OnTriggerEnter2D(Collider2D other)
    {
        if (other.Self.Node.Tag == Shared.TagPlayer) Shared.Won = true;
    }
}
'''

SUBSET_BOT_CS = r'''using System;
using @NS@;
using @PB2NS@;

// A reactive test driver standing in for a keyboard and mouse: runs toward the goal, jumps walls
// and gaps, holds jump to climb, stops to shoot any enemy it can see nearby, and swings on the
// level's anchor hints to cross gaps a jump cannot. It uses the same probes as the player.
[Script(Order = 0), MaxInstances(1)]
class BotScript
{
    public Component Self;
    Scene2D scene;
    uint solidMask;
    float releaseAngle;

    public void Start()
    {
        scene = Scene2D.Current;
        solidMask = (1u << Layers.Wall) | (1u << Layers.Climbable);
        releaseAngle = @RELEASE_ANGLE@;
    }

    bool Probe(float cx, float cy, float halfW, float halfH)
    {
        return scene.Physics.Sim.OverlapBox(cx, cy, halfW, halfH, 0f, solidMask, false) > 0;
    }

    public void FixedUpdate()
    {
        Node pn = Shared.PlayerNode;
        if (pn == null) return;
        float x = pn.WorldX();
        float y = pn.WorldY();
        float halfW = Cfg.ColliderW * 0.5f;
        float halfH = Cfg.ColliderH * 0.5f;

        // the nearest living enemy that is close and not behind a wall
        bool engage = false;
        float bestD = 11f;
        float aimX = 0f;
        float aimY = 0f;
        for (int i = 0; i < Shared.EnemyCount(); i++)
        {
            if (!Shared.EnemyAlive(i)) continue;
            float dx = Shared.EnemyX(i) - x;
            float dy = Shared.EnemyY(i) - y;
            float d = MathF.Sqrt(dx * dx + dy * dy);
            if (d >= bestD || d < 0.001f) continue;
            if (scene.Physics.Sim.Raycast(x, y, dx / d, dy / d, d, solidMask, false) >= 0) continue;
            bestD = d;
            aimX = Shared.EnemyX(i);
            aimY = Shared.EnemyY(i);
            engage = true;
        }
        InputState.Attack = engage;
        InputState.AimX = aimX;
        InputState.AimY = aimY;

        float dir = 0f;
        if (Level.GoalX() - x > 0.3f) dir = 1f;
        else if (Level.GoalX() - x < -0.3f) dir = -1f;
        // stand still to shoot (unless climbing or in the air)
        if (engage && Shared.PlayerGrounded && !Shared.PlayerClimbing) dir = 0f;

        // lasso: hook the nearest anchor hint ahead while airborne; let go once swung far enough forward
        bool lasso = false;
        if (Shared.LassoAttached)
        {
            lasso = true;
            float rx = x - Shared.LassoTipX;
            float ry = Shared.LassoTipY - y;
            float forward = MathF.Atan2(rx, ry) * 57.29578f;      // degrees ahead of straight down
            if (forward >= releaseAngle) lasso = false;
        }
        else if (!Shared.PlayerGrounded && !Shared.PlayerClimbing && dir > 0f)
        {
            float bestA = 6.5f;
            for (int i = 0; i < Level.AnchorCount; i++)
            {
                float ax = Level.Anchor(i, 0);
                float ay = Level.Anchor(i, 1);
                if (ax < x + 1.5f || ay < y + 1f) continue;
                float dx = ax - x;
                float dy = ay - y;
                float d = MathF.Sqrt(dx * dx + dy * dy);
                if (d >= bestA) continue;
                bestA = d;
                lasso = true;
                InputState.AimX = ax;
                InputState.AimY = ay + 4f;        // aim well up into the ceiling: the hook rides along with a moving player
            }
        }
        InputState.Lasso = lasso;
        InputState.ChangeLassoLength = 0;

        bool jump = false;
        if (Shared.LassoAttached)
            jump = false;
        else if (Shared.PlayerClimbing)
            jump = true;                                   // hold to climb
        else if (Shared.PlayerGrounded)
        {
            bool wallAhead = Probe(x + dir * (halfW + 0.25f), y, 0.15f, halfH * 0.6f);
            bool floorAhead = Probe(x + dir * (halfW + 0.2f), y - halfH - 0.15f, 0.1f, 0.12f);
            if (dir != 0f && (wallAhead || !floorAhead)) jump = true;
        }
        else if (Shared.PlayerJumping)
            jump = true;                                   // full-height jumps
        // an arrow coming straight at us along the floor: jump it
        if (Shared.PlayerGrounded && !Shared.PlayerClimbing && !Shared.LassoAttached)
        {
            for (int i = 0; i < scene.NodeHighWater; i++)
            {
                Node a = scene.NodeAt(i);
                if (a == null || !a.Alive || a.Destroyed || a.Tag != Shared.TagArrow) continue;
                float adx = a.WorldX() - x;
                float ady = a.WorldY() - y;
                if (MathF.Abs(ady) < 0.9f && MathF.Abs(adx) < 6.5f && adx * MathF.Cos(a.WorldAngle()) < 0f) jump = true;
            }
        }
        InputState.Move = dir;
        InputState.Jump = jump;
    }
}
'''

SUBSET_GAME_CS = r'''using System;
using @NS@;
using @PB2NS@;

static class Game
{
    const int MaxFrames = @MAX_FRAMES@;

    static int Milli(float v) { return (int)(v * 1000f); }

    static Collider2D MakeCollider(Scene2D scene, Node n, float w, float h, bool trigger, float offY)
    {
        Collider2D c = scene.NewBoxCollider(n, w, h);
        c.Friction = 0f;
        c.IsTrigger = trigger;
        c.OffsetY = offY;
        scene.Finish(c.Self);
        return c;
    }

    static Node MakeBox(Scene2D scene, float x, float y, float w, float h, int layer, int tag, bool trigger, float offY)
    {
        Node n = scene.NewNode(null);
        n.SetPosition(x, y);
        n.Layer = layer;
        n.Tag = tag;
        MakeCollider(scene, n, w, h, trigger, offY);
        return n;
    }

    public static int Main()
    {
        Scripts.Init();
        Scene2D scene = new Scene2D();
        scene.FixedDeltaTime = Cfg.Dt;
        scene.SetGravity(0f, Cfg.Gravity);
        Level.Init();
        uint[] layerRows = Level.LayerRows();
        scene.Physics.Sim.SetLayerMatrix(layerRows);

        for (int i = 0; i < Level.WallCount; i++)
            MakeBox(scene, Level.Wall(i, 0) + Level.Wall(i, 2) * 0.5f, Level.Wall(i, 1) + Level.Wall(i, 3) * 0.5f,
                    Level.Wall(i, 2), Level.Wall(i, 3), Layers.Wall, Shared.TagWall, false, 0f);
        for (int i = 0; i < Level.ClimbCount; i++)
            MakeBox(scene, Level.Climb(i, 0) + Level.Climb(i, 2) * 0.5f, Level.Climb(i, 1) + Level.Climb(i, 3) * 0.5f,
                    Level.Climb(i, 2), Level.Climb(i, 3), Layers.Climbable, Shared.TagWall, false, 0f);
        for (int i = 0; i < Level.SpikeCount; i++)
        {
            float sw = Level.Spike(i, 2);
            float sh = Level.Spike(i, 3);
            Node n = MakeBox(scene, Level.Spike(i, 0) + sw * 0.5f, Level.Spike(i, 1) + sh * 0.5f, sw, sh * 0.6f, Layers.Hazard, Shared.TagHazard, true, -sh * 0.2f);
            Scripts.AddHazardScript(n);
        }
        Shared.InitSaves(Level.SaveCount);
        for (int i = 0; i < Level.SaveCount; i++)
        {
            float sx = Level.Save(i, 0);
            float sy = Level.Save(i, 1);
            Node n = MakeBox(scene, sx, sy, 1.2f, 1.4f, Layers.Gem, Shared.TagSavePoint, true, 0.7f);
            SavePointScript sp = Scripts.AddSavePointScript(n);
            sp.X = sx;
            sp.Y = sy + Cfg.ColliderH * 0.5f + 0.2f;
            sp.Index = i;
        }
        Shared.InitGems(Level.GemCount);
        for (int i = 0; i < Level.GemCount; i++)
        {
            Node n = scene.NewNode(null);
            n.SetPosition(Level.Gem(i, 0), Level.Gem(i, 1));
            n.Layer = Layers.Gem;
            n.Tag = Shared.TagGem;
            Collider2D gc = scene.NewCircleCollider(n, 0.45f);
            gc.IsTrigger = true;
            scene.Finish(gc.Self);
            GemScript gs = Scripts.AddGemScript(n);
            gs.Index = i;
            Shared.RegisterGem(i, n);
        }
        Shared.InitCrumbly(Level.CrumblyCount);
        for (int i = 0; i < Level.CrumblyCount; i++)
        {
            // each collider is 0.04 wider than its tile, so neighbours overlap: with exactly flush boxes the slime catches on the seam (a ghost collision)
            Node cn = MakeBox(scene, Level.CrumblyTile(i, 0) + 0.5f, Level.CrumblyTile(i, 1) + 0.5f, Level.CrumblyTile(i, 2) + 0.04f, Level.CrumblyTile(i, 3), Layers.Wall, Shared.TagWall, false, 0f);
            CrumblyScript cs = Scripts.AddCrumblyScript(cn);
            cs.Index = i;
            Shared.RegisterCrumbly(i, cn);
        }
        for (int i = 0; i < Level.ShooterCount; i++)
        {
            // the shooter's tile is already part of the walls: this node only carries the trap
            Node sn = scene.NewNode(null);
            sn.SetPosition(Level.Shooter(i, 0), Level.Shooter(i, 1));
            ShooterScript ss = Scripts.AddShooterScript(sn);
            ss.DirX = Level.Shooter(i, 2);
            ss.DirY = Level.Shooter(i, 3);
        }
        Shared.InitEnemies(Level.EnemyCount);
        for (int i = 0; i < Level.EnemyCount; i++)
        {
            int kind = Level.EnemyKind(i);
            float ex = Level.EnemyX(i);
            float ey = Level.EnemyY(i);
            Node en = scene.NewNode(null);
            en.SetPosition(ex, ey);
            en.Layer = Layers.Enemy;
            en.Tag = Shared.TagEnemyBase + i;
            Rigidbody2D erb = scene.NewRigidbody(en, PB2.BodyDynamic);
            erb.GravityScale = EnemyCfg.GravityScale(kind);
            erb.LinearDamping = 0f;
            erb.FreezeRotation = true;
            erb.CanSleep = false;
            scene.Finish(erb.Self);
            MakeCollider(scene, en, EnemyCfg.ColliderW(kind), EnemyCfg.ColliderH(kind), false, 0f);
            EnemyScript es = Scripts.AddEnemyScript(en);
            es.Index = i;
            es.Kind = kind;
            es.InitX = ex;
            es.InitY = ey;
            Shared.RegisterEnemy(i, en, kind, ex, ey);
        }
        Node goal = MakeBox(scene, Level.GoalX(), Level.GoalY(), 1.4f, 2f, Layers.Gem, Shared.TagGoal, true, 0f);
        Scripts.AddGoalScript(goal);

        // the player: a dynamic box; gravity is applied by PlayerScript
        Node player = scene.NewNode(null);
        player.SetPosition(Level.SpawnX(), Level.SpawnY());
        player.Layer = Layers.Player;
        player.Tag = Shared.TagPlayer;
        Rigidbody2D rb = scene.NewRigidbody(player, PB2.BodyDynamic);
        rb.GravityScale = 0f;
        rb.LinearDamping = Cfg.LinearDamping;
        rb.FreezeRotation = true;
        rb.IsBullet = true;
        rb.CanSleep = false;
        scene.Finish(rb.Self);
        Collider2D playerCol = MakeCollider(scene, player, Cfg.ColliderW, Cfg.ColliderH, false, 0f);
        Shared.PlayerColliderIndex = playerCol.ColliderIndex;
        Scripts.AddPlayerScript(player);
        Scripts.AddLassoScript(player);
        Scripts.AddBotScript(player);

@RENDER_INIT@
        int wonFrame = -1;
        int firstClimb = -1;
        int firstJump = -1;
        float maxX = Level.SpawnX();
        float maxY = Level.SpawnY();
        for (int frame = 0; frame < MaxFrames && !Shared.Won; frame++)
        {
            Scripts.Tick(scene, Cfg.Dt);
            float px = player.WorldX();
            float py = player.WorldY();
            if (px > maxX) maxX = px;
            if (py > maxY) maxY = py;
            if (Shared.PlayerClimbing && firstClimb < 0) firstClimb = frame;
            if (Shared.PlayerJumping && firstJump < 0) firstJump = frame;
            if (Shared.LassoActive && frame % 10 == 0)
            {
                int att = 0;
                if (Shared.LassoAttached) att = 1;
                Console.WriteLine("lasso t=" + frame + " attached=" + att + " len*1000=" + Milli(Shared.LassoLength) + " x*1000=" + Milli(px) + " y*1000=" + Milli(py));
            }
            if (frame % 100 == 0)
            {
                string line = "t=" + (frame / 100) + "s x*1000=" + Milli(px) + " y*1000=" + Milli(py);
                Console.WriteLine(line);
            }
            if (Shared.Won) wonFrame = frame;
@RENDER_FRAME@
        }
@RENDER_END@
        int won = 0;
        if (Shared.Won) won = 1;
        int slain = 0;
        for (int i = 0; i < Shared.EnemyCount(); i++)
            if (!Shared.EnemyAlive(i)) slain++;
        Console.WriteLine("won=" + won + " frame=" + wonFrame + " deaths=" + Shared.Deaths + " gems=" + Shared.Gems + " enemies=" + Shared.EnemyCount() + " slain=" + slain + " crumbled=" + Shared.CrumbledCount());
        Console.WriteLine("first jump frame=" + firstJump + " first climb frame=" + firstClimb);
        Console.WriteLine("max x*1000=" + Milli(maxX) + " max y*1000=" + Milli(maxY) + " end x*1000=" + Milli(player.WorldX()) + " y*1000=" + Milli(player.WorldY()));
        return won == 1 ? 0 : 1;
    }
}
'''


SUBSET_RENDER_CS = r'''using System;
using @NS@;
using Prowl.Native.Gfx2D;

// Draws the game with Prowl2D's headless renderer (Native/Gfx2D): a batch of tinted boxes in one
// instanced draw call, saved as frame_NNNN.ppm. The renderer has no textures, so the pixel art
// (ASCII -> sprites.json) is drawn as one tiny box per run of same-coloured pixels (see Art.cs).
static class SlimeRender
{
    const int Width = @WIDTH@;
    const int Height = @HEIGHT@;
    const int Every = @EVERY@;
    const int MaxSprites = 6000;
    const float HalfHeight = @HALFH@;
    static bool enabled;
    static float[] batch;
    static int n;
    static int saved;

    static void Put(float[] a, int i, float v) { a[i] = v; }
    static float Clamp(float v, float lo, float hi) { if (v < lo) return lo; if (v > hi) return hi; return v; }

    public static void Init()
    {
        Art.Init();
        batch = new float[MaxSprites * 12];
        int ok = GFX.Init(Width, Height);
        if (ok != 0) enabled = true;
        Console.WriteLine("renderer=" + ok + " size=" + Width + "x" + Height + " every=" + Every);
    }

    static void Box(float x, float y, float hw, float hh, float r, float g, float b, float a, float layer)
    {
        BoxA(x, y, hw, hh, 0f, r, g, b, a, layer);
    }

    static void BoxA(float x, float y, float hw, float hh, float angle, float r, float g, float b, float a, float layer)
    {
        if (n >= MaxSprites) return;
        int o = n * 12;
        Put(batch, o, x);
        Put(batch, o + 1, y);
        Put(batch, o + 2, hw);
        Put(batch, o + 3, hh);
        Put(batch, o + 4, angle);
        Put(batch, o + 5, r);
        Put(batch, o + 6, g);
        Put(batch, o + 7, b);
        Put(batch, o + 8, a);
        Put(batch, o + 9, 0f);
        Put(batch, o + 10, layer);
        Put(batch, o + 11, 0f);
        n++;
    }

    static void Rect(float x, float y, float w, float h, float r, float g, float b, float layer)
    {
        Box(x + w * 0.5f, y + h * 0.5f, w * 0.5f, h * 0.5f, r, g, b, 1f, layer);
    }

    static void DrawArt(int id, float cx, float cy, bool flip, float alpha, float layer)
    {
        int cnt = Art.Count(id);
        for (int i = 0; i < cnt; i++)
        {
            float dx = Art.Run(id, i, 0);
            if (flip) dx = -dx;
            Box(cx + dx, cy + Art.Run(id, i, 1), Art.Run(id, i, 2), Art.Run(id, i, 3),
                Art.Run(id, i, 4), Art.Run(id, i, 5), Art.Run(id, i, 6), alpha, layer);
        }
    }

    // The same art, turned: every run of boxes is rotated about the sprite's pivot and drawn at the same angle.
    static void DrawArtRot(int id, float cx, float cy, float angle, float alpha, float layer)
    {
        float ca = MathF.Cos(angle);
        float sa = MathF.Sin(angle);
        int cnt = Art.Count(id);
        for (int i = 0; i < cnt; i++)
        {
            float dx = Art.Run(id, i, 0);
            float dy = Art.Run(id, i, 1);
            BoxA(cx + dx * ca - dy * sa, cy + dx * sa + dy * ca, Art.Run(id, i, 2), Art.Run(id, i, 3), angle,
                 Art.Run(id, i, 4), Art.Run(id, i, 5), Art.Run(id, i, 6), alpha, layer);
        }
    }

    static bool Visible(float x, float y, float w, float h, float x0, float y0, float x1, float y1)
    {
        return !(x > x1 || x + w < x0 || y > y1 || y + h < y0);
    }

    // Rock is flat colour + a light top edge + deterministic speckles (the renderer has no textures).
    static void DrawRock(float rx, float ry, float rw, float rh, float x0, float y0, float x1, float y1)
    {
        Rect(rx, ry, rw, rh, @ROCK@, 1f);
        Rect(rx, ry + rh - 0.1f, rw, 0.1f, @ROCK_LIGHT@, 1.5f);
        float lx = rx; if (lx < x0) lx = x0;
        float hx = rx + rw; if (hx > x1) hx = x1;
        float ly = ry; if (ly < y0) ly = y0;
        float hy = ry + rh; if (hy > y1) hy = y1;
        for (int tx = (int)lx; tx < (int)hx + 1; tx++)
            for (int ty = (int)ly; ty < (int)hy + 1; ty++)
            {
                if (tx < rx || tx >= rx + rw || ty < ry || ty >= ry + rh) continue;
                uint h = (uint)tx * 73856093u ^ (uint)ty * 19349663u;
                if ((h & 3u) == 0u)
                    Box(tx + 0.1f + ((h >> 4) & 7u) * 0.1f, ty + 0.1f + ((h >> 8) & 7u) * 0.1f, 0.0625f, 0.0625f, @ROCK_DARK@, 1f, 1.2f);
                else if (((h >> 12) & 3u) == 0u)
                    Box(tx + 0.1f + ((h >> 16) & 7u) * 0.1f, ty + 0.1f + ((h >> 20) & 7u) * 0.1f, 0.0625f, 0.0625f, @ROCK_LIGHT@, 1f, 1.2f);
            }
    }

    static void DrawTiles(int art, float rx, float ry, float rw, float rh, float x0, float y0, float x1, float y1, float layer)
    {
        for (int tx = (int)rx; tx < (int)(rx + rw); tx++)
            for (int ty = (int)ry; ty < (int)(ry + rh); ty++)
                if (tx + 1 >= x0 && tx <= x1 && ty + 1 >= y0 && ty <= y1)
                    DrawArt(art, tx + 0.5f, ty + 0.5f, false, 1f, layer);
    }

    public static void Frame(int frame, float px, float py, float facing)
    {
        if (!enabled || frame % Every != 0) return;
        float halfW = HalfHeight * Width / Height;
        float camX = px;
        float camY = py;
        if (Level.MaxX() - Level.MinX() > halfW * 2f) camX = Clamp(px, Level.MinX() + halfW, Level.MaxX() - halfW);
        if (Level.MaxY() > HalfHeight * 2f) camY = Clamp(py, HalfHeight, Level.MaxY() - HalfHeight);
        float x0 = camX - halfW - 1f;
        float x1 = camX + halfW + 1f;
        float y0 = camY - HalfHeight - 1f;
        float y1 = camY + HalfHeight + 1f;
        n = 0;

        for (int i = 0; i < Level.WallCount; i++)
        {
            float rx = Level.Wall(i, 0), ry = Level.Wall(i, 1), rw = Level.Wall(i, 2), rh = Level.Wall(i, 3);
            if (Visible(rx, ry, rw, rh, x0, y0, x1, y1)) DrawRock(rx, ry, rw, rh, x0, y0, x1, y1);
        }
        for (int i = 0; i < Level.ClimbCount; i++)
        {
            float rx = Level.Climb(i, 0), ry = Level.Climb(i, 1), rw = Level.Climb(i, 2), rh = Level.Climb(i, 3);
            if (Visible(rx, ry, rw, rh, x0, y0, x1, y1)) DrawTiles(Art.Moss, rx, ry, rw, rh, x0, y0, x1, y1, 2f);
        }
        for (int i = 0; i < Level.SpikeCount; i++)
        {
            float rx = Level.Spike(i, 0), ry = Level.Spike(i, 1), rw = Level.Spike(i, 2), rh = Level.Spike(i, 3);
            if (Visible(rx, ry, rw, rh, x0, y0, x1, y1)) DrawTiles(Art.Spike, rx, ry, rw, rh, x0, y0, x1, y1, 3f);
        }
        for (int i = 0; i < Level.CrumblyCount; i++)
        {
            float cx = Level.CrumblyTile(i, 0), cy = Level.CrumblyTile(i, 1);
            float ca = Shared.CrumblyAlpha(i);
            if (ca > 0f && Visible(cx, cy, 1f, 1f, x0, y0, x1, y1)) DrawArt(Art.Crumbly, cx + 0.5f, cy + 0.5f, false, ca, 2f);
        }
        for (int i = 0; i < Level.ShooterCount; i++)
        {
            float shx = Level.Shooter(i, 0), shy = Level.Shooter(i, 1);
            if (Visible(shx - 1f, shy - 1f, 2f, 2f, x0, y0, x1, y1))
                DrawArtRot(Art.Shooter, shx, shy, MathF.Atan2(Level.Shooter(i, 3), Level.Shooter(i, 2)), 1f, 2.5f);
        }
        for (int i = 0; i < Level.SaveCount; i++)
        {
            float sx = Level.Save(i, 0), sy = Level.Save(i, 1);
            if (!Visible(sx - 1f, sy, 2f, 2f, x0, y0, x1, y1)) continue;
            int art = Art.Savepoint;
            if (Shared.SaveTouched(i)) art = Art.SavepointOn;
            DrawArt(art, sx, sy, false, 1f, 4f);
        }
        for (int i = 0; i < Level.GemCount; i++)
        {
            float gx = Level.Gem(i, 0), gy = Level.Gem(i, 1);
            int state = Shared.GemState(i);
            if (state == 1 || !Visible(gx - 1f, gy - 1f, 2f, 2f, x0, y0, x1, y1)) continue;
            float a = 1f;
            if (state == 2) a = 0.25f;
            DrawArt(Art.Gem, gx, gy, false, a, 4f);
        }
        if (Visible(Level.GoalX() - 1f, Level.GoalY() - 1f, 2f, 2f, x0, y0, x1, y1))
            DrawArt(Art.Goal, Level.GoalX(), Level.GoalY(), false, 1f, 4f);
        for (int i = 0; i < Shared.EnemyCount(); i++)
        {
            if (!Shared.EnemyAlive(i)) continue;
            float ex = Shared.EnemyX(i), ey = Shared.EnemyY(i);
            if (!Visible(ex - 1f, ey - 1f, 2f, 2f, x0, y0, x1, y1)) continue;
            int art = Art.Worm;
            if (Shared.EnemyKind(i) == 1) art = Art.Bat;
            DrawArt(art, ex, ey, Shared.EnemyFace(i) < 0f, 1f, 5f);
        }
        Scene2D scn = Scene2D.Current;
        for (int i = 0; i < scn.NodeHighWater; i++)
        {
            Node b = scn.NodeAt(i);
            if (b == null || !b.Alive || b.Destroyed) continue;
            if (b.Tag == Shared.TagBulletPlayer) DrawArt(Art.BulletGreen, b.WorldX(), b.WorldY(), false, 1f, 7f);
            else if (b.Tag == Shared.TagBulletEnemy) DrawArt(Art.BulletRed, b.WorldX(), b.WorldY(), false, 1f, 7f);
            else if (b.Tag == Shared.TagArrow) DrawArtRot(Art.Arrow, b.WorldX(), b.WorldY(), b.WorldAngle(), 1f, 7f);
        }
        if (Shared.LassoActive)
        {
            float tipX = Shared.LassoTipX;
            float tipY = Shared.LassoTipY;
            if (!Shared.LassoAttached) { tipX = px + Shared.LassoOffX; tipY = py + Shared.LassoOffY; }
            float rdx = tipX - px;
            float rdy = tipY - py;
            float rlen = MathF.Sqrt(rdx * rdx + rdy * rdy);
            if (rlen > 0.01f)
                BoxA(px + rdx * 0.5f, py + rdy * 0.5f, rlen * 0.5f, 0.05f, MathF.Atan2(rdy, rdx), 0.95f, 0.85f, 0.55f, 1f, 6.5f);
            Box(tipX, tipY, 0.12f, 0.12f, 0.95f, 0.85f, 0.55f, 1f, 6.6f);
        }
        DrawArt(Art.Slime, px, py, facing < 0f, 1f, 6f);

        GFX.Camera(camX, camY, HalfHeight, @BG@);
        int drawn = GFX.Draw(batch, n);
        GFX.SaveFrame(saved);
        saved++;
        Console.WriteLine("frame " + saved + " t=" + frame + " sprites=" + drawn + " hash=" + GFX.FrameHash());
    }

    public static void Shutdown()
    {
        if (enabled) GFX.Shutdown();
    }
}
'''


def _f(v):
    """A C# float literal."""
    s = repr(float(v))
    if s.endswith(".0"):
        s = s[:-2]
    return s + "f"


def subset_cfg_cs():
    p, w, l = CONFIG["player"], CONFIG["world"], CONFIG["lasso"]
    consts = [
        ("Dt", w["fixedDeltaTime"]), ("Gravity", w["gravity"]), ("KillPlaneMargin", w["killPlaneMargin"]),
        ("MoveSpeed", p["moveSpeed"]), ("JumpSpeed", p["jumpSpeed"]), ("ClimbSpeed", p["climbSpeed"]),
        ("ClimbFallSpeed", p["climbFallSpeed"]), ("LinearDamping", p["linearDamping"]),
        ("RespawnDelay", p["respawnDelay"]), ("ColliderW", p["colliderW"]), ("ColliderH", p["colliderH"]),
        ("CrumblyDissolveTime", CONFIG["crumbly"]["dissolveTime"]), ("ArrowSpeed", CONFIG["arrow"]["speed"]),
        ("ArrowLifetime", CONFIG["arrow"]["lifetime"]), ("ArrowDamage", CONFIG["arrow"]["damage"]),
        ("ShooterCooldown", CONFIG["arrow"]["cooldown"]),
        ("ShootCooldown", p["shootCooldown"]), ("BulletSpeed", p["bulletSpeed"]), ("BulletDamage", p["bulletDamage"]),
        ("BulletLifetime", p["bulletLifetime"]),
        ("LassoMaxLength", l["maxLength"]), ("LassoShootSpeed", l["shootSpeed"]),
        ("LassoSwingSpeed", l["swingSpeed"]), ("LassoChangeLengthSpeed", l["changeLengthSpeed"]),
    ]
    lines = ["// GENERATED from gen_slime.CONFIG", "static class Cfg", "{"]
    lines += ["    public const float %s = %s;" % (n, _f(v)) for n, v in consts]
    lines.append("}")
    return "\n".join(lines) + "\n"


ENEMY_KINDS = ["worm", "bat"]          # Level.EnemyKind: 0 = worm, 1 = bat
ENEMY_FIELDS = [("Hp", "hp"), ("MoveSpeed", "moveSpeed"), ("VisionRange", "visionRange"), ("VisionAngle", "visionAngle"),
                ("PatrolRange", "patrolRange"), ("PatrolStopDist", "patrolStopDist"),
                ("MinPatrolAngle", "minPatrolDestinationAngleDifference"), ("PatrolStopMin", "patrolStopTimeMin"),
                ("PatrolStopMax", "patrolStopTimeMax"), ("LookToHurt", "lookToHurtDirectionDuration"),
                ("AttackMin", "attackDistMin"), ("AttackMax", "attackDistMax"), ("ChaseStopMin", "chaseStopDistMin"),
                ("ChaseStopMax", "chaseStopDistMax"), ("ShootInterval", "shootInterval"), ("BulletSpeed", "bulletSpeed"),
                ("BulletDamage", "bulletDamage"), ("BulletLifetime", "bulletLifetime"), ("ContactDamage", "contactDamage"),
                ("ColliderW", "colliderW"), ("ColliderH", "colliderH"), ("GravityScale", "gravityScale")]


def subset_enemy_cfg_cs():
    """Per-kind enemy tunables as functions of the kind (subset code has no per-object config)."""
    worm, bat = CONFIG["worm"], CONFIG["bat"]
    L = ["// GENERATED from gen_slime.CONFIG (worm = kind 0, bat = kind 1)", "static class EnemyCfg", "{",
         "    public static bool IsFlying(int kind) { return kind == 1; }"]
    assert not worm["isFlying"] and bat["isFlying"]
    for name, key in ENEMY_FIELDS:
        L.append("    public static float %s(int kind) { if (kind == 1) return %s; return %s; }" % (name, _f(bat[key]), _f(worm[key])))
    L.append("}")
    return "\n".join(L) + "\n"


def subset_layers_cs():
    lines = ["// GENERATED from gen_slime.LAYERS / LAYER_IGNORES", "static class Layers", "{"]
    for n, i in sorted(LAYERS.items(), key=lambda kv: kv[1]):
        lines.append("    public const int %s = %d;" % (n.replace(" ", ""), i))
    lines.append("}")
    return "\n".join(lines) + "\n"


def subset_level_cs(level):
    """The level as C# data. Subset code cannot parse JSON, cannot initialise array fields where they
    are declared, and cannot subscript a static array from another class, so: arrays are filled in
    Init(), and every read goes through an accessor method here."""
    arrays = [("Walls", "Wall", "WallCount", level["walls"], 4), ("Climbs", "Climb", "ClimbCount", level["climbables"], 4),
              ("Spikes", "Spike", "SpikeCount", level["spikes"], 4), ("Saves", "Save", "SaveCount", level["savepoints"], 2),
              ("Gems", "Gem", "GemCount", level["gems"], 2),
              ("Anchors", "Anchor", "AnchorCount", level.get("hints", {}).get("anchors", []), 2),
              ("Crumbles", "CrumblyTile", "CrumblyCount", level.get("crumbly", []), 4),
              ("Shooters", "Shooter", "ShooterCount", level.get("shooters", []), 4)]
    L = ["// GENERATED from the level data", "static class Level", "{",
         "    static float spawnX, spawnY, goalX, goalY, minY, minX, maxX, maxY;",
         "    public static float MinX() { return minX; }",
         "    public static float MaxX() { return maxX; }",
         "    public static float MaxY() { return maxY; }",
         "    public static float SpawnX() { return spawnX; }",
         "    public static float SpawnY() { return spawnY; }",
         "    public static float GoalX() { return goalX; }",
         "    public static float GoalY() { return goalY; }",
         "    public static float MinY() { return minY; }"]
    for arr, acc, cnt, items, stride in arrays:
        L.append("    static float[] %s;" % arr)
        L.append("    public static int %s;" % cnt)
        L.append("    public static float %s(int i, int k) { return At(%s, i * %d + k); }" % (acc, arr, stride))
    L += ["    static float[] Enemies;", "    public static int EnemyCount;",
          "    public static int EnemyKind(int i) { return (int)At(Enemies, i * 3); }",
          "    public static float EnemyX(int i) { return At(Enemies, i * 3 + 1); }",
          "    public static float EnemyY(int i) { return At(Enemies, i * 3 + 2); }",
          "    static float At(float[] a, int i) { return a[i]; }",
          "    static void Set4(float[] a, int i, float x, float y, float w, float h) { a[i * 4] = x; a[i * 4 + 1] = y; a[i * 4 + 2] = w; a[i * 4 + 3] = h; }",
          "    static void Set2(float[] a, int i, float x, float y) { a[i * 2] = x; a[i * 2 + 1] = y; }", "",
          "    public static void Init()", "    {",
          "        spawnX = %s; spawnY = %s;" % (_f(level["spawn"][0]), _f(level["spawn"][1])),
          "        goalX = %s; goalY = %s;" % (_f(level["goal"]["x"]), _f(level["goal"]["y"])),
          "        minY = %s;" % _f(level["bounds"][1]),
          "        minX = %s; maxX = %s; maxY = %s;" % (_f(level["bounds"][0]), _f(level["bounds"][2]), _f(level["bounds"][3]))]
    for arr, acc, cnt, items, stride in arrays:
        L.append("        %s = new float[%d]; %s = %d;" % (arr, max(1, len(items)) * stride, cnt, len(items)))
        for i, r in enumerate(items):
            if stride == 4:
                third, fourth = (r["w"], r["h"]) if "w" in r else (r["dx"], r["dy"])      # rects, or shooters: x y dx dy
                L.append("        Set4(%s, %d, %s, %s, %s, %s);" % (arr, i, _f(r["x"]), _f(r["y"]), _f(third), _f(fourth)))
            else:
                L.append("        Set2(%s, %d, %s, %s);" % (arr, i, _f(r["x"]), _f(r["y"])))
    L.append("        Enemies = new float[%d]; EnemyCount = %d;" % (max(1, len(level["enemies"])) * 3, len(level["enemies"])))
    for i, e in enumerate(level["enemies"]):
        L.append("        Enemies[%d] = %d; Enemies[%d] = %s; Enemies[%d] = %s;" % (i * 3, ENEMY_KINDS.index(e["type"]), i * 3 + 1, _f(e["x"]), i * 3 + 2, _f(e["y"])))
    L += ["    }", "", "    // Layer collision matrix: rows[a] bit b set == layers a and b collide.",
          "    public static uint[] LayerRows()", "    {", "        uint[] r = new uint[32];",
          "        for (int i = 0; i < 32; i++) r[i] = 0xFFFFFFFFu;"]
    for a_, b_ in ignored_pairs():
        L.append("        r[%d] &= ~(1u << %d); r[%d] &= ~(1u << %d);" % (a_, b_, b_, a_))
    L += ["        return r;", "    }", "}"]
    return "\n".join(L) + "\n"


def _hex_rgb(h):
    h = h.lstrip("#")
    return tuple(int(h[i:i + 2], 16) / 255.0 for i in (0, 2, 4))


def subset_art_cs(sheet):
    """Pixel art as boxes: for each sprite, one record per horizontal run of same-coloured pixels
    (x, y of the run's centre relative to the sprite's pivot, half width, half height, r, g, b), in
    world units. The renderer in Prowl2D has no textures, so this is how the ASCII art reaches it."""
    runs, starts, counts, names = [], [], [], []
    for sp in sheet["sprites"]:
        pal = {e[0]: _hex_rgb(e[1:]) for e in sp["palette"]}
        h, w = len(sp["rows"]), len(sp["rows"][0])
        ppu, px, py = float(sp["ppu"]), sp["pivotX"], sp["pivotY"]
        starts.append(len(runs))
        for r, row in enumerate(sp["rows"]):
            x = 0
            while x < w:
                ch = row[x]
                if ch == "." or ch not in pal:
                    x += 1
                    continue
                x1 = x
                while x1 + 1 < w and row[x1 + 1] == ch:
                    x1 += 1
                n = x1 - x + 1
                runs.append(((x + n / 2.0 - w * px) / ppu, (h - r - 0.5 - h * py) / ppu, n / (2.0 * ppu), 0.5 / ppu) + pal[ch])
                x = x1 + 1
        counts.append(len(runs) - starts[-1])
        names.append(sp["name"])
    camel = lambda n: "".join(part.capitalize() for part in n.split("_"))
    L = ["// GENERATED from sprites.json (gen_sprites.py): pixel art as runs of boxes", "static class Art", "{"]
    for i, n in enumerate(names):
        L.append("    public const int %s = %d;" % (camel(n), i))
    L += ["    static float[] data;", "    static int[] start;", "    static int[] cnt;",
          "    static float AtF(float[] a, int i) { return a[i]; }",
          "    static int AtI(int[] a, int i) { return a[i]; }",
          "    static void SetI(int[] a, int i, int v) { a[i] = v; }",
          "    static void Set7(float[] a, int i, float v0, float v1, float v2, float v3, float v4, float v5, float v6)",
          "    {", "        int o = i * 7;",
          "        a[o] = v0; a[o + 1] = v1; a[o + 2] = v2; a[o + 3] = v3; a[o + 4] = v4; a[o + 5] = v5; a[o + 6] = v6;", "    }",
          "    public static int Count(int id) { return AtI(cnt, id); }",
          "    public static float Run(int id, int i, int k) { return AtF(data, (AtI(start, id) + i) * 7 + k); }", "",
          "    public static void Init()", "    {",
          "        data = new float[%d];" % (len(runs) * 7), "        start = new int[%d];" % len(names), "        cnt = new int[%d];" % len(names)]
    for i in range(len(names)):
        L.append("        SetI(start, %d, %d); SetI(cnt, %d, %d);" % (i, starts[i], i, counts[i]))
    for i, r in enumerate(runs):
        L.append("        Set7(data, %d, %s);" % (i, ", ".join(_f(round(v, 5)) for v in r)))
    L += ["    }", "}"]
    return "\n".join(L) + "\n"


def _palette_entries():
    import gen_sprites
    return gen_sprites.PALETTE


def write_subset_project(out, level, engine, max_frames=3000, force=False, render=False,
                         render_every=8, render_size=(800, 450), sprites=None, release_angle=50.0):
    """Write a Prowl2D/Stride2D game folder: any folder of .cs files with a static Main.
    Build it with  python3 build.py player <out> --verify --run  inside the engine repo
    (or tools/run_2d.py)."""
    validate_level(level)
    if engine not in ENGINES:
        raise ValueError("engine must be one of %s" % sorted(ENGINES))
    e = ENGINES[engine]
    if os.path.exists(out) and os.listdir(out):
        if not (os.path.exists(os.path.join(out, MARKER)) or force):
            raise SystemExit("%s exists and was not created by gen_slime (use --force)" % out)
        for entry in os.listdir(out):
            p = os.path.join(out, entry)
            shutil.rmtree(p) if os.path.isdir(p) else os.remove(p)
    os.makedirs(out, exist_ok=True)
    _write(os.path.join(out, MARKER), "generated by tools/gen_slime.py (%s)\n" % engine)
    if render and engine != "prowl2d":
        raise ValueError("only Prowl2D has a renderer (Native/Gfx2D); Stride2D draws nothing yet")
    game = SUBSET_GAME_CS.replace("@MAX_FRAMES@", str(max_frames))
    if render:
        game = (game.replace("@RENDER_INIT@", "        SlimeRender.Init();")
                    .replace("@RENDER_FRAME@", "            SlimeRender.Frame(frame, px, py, Shared.PlayerFacing);")
                    .replace("@RENDER_END@", "        SlimeRender.Shutdown();"))
    else:
        game = game.replace("@RENDER_INIT@\n", "").replace("@RENDER_FRAME@\n", "").replace("@RENDER_END@\n", "")
    files = {
        "Game.cs": game,
        "Player.cs": SUBSET_PLAYER_CS, "Shared.cs": SUBSET_SHARED_CS, "Objects.cs": SUBSET_OBJECTS_CS, "Enemy.cs": SUBSET_ENEMY_CS, "Traps.cs": SUBSET_TRAPS_CS, "Lasso.cs": SUBSET_LASSO_CS,
        "EnemyCfg.cs": subset_enemy_cfg_cs(), "Bot.cs": SUBSET_BOT_CS,
        "Input.cs": SUBSET_INPUT_CS,
        "Cfg.cs": subset_cfg_cs(), "Layers.cs": subset_layers_cs(), "Level.cs": subset_level_cs(level),
    }
    if render:
        bg = _hex_rgb(level["background"])
        palette = {n: _hex_rgb(c) for n, c, _ in _palette_entries()}
        fl = lambda c: ", ".join(_f(round(v, 4)) for v in c)
        files["SlimeRender.cs"] = (SUBSET_RENDER_CS.replace("@WIDTH@", str(render_size[0])).replace("@HEIGHT@", str(render_size[1]))
                              .replace("@EVERY@", str(render_every)).replace("@HALFH@", _f(CONFIG["world"]["cameraSize"] * 0.75))
                              .replace("@BG@", fl(bg)).replace("@ROCK@", fl(palette["r"])).replace("@ROCK_LIGHT@", fl(palette["q"]))
                              .replace("@ROCK_DARK@", fl(palette["R"])))
        files["Art.cs"] = subset_art_cs(sprites or build_sprites())
    written = []
    for name, text in files.items():
        text = text.replace("@NS@", e["ns"]).replace("@PB2NS@", e["pb2"]).replace("@RELEASE_ANGLE@", _f(release_angle))
        path = os.path.join(out, name)
        _write(path, text)
        written.append(path)
    return written


# ======================================================================================
# 7. CLI
# ======================================================================================
def main(argv=None):
    ap = argparse.ArgumentParser(description="Generate a Slime Jump project (Unity, Prowl2D or Stride2D) from a level JSON.")
    ap.add_argument("--level", required=True, help="level.json (see validate_level for the schema)")
    ap.add_argument("--out", default="/tmp/SlimeJumpProject")
    ap.add_argument("--unity-version", default=DEFAULT_UNITY_VERSION)
    ap.add_argument("--input-system", action="store_true", help="add the Input System package")
    ap.add_argument("--sprites", help="sprites.json from gen_sprites.py (default: built-in art)")
    ap.add_argument("--target", choices=["unity", "prowl2d", "stride2d"], default="unity")
    ap.add_argument("--render", action="store_true", help="prowl2d: draw frames with Native/Gfx2D and save PPMs")
    ap.add_argument("--render-every", type=int, default=8, help="save a frame every N game frames")
    ap.add_argument("--max-frames", type=int, default=3000, help="subset targets: frames the bot may play")
    ap.add_argument("--data-only", action="store_true", help="only write the JSON data files")
    ap.add_argument("--check", action="store_true", help="syntax-check generated C# with tree-sitter")
    ap.add_argument("--force", action="store_true")
    a = ap.parse_args(argv)
    with open(a.level) as f:
        level = json.load(f)
    sprites_override = None
    if a.sprites:
        with open(a.sprites) as f:
            sprites_override = json.load(f)
    if a.target != "unity":
        files = write_subset_project(a.out, level, a.target, a.max_frames, force=a.force,
                                     render=a.render, render_every=a.render_every, sprites=sprites_override)
        print("wrote", a.out, "(%s)" % a.target)
        return 0
    sprites = None
    if a.sprites:
        with open(a.sprites) as f:
            sprites = json.load(f)
    files = write_project(a.out, level, a.unity_version, input_system=a.input_system,
                          data_only=a.data_only, force=a.force, sprites=sprites)
    print("wrote", a.out)
    if a.check and files:
        probs = check_csharp(files)
        if probs is None:
            print("tree-sitter not installed: pip install tree-sitter tree-sitter-c-sharp")
        elif probs:
            print("\n".join(probs))
            return 1
        else:
            print("C# syntax check passed (%d files)" % len(files))
    return 0


if __name__ == "__main__":
    sys.exit(main())

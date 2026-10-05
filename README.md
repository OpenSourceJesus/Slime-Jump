# Slime Jump

A 2D platformer in which you play a slime: variable-height jumps, wall climbing on moss, a lasso you swing on, a blaster, gems and savepoints, worms and bats, arrow traps and crumbling floors.

This repository holds two things:

1. the **Unity project** (`Assets/`, `ProjectSettings/`), and
2. a **porting toolchain** (`tools/`) that regenerates the game's data and logic as code for Unity, [Prowl2D](https://github.com/crustos/Prowl2D) and [Stride2D](https://github.com/crustos/stride2D), so the same game can run on engines that translate C# to native C.

---

## The Unity project

- Scripts are found under `Assets/Standard Assets`.
- If you make changes to `Assets/Scenes/World.unity` or `Assets/Scenes/World (Rage).unity` click "Game->Update world" or press Ctrl + M before testing.
- The Unity version is in `ProjectSettings/ProjectVersion.txt`.
- The scenes (`*.unity`) and sound effects (`*.wav`) are stored with **Git LFS** (see `.gitattributes`). A clone without the LFS objects contains only small pointer files for them. The toolchain below does not need those files.
- Third-party code is bundled under `Assets/Standard Assets` (Destructible2D, 2DColliderGen, TextMesh Pro, Confetti FX).

---

## Porting toolchain (`tools/`)

The level data lives in the scenes, so the toolchain rebuilds the game from small, reviewable text sources instead: **ASCII art** for sprites, **ASCII maps** for levels, and the game logic written once as C# in `gen_slime.py`.

```
gen_sprites.py   ASCII art  --+
gen_levels.py    ASCII maps --+--> gen_slime.py --+--> Unity project             (C#, runs at startup from JSON)
gen_cave_level.py  (procedural cave, rasterised   |--> Prowl2D game folder       (translatable C# subset)
                    into the same ASCII format)   +--> Stride2D game folder      (the same source)
```

### Quick start

```sh
pip install pillow                                    # the only Python dependency

python3 tools/gen_sprites.py                          # ASCII art  -> PNGs, atlas, tileset, sprites.json
python3 tools/gen_levels.py                           # ASCII maps -> level.json, .tmj, preview, and a finishability check
python3 tools/gen_levels.py --unity /tmp/SlimeJumpCave --level cave    # also write a Unity project
```

Everything is written under `/tmp/slimejump_art` (change with `--out`):

```
ascii/palette.txt, ascii/sprites/*.txt, ascii/levels/*.txt     the sources (edit these, or write your own and pass --src)
png/*.png, atlas.png + atlas.json, tileset.png, preview.png    rendered sprites
sprites.json                                                   what the generators consume
levels/<name>.json   .tmj   .png                               level data, Tiled map, preview drawn with the real sprites
```

Open the generated Unity project (`/tmp/SlimeJumpCave`) with the Unity version in its `ProjectSettings/ProjectVersion.txt`, open any scene and press Play. Controls: A/D move, W jump (hold to climb moss), left click shoot, right click lasso (W/S reel in/out), Esc pause, Del reset save.

### The ASCII formats

**Sprites** (`ascii/sprites/<name>.txt`): one character is one pixel, and one character always means one colour (`palette.txt`).

```
@ppu 14             pixels per world unit
@pivot 0.5 0.5      x y, from the bottom-left
@kind tile          optional: tiles are repeated when drawn
....GGGGGG....     rows, top first; '.' is transparent
..GGhggggggG..
```

**Levels** (`ascii/levels/<name>.txt`): one character per tile, the top row of the file is the top of the level.

| char | meaning | char | meaning |
|---|---|---|---|
| `#` `=` | solid rock / platform | `@` | player spawn (exactly one) |
| `M` | moss: solid and climbable | `S` | savepoint |
| `^` | spikes | `o` | gem |
| `.` | empty | `G` | goal portal (exactly one) |
| `w` `b` | worm (ground) / bat (flying) | `*` | lasso anchor hint for the test bot (not part of the game) |
| `~` | crumbly: solid, fades 1 s after you first touch it, back on respawn | `>` `<` `v` `A` | arrow shooter (solid), fires right / left / down / up when you are in its line of sight |

Header lines: `@name`, `@background #rrggbb`, and `@requires lasso` for a level that cannot be finished without the lasso (the checker then verifies it really is *not* passable by walking and jumping).

Four levels ship: `tutorial`, `swing` and `gauntlet` (hand-drawn) and `cave` (procedural; `--cave-seed`, `--cave-length`).

### Targets

| Target | What you get | How |
|---|---|---|
| **Unity** | A Unity project: C# scripts, level/config/sprites as JSON, built at startup (no scene needed) | `gen_levels.py --unity DIR --level NAME` |
| **Prowl2D** | A game folder in the C# subset that CCSharp translates to C; also runs on .NET | `gen_levels.py --prowl2d DIR`, built with `python3 build.py player DIR` in the engine repo |
| **Stride2D** | The same source with the other engine's namespace | `gen_levels.py --stride2d DIR` |

`tools/run_2d.py` does the whole 2D loop for you: generate, translate to C, build the native player, run it, run the same source on .NET, and compare.

```sh
python3 tools/run_2d.py --level /tmp/slimejump_art/levels/tutorial.json
python3 tools/run_2d.py --level /tmp/slimejump_art/levels/cave.json --max-frames 20000
python3 tools/run_2d.py --level /tmp/slimejump_art/levels/swing.json --render --engines prowl2d --gif /tmp/swing.gif
```

### What is checked

- **Levels** are verified by simulating the player (walks, jumps, cut-short jumps, mid-air direction release, moss climbs) on the tile grid; a level that is not finishable fails the run. This is an approximation of the real physics, so treat a failure as "look at this".
- **2D engines:** a deterministic test bot plays each level. The run must (1) win, (2) print the same output natively and on .NET, and (3) print the same output on both engines. With `--render`, every saved frame's pixel hash is part of that output.
- **Unity target:** the generated C# is syntax-checked with tree-sitter (`gen_slime.py --check`).

Results at the time of writing (the bot is deterministic):

| Level | Exercises | Result on both engines |
|---|---|---|
| `tutorial` | step, spiked pit, worm, moss climb, bat | won at frame 528, 0 deaths, both enemies killed |
| `cave` (seed 7) | 419x51 tiles, 10 enemies, 2 climbs, wide pits, a lasso gap with stepping stones | won at frame 2850, 1 death |
| `swing` | a 30-wide gap that needs the lasso | won at frame 460, 0 deaths; the same bot without the lasso never crosses |
| `gauntlet` | a head-on arrow shooter, and a 26-wide spiked pit with a crumbly bridge | won at frame 390, 0 deaths, 17 bridge tiles crumbled behind it; standing still on the bridge or never dodging arrows kills the bot |

### What is ported

Movement, variable-height jump, wall climbing, spikes, savepoints, gems (committed at a savepoint), goal, death and respawn, worms and bats (patrol, vision cone, chase, spit), bullets, a blaster, the lasso (hook flight, taut-rope pendulum with the original constants, release momentum, reeling), crumbly walls (the original `DissolveOnHit`, 1 s) and arrow shooters (the original `ShooterTrap`: one arrow per second while the player is in line of sight, speed 9).

A note on crumbly floors: side-by-side physics boxes can make a player catch on the seam between them (a "ghost collision"). Level walls avoid it because they are merged into single rectangles, but crumbly tiles must fade one by one, so each tile's collider is made 0.04 wider than its tile and neighbours overlap.

### Known limits

- **The Unity target has never been opened in Unity.** It is generated, syntax-checked and compiled against hand-written stubs of the Unity API, which catches mistakes in the generated code but not misuse of the real API.
- **Prowl2D and Stride2D have no window, input or audio.** The test bot stands in for a player. Only Prowl2D renders (headless, to PPM frames); its renderer draws boxes and discs only, so each art pixel is drawn as a small box. Stride2D produces no pictures.
- Not ported: items, achievements, cosmetics, the world map, procedural and survival modes, UI, audio, Destructible2D fracture, moving platforms, saws, falling rocks, lasers, vortexes, slippery walls, moving or slippery lasso anchors, camera freeze while the rope is taut.
- The 2D engines use a newer Box2D than Unity's, so jump and swing feel may differ. It has not been compared against Unity.
- The level checker does not model the lasso, and the swing test bot uses the level's `*` anchor hints, which a human does not have.

### Requirements

- Python 3 and Pillow. Optional: `tree-sitter` and `tree-sitter-c-sharp` for `--check`.
- For the 2D engines: the .NET 10 SDK, cmake and gcc, with `Prowl2D`, `stride2D` and `crust` cloned next to this repository (or pass `--engines-root`). In each engine repo run `python3 build.py deps` and `python3 build.py native`; Stride2D's `python3 build.py ccsharp` builds the translator both engines use.
- For `--render`: `libegl-dev libgles-dev libegl-mesa0 libgl1-mesa-dri`, then `python3 tools/build_gfx2d.py` (Prowl2D ships the renderer source but no build step for it; this builds it into the engine's untracked `Libraries/` folder).
- Stride2D tracks some files with Git LFS. If `git-lfs` is installed and GitHub rate-limits the download, the checkout fails partway; clone it with `GIT_LFS_SKIP_SMUDGE=1`.

### Tool reference

| File | Role |
|---|---|
| `gen_slime.py` | The game logic as C# strings, plus tunables (taken from the original prefabs), the layer table, and the writers for all three targets |
| `gen_sprites.py` | ASCII art to PNGs, atlas, tileset, `sprites.json` |
| `gen_levels.py` | ASCII levels to JSON / Tiled / preview, the finishability checker, and the hand-drawn levels |
| `gen_cave_level.py` | The procedural cave generator (rasterised into ASCII by `gen_levels.py`) |
| `run_2d.py` | Build, run and compare on Prowl2D and Stride2D |
| `build_gfx2d.py` | Builds Prowl2D's headless renderer library |

The generated projects and game folders are build output: change the generators or the ASCII sources and regenerate, rather than editing the generated files. To add a copy of the game to an engine repo as a sample, generate it into that repo's samples folder, for example `python3 tools/gen_slime.py --level LEVEL.json --target prowl2d --out ../Prowl2D/Samples/SlimeJump` (Stride2D uses lowercase `samples/`).

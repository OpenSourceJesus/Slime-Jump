#!/usr/bin/env python3
"""gen_levels.py - ASCII level design -> level data for Unity and other engines.

Levels are drawn as text, one character per tile (y up, so the TOP row of the file is the
top of the level). The same file format is used for hand-drawn levels and for the
procedural cave (gen_cave_level.Cave is rasterised into it).

PIPELINE
    1. write the source ASCII into   <out>/ascii/levels/*.txt
       (or use --src DIR to translate your own hand-edited ASCII)
    2. translate each level into     <out>/levels/<name>.json   gen_slime.py level format
                                     <out>/levels/<name>.tmj    Tiled map (tile layer + objects)
                                     <out>/levels/<name>.png    preview drawn with the real sprites
    3. verify it can be finished     simulates jumps/climbs with the game's actual physics
    4. optionally emit a Unity project (--unity DIR --level NAME), or a Prowl2D / Stride2D
       game folder (--prowl2d DIR / --stride2d DIR); tools/run_2d.py builds and runs those

LEGEND
    #  solid rock          M  moss: solid + climbable      =  solid (same as #, for platforms)
    ^  spikes (1 tile)     .  empty (a space also works)
    @  player spawn        S  savepoint (sits on the tile's bottom edge)
    o  gem                 G  goal portal (2 tiles tall, put it at floor level)
    w  worm (ground, put it on a floor tile)     b  bat (flying)
    ~  crumbly: solid, fades 1s after the player first touches it, back on respawn
    > < v A   arrow shooter (solid): fires toward where the arrow points when the player is in its line of sight
    *  lasso anchor HINT for the test bot (an empty tile just under a ceiling); not part of the game
    Header lines:  @name <Name>   @background #rrggbb   @requires lasso   ';' starts a comment.
    `@requires lasso` says the level cannot be finished without the lasso: the verifier then
    checks that it is NOT passable by walking and jumping alone.

USAGE
    python3 tools/gen_levels.py [--out /tmp/slimejump_art] [--src DIR] [--cave-seed 7]
                                [--cave-length 360] [--unity /tmp/SlimeJumpCave --level cave]
                                [--no-verify] [--force]
"""
import argparse
import json
import math
import os
import sys
from collections import deque

from PIL import Image

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import gen_sprites as sprites_mod  # noqa: E402
import gen_slime as gs  # noqa: E402

DEFAULT_OUT = sprites_mod.DEFAULT_OUT
SHOOTERS = {">": (1, 0), "<": (-1, 0), "v": (0, -1), "A": (0, 1)}      # solid tiles that fire arrows
SOLID = set("#=M~") | set(SHOOTERS)
ENTITIES = set("@SoGwb*")

# --------------------------------------------------------------------------------------
# Hand-drawn levels (source of truth is this text; edit it or the dumped .txt files)
# --------------------------------------------------------------------------------------
HAND_LEVELS = {}
HAND_LEVELS["tutorial"] = """
; Tutorial: step, spiked pit, worm, mossy climb, bat, goal.
@name Tutorial
@background #14101c
################################################################
#..............................................................#
#..............................................................#
#...........................................................b..#
#.........................................................o....#
#..............................................................#
#............................................................G.#
#.......................................................M#######
#.......................................................M#######
#.......................................................M#######
#.......................................................M#######
#.......................................................M#######
#..........................o............................M#######
#............................................o..........M#######
#.............#######...................................M#######
#..@....S.....#######....................w........S.....M#######
########################.......#################################
########################^^^^^^^#################################
################################################################
################################################################
"""

HAND_LEVELS["swing"] = """
; Swing: a 30-wide spiked gap (a jump reaches ~20) under a low ceiling. Cross it with the lasso.
; The * marks are lasso anchor hints for the test bot.
@name Swing
@background #14101c
@requires lasso
################################################################################
#.................###################################..........................#
#.................###################################..........................#
#.................###################################..........................#
#.................###################################..........................#
#.................###################################..........................#
#.................###################################..........................#
#.................###################################..........................#
#.................###################################..........................#
#.................###################################..........................#
#.......................*...*...*...*...*...*..................................#
#..............................................................................#
#..................................o...........................................#
#..............................................................................#
#...........................................................o..................#
#..@....S...............................................S.................G....#
####################..............................##############################
####################^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^##############################
################################################################################
################################################################################
"""

HAND_LEVELS["gauntlet"] = """
; Gauntlet: an arrow shooter (fires back along the corridor, jump the arrows) and a 26-wide spiked
; pit with a crumbly bridge (keep moving: each tile fades 1s after you first touch it).
@name Gauntlet
@background #14101c
############################################################################
#..........................................................................#
#..........................................................................#
#..........................................................................#
#..........................................................................#
#..........................................................................#
#..........................................................................#
#..........................................................................#
#..........................................................................#
#..........................................................................#
#..........................................................................#
#..........................................................................#
#.................................................o........................#
#...................o......................................................#
#..........................................................................#
#..@....S.....................<...................................S.....G..#
######################################~~~~~~~~~~~~~~~~~~~~~~~~~~############
######################################..........................############
######################################^^^^^^^^^^^^^^^^^^^^^^^^^^############
############################################################################
"""


# --------------------------------------------------------------------------------------
# Parsing ASCII -> grid
# --------------------------------------------------------------------------------------
def parse_level(text, default_name="level"):
    meta = {"name": default_name, "background": "#14101c"}
    rows = []
    for line in text.splitlines():
        line = line.rstrip("\n")
        if not line.strip() or line.startswith(";"):
            continue
        if line.startswith("@"):
            parts = line[1:].split(None, 1)
            meta[parts[0]] = parts[1].strip() if len(parts) > 1 else ""
        else:
            rows.append(line.replace(" ", "."))
    if not rows:
        raise ValueError("%s: no map rows" % meta["name"])
    w = len(rows[0])
    for i, r in enumerate(rows):
        if len(r) != w:
            raise ValueError("%s: row %d is %d wide, expected %d" % (meta["name"], i, len(r), w))
    legal = SOLID | ENTITIES | set(".^")
    for i, r in enumerate(rows):
        for x, ch in enumerate(r):
            if ch not in legal:
                raise ValueError("%s: unknown character '%s' at row %d col %d" % (meta["name"], ch, i, x))
    return {"name": meta["name"], "background": meta["background"], "rows": rows, "W": w, "H": len(rows),
            "requires": meta.get("requires", "").split()}


def cells(lv, chars):
    """Set of (x, y) with y UP (bottom row is y=0) for the given characters."""
    H = lv["H"]
    return {(x, H - 1 - r) for r, row in enumerate(lv["rows"]) for x, ch in enumerate(row) if ch in chars}


def merge_rects(cs, merge_vertical=True):
    """Greedy merge of tile cells into rectangles (rows into runs, equal runs stacked)."""
    byrow = {}
    for x, y in cs:
        byrow.setdefault(y, []).append(x)
    out, active, prev_y = [], {}, None

    def emit(key, y0, y1):
        out.append({"x": key[0], "y": y0, "w": key[1] - key[0], "h": y1 - y0})

    for y in sorted(byrow):
        xs = sorted(byrow[y])
        runs, start, last = [], xs[0], xs[0]
        for x in xs[1:]:
            if x == last + 1:
                last = x
            else:
                runs.append((start, last + 1))
                start = last = x
        runs.append((start, last + 1))
        runs = set(runs)
        if prev_y is not None and (y != prev_y + 1 or not merge_vertical):
            for key in list(active):
                emit(key, active.pop(key), prev_y + 1)
        for key in list(active):
            if key not in runs:
                emit(key, active.pop(key), prev_y + 1)
        for key in runs:
            active.setdefault(key, y)
        prev_y = y
    for key in list(active):
        emit(key, active.pop(key), prev_y + 1)
    return sorted(out, key=lambda r: (r["x"], r["y"]))


# --------------------------------------------------------------------------------------
# Grid -> level dict (gen_slime format)
# --------------------------------------------------------------------------------------
def entity_list(lv, ch):
    """Tiles holding ch, ordered left to right then bottom to top."""
    return sorted(cells(lv, {ch}))


def to_level(lv, config=None):
    cfg = config or gs.CONFIG
    pc, wc = cfg["player"], cfg["worm"]
    W, H = lv["W"], lv["H"]
    spawns = entity_list(lv, "@")
    if len(spawns) != 1:
        raise ValueError("%s: need exactly one '@' (found %d)" % (lv["name"], len(spawns)))
    goals = entity_list(lv, "G")
    if len(goals) != 1:
        raise ValueError("%s: need exactly one 'G' (found %d)" % (lv["name"], len(goals)))
    sx, sy = spawns[0]
    gx, gy = goals[0]
    enemies = [{"type": "worm", "x": x + .5, "y": y + wc["colliderH"] / 2.0} for x, y in entity_list(lv, "w")]
    enemies += [{"type": "bat", "x": x + .5, "y": y + .5} for x, y in entity_list(lv, "b")]
    return {
        "name": lv["name"], "background": lv["background"],
        "bounds": [0, 0, W, H],
        "spawn": [sx + .5, sy + pc["colliderH"] / 2.0 + 0.2],
        "walls": merge_rects(cells(lv, set("#=") | set(SHOOTERS))),
        "climbables": merge_rects(cells(lv, {"M"})),
        "spikes": merge_rects(cells(lv, {"^"}), merge_vertical=False),
        # one object per tile, so each tile fades on its own when touched
        "crumbly": [{"x": x, "y": y, "w": 1, "h": 1} for x, y in sorted(cells(lv, {"~"}))],
        "shooters": [{"x": x + .5, "y": y + .5, "dx": SHOOTERS[ch][0], "dy": SHOOTERS[ch][1]}
                     for ch in SHOOTERS for x, y in sorted(cells(lv, {ch}))],
        "savepoints": [{"name": "Save %d" % (i + 1), "x": x + .5, "y": y} for i, (x, y) in enumerate(entity_list(lv, "S"))],
        "gems": [{"name": "Gem %d" % (i + 1), "x": x + .5, "y": y + .5} for i, (x, y) in enumerate(entity_list(lv, "o"))],
        "enemies": enemies,
        "goal": {"name": "Goal", "x": gx + .5, "y": gy + 1.0},
        # aiming hints for the test bot (a point on the ceiling above each '*'); the game ignores them
        "hints": {"anchors": [{"x": x + .5, "y": y + 1.0} for x, y in entity_list(lv, "*")]},
        "requires": lv["requires"],
    }


# --------------------------------------------------------------------------------------
# Tiled (.tmj) export: a tile layer using tileset.png + an object layer for the entities
# --------------------------------------------------------------------------------------
def to_tmj(lv, level, tile=8, tileset="../tileset.png", tileset_size=(32, 8)):
    gid = {"#": 1, "=": 1, "M": 2, "^": 3, "~": 4}
    gid.update({ch: 1 for ch in SHOOTERS})
    data = [gid.get(ch, 0) for row in lv["rows"] for ch in row]
    H = lv["H"]
    objs, oid = [], 1

    def add(kind, name, wx, wy):
        nonlocal oid
        objs.append({"id": oid, "name": name, "type": kind, "x": wx * tile, "y": (H - wy) * tile,
                     "width": 0, "height": 0, "rotation": 0, "visible": True, "point": True})
        oid += 1

    add("spawn", "Spawn", level["spawn"][0], level["spawn"][1])
    for p in level["savepoints"]:
        add("savepoint", p["name"], p["x"], p["y"])
    for p in level["gems"]:
        add("gem", p["name"], p["x"], p["y"])
    for e in level["enemies"]:
        add(e["type"], e["type"], e["x"], e["y"])
    add("goal", "Goal", level["goal"]["x"], level["goal"]["y"])
    for a in level.get("hints", {}).get("anchors", []):
        add("anchor", "Anchor", a["x"], a["y"])
    for sh in level.get("shooters", []):
        add("shooter", "Shooter %d,%d" % (sh["dx"], sh["dy"]), sh["x"], sh["y"])
    return {
        "version": "1.10", "tiledversion": "1.10.2", "type": "map", "orientation": "orthogonal",
        "renderorder": "right-down", "infinite": False, "width": lv["W"], "height": H,
        "tilewidth": tile, "tileheight": tile, "nextlayerid": 3, "nextobjectid": oid,
        "backgroundcolor": lv["background"],
        "tilesets": [{"firstgid": 1, "name": "tiles", "image": tileset, "imagewidth": tileset_size[0],
                      "imageheight": tileset_size[1], "tilewidth": tile, "tileheight": tile,
                      "tilecount": 4, "columns": 4, "margin": 0, "spacing": 0}],
        "layers": [
            {"id": 1, "name": "tiles", "type": "tilelayer", "width": lv["W"], "height": H, "x": 0, "y": 0,
             "opacity": 1, "visible": True, "data": data},
            {"id": 2, "name": "objects", "type": "objectgroup", "draworder": "topdown", "x": 0, "y": 0,
             "opacity": 1, "visible": True, "objects": objs},
        ],
    }


# --------------------------------------------------------------------------------------
# Preview PNG drawn from the real sprites
# --------------------------------------------------------------------------------------
def render_level(lv, level, sprs, path, T=16):
    by = {s["name"]: s for s in sprs}
    W, H = lv["W"], lv["H"]
    bg = tuple(int(lv["background"].lstrip("#")[i:i + 2], 16) for i in (0, 2, 4)) + (255,)
    img = Image.new("RGBA", (W * T, H * T), bg)
    tiles = {k: sprites_mod.sprite_image(by[n]).resize((T, T), Image.NEAREST)
             for k, n in (("#", "rock"), ("=", "rock"), ("M", "moss"), ("^", "spike"), ("~", "crumbly"))}
    for ch in SHOOTERS:
        tiles[ch] = tiles["#"].copy()
    for r, row in enumerate(lv["rows"]):
        for x, ch in enumerate(row):
            if ch in tiles:
                img.alpha_composite(tiles[ch], (x * T, r * T))

    for sh in level.get("shooters", []):
        sp = sprites_mod.sprite_image(by["shooter"]).resize((T, T), Image.NEAREST)
        sp = sp.rotate(math.degrees(math.atan2(sh["dy"], sh["dx"])))
        img.alpha_composite(sp, (int(round((sh["x"] - .5) * T)), int(round((H - sh["y"] - .5) * T))))

    def draw(name, wx, wy):
        s = by[name]
        k = T / float(s["ppu"])
        im = sprites_mod.sprite_image(s).resize((max(1, round(s["w"] * k)), max(1, round(s["h"] * k))), Image.NEAREST)
        left = wx * T - s["pivot"][0] * im.width
        top = (H - wy) * T - (1 - s["pivot"][1]) * im.height
        img.alpha_composite(im, (int(round(left)), int(round(top))))

    for p in level["savepoints"]:
        draw("savepoint", p["x"], p["y"])
    for p in level["gems"]:
        draw("gem", p["x"], p["y"])
    for e in level["enemies"]:
        draw(e["type"], e["x"], e["y"])
    draw("goal", level["goal"]["x"], level["goal"]["y"])
    draw("slime", level["spawn"][0], level["spawn"][1])
    img.convert("RGB").save(path)


# --------------------------------------------------------------------------------------
# Verification: can the player get from '@' to 'G'?
#   Breadth-first search over "standing tiles". Edges come from simulating the real
#   player (gen_slime.CONFIG): walk/fall off ledges, full and cut-short jumps with a
#   direction (optionally dropping input mid-air), and wall climbs on moss. The sim is
#   a tile-grid approximation of the Unity physics, so it can be wrong in either
#   direction at the margins - treat a FAIL as "look at this", not as proof.
# --------------------------------------------------------------------------------------
class Verifier:
    DT = 0.02
    T_MAX = 4.0

    def __init__(self, lv, cfg=None):
        cfg = cfg or gs.CONFIG
        p, w = cfg["player"], cfg["world"]
        self.g, self.damp = w["gravity"], p["linearDamping"]
        self.speed, self.jump, self.climb = p["moveSpeed"], p["jumpSpeed"], p["climbSpeed"]
        self.bw, self.bh = p["colliderW"], p["colliderH"]
        self.lv = lv
        self.solid = cells(lv, SOLID)
        self.moss = cells(lv, {"M"})
        self.spikes = cells(lv, {"^"})
        self.trail = set()      # half-tile cells the player's centre passed through

    def hit(self, cx, cy):
        """Box centred at (cx,cy): returns 'solid', 'spike' or None."""
        x0, x1 = math.floor(cx - self.bw / 2), math.floor(cx + self.bw / 2 - 1e-6)
        y0, y1 = math.floor(cy - self.bh / 2), math.floor(cy + self.bh / 2 - 1e-6)
        for tx in range(x0, x1 + 1):
            for ty in range(y0, y1 + 1):
                if (tx, ty) in self.solid:
                    return "solid"
        for tx in range(x0, x1 + 1):
            for ty in range(y0, y1 + 1):
                if (tx, ty) in self.spikes and cy - self.bh / 2 < ty + 0.6:
                    return "spike"
        return None

    def sim(self, fx, fy, d, vy0, release=None, switch=None, start=None, walking=False):
        """Returns the standing tile (tx, ty) reached, or None (died / fell / stuck)."""
        dt = self.DT
        cx, cy, vy, t = fx, fy + self.bh / 2 + 0.01, vy0, 0.0
        while t < self.T_MAX:
            if release is not None and t >= release and vy > 0:
                vy, release = 0.0, None
            if switch is not None and t >= switch:
                d, switch = 0, None
            vy = (vy + self.g * dt) * (1 - self.damp * dt)
            self.trail.add((round(cx * 2), round(cy * 2)))
            nx = cx + d * self.speed * dt
            h = self.hit(nx, cy)
            if h == "spike":
                return None
            if h != "solid":
                cx = nx
            ny = cy + vy * dt
            h = self.hit(cx, ny)
            if h == "spike":
                return None
            if h == "solid":
                if vy < 0:
                    feet = cy - self.bh / 2
                    surf = math.floor(feet + 0.05)
                    cy, vy = surf + self.bh / 2 + 0.002, 0.0
                    node = (math.floor(cx), surf)
                    if not walking or node != start:
                        return node
                else:
                    vy = 0.0
            else:
                cy = ny
            if cy < -3:
                return None
            t += dt
        return None

    def arcs(self, fx, fy, d, vy0):
        """One start state, every control variant: full jump, cut-short jumps, and letting go
        of the direction key mid-air (needed to land on narrow ledges)."""
        for rel in (None, 0.12, 0.25):
            yield self.sim(fx, fy, d, vy0, release=rel)
        for sw in (0.1, 0.15, 0.25, 0.4):
            yield self.sim(fx, fy, d, vy0, switch=sw)

    def edges(self, node):
        tx, ty = node
        out = set()

        def add(n):
            if n and n != node:
                out.add(n)

        for d in (-1, 1):
            add(self.sim(tx + 0.5, ty, d, 0.0, start=node, walking=True))      # walk / walk off a ledge
            edge = tx + (0.9 if d > 0 else 0.1)                                 # jump from the near edge...
            for n in self.arcs(edge, ty, d, self.jump):
                add(n)
            for n in self.arcs(tx + 0.5, ty, d, self.jump):                     # ...or the middle
                add(n)
        for rel in (None, 0.12, 0.25):
            add(self.sim(tx + 0.5, ty, 0, self.jump, release=rel))              # straight up
        # wall climb: a moss tile beside our feet, with a free shaft up to its top
        for d in (-1, 1):
            if (tx + d, ty) in self.moss:
                top = ty
                while (tx + d, top + 1) in self.moss:
                    top += 1
                if all((tx, y) not in self.solid for y in range(ty, top + 2)):
                    # leaving the top of the climb pops the player up at climbSpeed
                    for n in self.arcs(tx + 0.5, top + 0.2, d, self.climb):
                        add(n)
        return out

    def run(self):
        lv = self.lv
        spawn = entity_list(lv, "@")[0]
        goal = entity_list(lv, "G")[0]
        seen, q = {spawn: 0}, deque([spawn])
        while q:
            n = q.popleft()
            for m in self.edges(n):
                if m not in seen:
                    seen[m] = seen[n] + 1
                    q.append(m)
        def touched(p):    # gem centre vs. the player's box swept along every simulated path
            gx, gy = p[0] + .5, p[1] + .5
            return any(abs(tx / 2.0 - gx) <= (self.bw / 2 + .4) and abs(ty / 2.0 - gy) <= (self.bh / 2 + .4)
                       for tx in range(round(gx * 2) - 3, round(gx * 2) + 4)
                       for ty in range(round(gy * 2) - 3, round(gy * 2) + 4) if (tx, ty) in self.trail)
        return {
            "nodes": len(seen),
            "goal": any(abs(goal[0] - n[0]) <= 2 and abs(goal[1] - n[1]) <= 1 for n in seen),
            "savepoints_missed": [p for p in entity_list(lv, "S") if not any(abs(p[0] - n[0]) <= 1 and p[1] == n[1] for n in seen)],
            "gems_missed": [p for p in entity_list(lv, "o") if not touched(p)],
        }


# --------------------------------------------------------------------------------------
# Procedural cave -> ASCII (reuses gen_cave_level.Cave)
# --------------------------------------------------------------------------------------
def cave_ascii(seed=7, length=360, name="Cave"):
    import random
    import gen_cave_level as gcl
    cave = gcl.Cave(random.Random(seed), gs.CONFIG)
    goal = cave.build(length)
    cols = cave.cols
    pad = 2
    ymin = min(c["floor"] for c in cols) - 6
    ymax = max(c["ceil"] for c in cols) + 6
    W, H = len(cols) + 2 * pad, ymax - ymin
    g = [["#"] * W for _ in range(H)]                  # g[y][x], y up
    for i, c in enumerate(cols):
        for y in range(H):
            wy = y + ymin
            if c["floor"] <= wy < c["ceil"]:
                g[y][i + pad] = "."
        if c["climb"]:
            for wy in range(c["climb"][0], c["climb"][1]):
                g[wy - ymin][i + pad] = "M"
        if c["spike"]:
            g[c["floor"] - ymin][i + pad] = "^"
    for (x, y, w, h) in cave.platforms:
        for xx in range(w):
            for yy in range(h):
                g[y + yy - ymin][x + xx + pad] = "#"

    def put(ch, x, y):
        x, y = int(x) + pad, int(y) - ymin
        while y < H - 1 and g[y][x] != ".":
            y += 1
        g[y][x] = ch

    put("@", cave.savepoints[0][0] - 2, cave.savepoints[0][1])
    for x, y in cave.savepoints:
        put("S", x, y)
    for x, y in cave.gems:
        put("o", x, y)
    for t, x, y in cave.enemies:
        if t == "worm":
            put("w", x, y - 0.5)       # y is the body centre; the tile is the floor row
        else:
            put("b", x, y)
    put("G", goal[0], goal[1])
    rows = ["".join(g[y]) for y in range(H - 1, -1, -1)]
    return "; Procedural cave, seed %d, length %d\n@name %s\n@background #14101c\n%s\n" % (seed, length, name, "\n".join(rows))


# --------------------------------------------------------------------------------------
# Driver
# --------------------------------------------------------------------------------------
def write_sources(out, cave_seed, cave_length):
    d = os.path.join(out, "ascii", "levels")
    os.makedirs(d, exist_ok=True)
    for name, text in HAND_LEVELS.items():
        with open(os.path.join(d, name + ".txt"), "w") as f:
            f.write(text.lstrip("\n"))
    with open(os.path.join(d, "cave.txt"), "w") as f:
        f.write(cave_ascii(cave_seed, cave_length))
    return d


def process(path, out, sprs, verify=True):
    name = os.path.splitext(os.path.basename(path))[0]
    with open(path) as f:
        lv = parse_level(f.read(), name)
    level = to_level(lv)
    ld = os.path.join(out, "levels")
    os.makedirs(ld, exist_ok=True)
    with open(os.path.join(ld, name + ".json"), "w") as f:
        json.dump(level, f, indent=1)
    with open(os.path.join(ld, name + ".tmj"), "w") as f:
        json.dump(to_tmj(lv, level), f)
    render_level(lv, level, sprs, os.path.join(ld, name + ".png"))
    info = "%-9s %3dx%-3d tiles -> %d wall rects, %d climbs, %d spike runs, %d gems, %d enemies, %d savepoints" % (
        name, lv["W"], lv["H"], len(level["walls"]), len(level["climbables"]), len(level["spikes"]),
        len(level["gems"]), len(level["enemies"]), len(level["savepoints"]))
    print(info)
    ok = True
    if verify:
        r = Verifier(lv).run()
        if "lasso" in lv["requires"]:
            ok = not r["goal"]
            print("          verify: needs the lasso: %s (%d standing tiles reachable by walking and jumping)" % (
                "NOT passable without it, as designed" if ok else "PASSABLE WITHOUT IT - the level does not need the lasso!",
                r["nodes"]))
        else:
            ok = r["goal"]
            print("          verify: goal %s (%d standing tiles reachable)%s%s" % (
                "REACHABLE" if r["goal"] else "NOT REACHABLE", r["nodes"],
                "; savepoints not reached: %s" % r["savepoints_missed"] if r["savepoints_missed"] else "",
                "; gems not reached: %s" % r["gems_missed"] if r["gems_missed"] else ""))
    return name, lv, level, ok


def main(argv=None):
    ap = argparse.ArgumentParser(description="ASCII levels -> level.json / .tmj / preview + verification")
    ap.add_argument("--out", default=DEFAULT_OUT)
    ap.add_argument("--src", help="translate this dir of level .txt files instead of the built-in ones")
    ap.add_argument("--cave-seed", type=int, default=7)
    ap.add_argument("--cave-length", type=int, default=360)
    ap.add_argument("--unity", help="also write a Unity project here")
    ap.add_argument("--level", default="cave", help="which level goes into the Unity project")
    ap.add_argument("--prowl2d", help="also write a Prowl2D game folder here (subset C#)")
    ap.add_argument("--stride2d", help="also write a Stride2D game folder here (subset C#)")
    ap.add_argument("--max-frames", type=int, default=3000, help="frames the bot may play in the 2D engines")
    ap.add_argument("--no-verify", action="store_true")
    ap.add_argument("--force", action="store_true", help="overwrite a non-generated --unity dir")
    a = ap.parse_args(argv)

    ascii_dir = os.path.join(a.out, "ascii")
    if not os.path.exists(os.path.join(ascii_dir, "palette.txt")):
        sprites_mod.write_ascii(ascii_dir)             # make sure sprites exist for previews
    sprs = sprites_mod.load_dir(ascii_dir)
    if a.src:
        src = a.src
    else:
        src = write_sources(a.out, a.cave_seed, a.cave_length)
    results = {}
    for fn in sorted(os.listdir(src)):
        if fn.endswith(".txt"):
            name, lv, level, ok = process(os.path.join(src, fn), a.out, sprs, not a.no_verify)
            results[name] = (level, ok)
    if a.unity:
        if a.level not in results:
            raise SystemExit("no level named %r (have: %s)" % (a.level, ", ".join(results)))
        gs.write_project(a.unity, results[a.level][0], sprites=sprites_mod.sprite_sheet(sprs), force=a.force)
        print("wrote Unity project:", a.unity, "(level %s)" % a.level)
    for flag, engine in ((a.prowl2d, "prowl2d"), (a.stride2d, "stride2d")):
        if flag:
            if a.level not in results:
                raise SystemExit("no level named %r (have: %s)" % (a.level, ", ".join(results)))
            gs.write_subset_project(flag, results[a.level][0], engine, a.max_frames, force=a.force)
            print("wrote %s game: %s (level %s)" % (engine, flag, a.level))
    return 0 if all(ok for _, ok in results.values()) else 2


if __name__ == "__main__":
    sys.exit(main())

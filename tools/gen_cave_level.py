#!/usr/bin/env python3
"""gen_cave_level.py - (procedural generator; gen_levels.py rasterises it to ASCII and is the main entry point)

generate "Slime Jump: The Cave" (worms + bats) and emit a Unity project.

Builds a side-scrolling cave as a per-column profile (floor height / ceiling height), then
converts it into the engine-agnostic level format gen_slime understands: axis-aligned
rectangles (walls, climbable walls, spikes) plus points (savepoints, gems, goal, enemies).

Everything is sized from gen_slime.jump_capabilities(), i.e. from the real player
tunables, so the level stays passable if CONFIG changes:
    * floor steps are <= MAX_STEP (jumpable)
    * pits / gaps are <= SAFE_GAP wide (jumpable, with margin)
    * climb walls are taller than a jump apex (must be climbed)
    * the long "lasso gap" has stepping platforms, so the lasso is a shortcut, not a
      requirement (the swing code is the least-tested part of the port)

Usage:
    python3 tools/gen_cave_level.py [--seed 7] [--length 360] [--out /tmp/SlimeJumpCave]
                                    [--preview /tmp/cave.png] [--level-json /tmp/cave.json]
"""
import argparse
import json
import os
import random
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import gen_slime as gs  # noqa: E402

CHAMBER_H = 14      # normal floor->ceiling height (apex is ~4, camera half-height 11)
CAVERN_H = 20       # tall caverns where bats live
LASSO_CEIL = 7      # ceiling height above the platforms in the lasso gap (rope length is 7)


class Cave:
    def __init__(self, rnd, cfg):
        self.rnd = rnd
        self.cfg = cfg
        apex, air, dist = gs.jump_capabilities(cfg)
        self.apex, self.air, self.jump_dist = apex, air, dist
        self.max_step = max(1, int(apex * 0.6))          # climbable by a plain jump
        self.safe_gap = max(3, int(dist * 0.5))           # pit width with plenty of margin
        self.climb_min = int(apex) + 3                    # taller than a jump => must climb
        self.cols = []          # one dict per x: floor, ceil, spike, climb
        self.platforms = []     # (x, y, w, h) floating rects
        self.savepoints, self.gems, self.enemies = [], [], []
        self.floor_min, self.floor_max = 4, 26
        self.height = CHAMBER_H

    # ---- primitives -------------------------------------------------------------
    @property
    def x(self):
        return len(self.cols)

    @property
    def floor(self):
        return self.cols[-1]["floor"]

    def column(self, floor, ceil=None, spike=False, climb=None):
        self.cols.append({"floor": floor, "ceil": ceil if ceil is not None else floor + self.height,
                          "spike": spike, "climb": climb})

    def gem(self, x, y):
        self.gems.append((x, y))

    # ---- segments ---------------------------------------------------------------
    def flat(self, n, walk=True):
        """Gently rolling ground; floor changes by at most max_step, then holds a few columns."""
        hold = 0
        for _ in range(n):
            f = self.floor if self.cols else 6
            if walk and hold <= 0 and self.rnd.random() < 0.18:
                f = min(self.floor_max, max(self.floor_min, f + self.rnd.choice([-2, -1, 1, 2])))
                f = max(self.floor - self.max_step, min(self.floor + self.max_step, f))
                hold = 4
            hold -= 1
            self.column(f)

    def rest(self):
        """Flat area with a savepoint in the middle."""
        start = self.x
        self.flat(9, walk=False)
        self.savepoints.append((start + 4.5, self.floor))

    def pit(self):
        """Spiked pit, narrower than the safe jump distance. Gems float over it."""
        w = self.rnd.randint(max(3, self.safe_gap // 2), self.safe_gap)
        base, ceil = self.floor, self.cols[-1]["ceil"]
        start = self.x
        for _ in range(w):
            self.column(base - 6, ceil=ceil, spike=True)
        self.gem(start + w / 2.0, base + self.apex * 0.7)
        # landing: same level +-1 so the far edge is always reachable
        self.flat(4, walk=False)
        self.cols[-4:] = [dict(c, floor=base + self.rnd.choice([-1, 0, 1])) if i == 0 else c
                          for i, c in enumerate(self.cols[-4:])]
        f = self.cols[-4]["floor"]
        for c in self.cols[-4:]:
            c["floor"] = f
            c["ceil"] = max(ceil, f + self.height)

    def worm_flat(self):
        """Long flat stretch with a worm patrolling the middle (patrolRange 5 each way)."""
        n = 26
        start = self.x
        self.flat(n, walk=False)
        wc = self.cfg["worm"]
        self.enemies.append(("worm", start + n / 2.0, self.floor + wc["colliderH"] / 2.0))
        self.gem(start + n - 3, self.floor + 1.5)

    def cavern(self):
        """Tall chamber with bats hanging near the ceiling."""
        old_h, self.height = self.height, CAVERN_H
        n = 28
        start = self.x
        self.flat(n, walk=False)
        base = self.floor
        for i in range(self.rnd.randint(2, 3)):
            bx = start + (i + 1) * n / 4.0 + self.rnd.uniform(-2, 2)
            self.enemies.append(("bat", bx, base + CAVERN_H - 3.5))
        self.gem(start + n / 2.0, base + 3)     # must stay within jump reach (apex ~4)
        self.height = old_h
        # settle ceiling back to a normal chamber over the next columns
        for k in range(6):
            self.column(base, ceil=base + CAVERN_H - (CAVERN_H - old_h) * (k + 1) / 6.0)

    def climb(self):
        """A sheer climbable wall taller than a jump; alcove before it has a tall ceiling."""
        base = self.floor
        rise = self.rnd.randint(self.climb_min, self.climb_min + 3)
        new_floor = min(self.floor_max, base + rise)
        rise = new_floor - base
        if rise < self.climb_min:     # no headroom in the allowed floor range: skip
            return False
        tall_ceil = new_floor + self.height
        for c in self.cols[-5:]:
            c["ceil"] = max(c["ceil"], tall_ceil)
        for _ in range(4):
            self.column(base, ceil=tall_ceil)
        self.column(new_floor, ceil=tall_ceil + 0, climb=(base, new_floor))
        self.flat(6, walk=False)
        self.gem(self.x - 5, new_floor + 2)
        return True

    def drop(self):
        """Walk off ledges down to a lower floor, in steps small enough that each new
        ceiling stays above the old floor (otherwise the old floor becomes a sealed pocket)."""
        target = max(self.floor_min + 6, 10)
        step = max(2, self.height - 8)
        while self.floor > target:
            f = max(target, self.floor - step)
            self.flat(2, walk=False)
            for _ in range(5):
                self.column(f)

    def lasso_gap(self):
        """Very wide spiked gap with low ceiling and stepping platforms. Jumpable via the
        platforms; the ceiling is within rope length so the lasso can be used instead."""
        base = self.floor
        width = int(self.jump_dist * 1.6)
        start = self.x
        for _ in range(width):
            self.column(base - 8, ceil=base + LASSO_CEIL, spike=True)
        step = max(6, int(self.safe_gap * 0.8))
        px = start + step - 2
        while px + 4 < start + width - 2:
            self.platforms.append((px, base - 1, 4, 1))
            self.gem(px + 2, base + 1.5)
            px += step + 4
        for _ in range(4):
            self.column(base, ceil=base + LASSO_CEIL)
        # open the ceiling again after the gap
        for k in range(5):
            self.column(base, ceil=base + LASSO_CEIL + (self.height - LASSO_CEIL) * (k + 1) / 5.0)

    # ---- assembly ---------------------------------------------------------------
    def build(self, length):
        self.flat(6, walk=False)
        self.rest()
        plan = ["worm", "pit", "climb", "cavern", "lasso", "pit", "worm", "cavern", "pit", "climb"]
        self.rnd.shuffle(plan)
        plan = [p for p in plan if p != "lasso"]
        plan.insert(len(plan) // 2, "lasso")        # exactly one lasso gap, mid-level
        segs_since_rest = 0
        i = 0
        while self.x < length:
            kind = plan[i] if i < len(plan) else self.rnd.choice(["worm", "pit", "cavern", "pit"])
            i += 1
            if self.floor > 18 and kind != "climb":
                self.drop()
            if kind == "worm":
                self.worm_flat()
            elif kind == "pit":
                self.pit()
            elif kind == "cavern":
                self.cavern()
            elif kind == "lasso":
                self.lasso_gap()
            elif kind == "climb":
                if not self.climb():
                    self.drop()
            segs_since_rest += 1
            if segs_since_rest >= 2:
                self.rest()
                segs_since_rest = 0
        self.rest()
        self.flat(8, walk=False)
        goal = (self.x - 4, self.floor)
        self.flat(6, walk=False)
        return goal


def level_from_cave(cave, goal, name="Cave"):
    cols = cave.cols
    ymin = min(c["floor"] for c in cols) - 6
    ymax = max(c["ceil"] for c in cols) + 6

    def spans(c):
        out = []
        if c["climb"]:
            y0, y1 = c["climb"]
            out += [(ymin, y0, "wall"), (y0, y1, "climb")]
            if y1 < c["floor"]:
                out.append((y1, c["floor"], "wall"))
        else:
            out.append((ymin, c["floor"], "wall"))
        out.append((c["ceil"], ymax, "wall"))
        return tuple(out)

    walls, climbs = [], []
    i = 0
    while i < len(cols):                     # merge runs of identical columns into rects
        s = spans(cols[i])
        j = i
        while j + 1 < len(cols) and spans(cols[j + 1]) == s:
            j += 1
        for (y0, y1, kind) in s:
            r = {"x": i, "y": y0, "w": j - i + 1, "h": y1 - y0}
            (climbs if kind == "climb" else walls).append(r)
        i = j + 1
    # outer end caps
    walls.append({"x": -8, "y": ymin, "w": 8, "h": ymax - ymin})
    walls.append({"x": len(cols), "y": ymin, "w": 8, "h": ymax - ymin})
    for (x, y, w, h) in cave.platforms:
        walls.append({"x": x, "y": y, "w": w, "h": h})

    spikes, i = [], 0
    while i < len(cols):
        if cols[i]["spike"]:
            j = i
            while j + 1 < len(cols) and cols[j + 1]["spike"] and cols[j + 1]["floor"] == cols[i]["floor"]:
                j += 1
            spikes.append({"x": i, "y": cols[i]["floor"], "w": j - i + 1, "h": 1})
            i = j + 1
        else:
            i += 1

    first = cave.savepoints[0]
    pc = cave.cfg["player"]
    level = {
        "name": name,
        "background": "#14101c",
        "bounds": [0, ymin, len(cols), ymax],
        "spawn": [first[0], first[1] + pc["colliderH"] / 2 + 0.2],
        "walls": walls, "climbables": climbs, "spikes": spikes,
        "savepoints": [{"name": "Save %d" % (k + 1), "x": x, "y": y}
                       for k, (x, y) in enumerate(cave.savepoints)],
        "gems": [{"name": "Gem %d" % (k + 1), "x": x, "y": y} for k, (x, y) in enumerate(cave.gems)],
        "enemies": [{"type": t, "x": x, "y": y} for (t, x, y) in cave.enemies],
        "goal": {"name": "Goal", "x": goal[0], "y": goal[1] + 1.0},
    }
    return level


def check_level(level, cave):
    """Cheap structural guarantees (not a full playthrough simulation)."""
    cols = cave.cols
    for a, b in zip(cols, cols[1:]):
        if a["spike"] or b["spike"] or b["climb"]:
            continue
        step = b["floor"] - a["floor"]
        # drops are fine; rises beyond a jump must be climbs or the pit->landing edge
        assert step <= cave.max_step, "unjumpable step %d at floor %d" % (step, a["floor"])
    run = 0
    for c in cols:
        run = run + 1 if c["spike"] else 0
        assert run <= cave.jump_dist * 1.7, "gap too wide"
    for e in level["enemies"]:
        assert level["bounds"][1] < e["y"] < level["bounds"][3], e
    assert level["goal"], "no goal"
    # neighbouring columns must share an opening (else a ledge becomes a sealed pocket)
    for i, (a, b) in enumerate(zip(cols, cols[1:])):
        opening = min(a["ceil"], b["ceil"]) - max(a["floor"], b["floor"])
        assert opening >= 4, "blocked passage between columns %d and %d (opening %d)" % (i, i + 1, opening)
    # headroom everywhere the player can stand (pits excluded): a full jump must fit.
    # The lasso gap's ceiling is deliberately low (LASSO_CEIL = rope length) but still clears a jump.
    for i, c in enumerate(cols):
        if not c["spike"]:
            assert c["ceil"] - c["floor"] >= cave.apex + 2, "low ceiling at column %d" % i
    # every hop across a spiked gap (edge->platform->platform->edge) must be well inside jump range
    reach = cave.jump_dist * 0.7
    i = 0
    while i < len(cols):
        if not cols[i]["spike"]:
            i += 1
            continue
        j = i
        while j + 1 < len(cols) and cols[j + 1]["spike"]:
            j += 1
        # stand points inside this gap: the platforms whose x-range lies in [i, j]
        pl = sorted((x, x + w) for (x, y, w, h) in cave.platforms if i <= x <= j)
        edges = [i] + [v for seg in pl for v in seg] + [j + 1]
        for a_end, b_start in zip(edges[0::2], edges[1::2]):
            assert b_start - a_end <= reach, "hop of %.1f > %.1f across gap at %d" % (b_start - a_end, reach, i)
        i = j + 1


def preview(level, path, scale=6):
    from PIL import Image, ImageDraw
    xmin, ymin, xmax, ymax = level["bounds"]
    W, H = int((xmax - xmin) * scale), int((ymax - ymin) * scale)
    img = Image.new("RGB", (W, H), (20, 16, 28))
    d = ImageDraw.Draw(img)

    def R(r, col):
        x0 = (r["x"] - xmin) * scale
        y1 = H - (r["y"] - ymin) * scale
        d.rectangle([x0, y1 - r["h"] * scale, x0 + r["w"] * scale - 1, y1 - 1], fill=col)

    for r in level["walls"]:
        R(r, (74, 66, 88))
    for r in level["climbables"]:
        R(r, (87, 145, 76))
    for r in level["spikes"]:
        R(r, (230, 230, 245))

    def P(p, col, rad):
        x, y = (p["x"] - xmin) * scale, H - (p["y"] - ymin) * scale
        d.ellipse([x - rad, y - rad, x + rad, y + rad], fill=col)

    for p in level["savepoints"]:
        P(p, (255, 228, 94), 6)
    for p in level["gems"]:
        P(p, (111, 240, 255), 4)
    for e in level["enemies"]:
        P(e, (227, 166, 192) if e["type"] == "worm" else (180, 90, 255), 6)
    P(level["goal"], (181, 140, 255), 9)
    P({"x": level["spawn"][0], "y": level["spawn"][1]}, (123, 224, 123), 6)
    img.save(path)


def main():
    ap = argparse.ArgumentParser(description=__doc__.split("\n")[0])
    ap.add_argument("--seed", type=int, default=7)
    ap.add_argument("--length", type=int, default=360, help="approx. level width in tiles")
    ap.add_argument("--out", default="/tmp/SlimeJumpCave")
    ap.add_argument("--unity-version", default=gs.DEFAULT_UNITY_VERSION)
    ap.add_argument("--input-system", action="store_true")
    ap.add_argument("--preview", help="write a PNG overview (needs Pillow)")
    ap.add_argument("--level-json", help="also write the level JSON here")
    ap.add_argument("--check", action="store_true", help="tree-sitter syntax check of the C#")
    ap.add_argument("--force", action="store_true")
    a = ap.parse_args()

    cave = Cave(random.Random(a.seed), gs.CONFIG)
    goal = cave.build(a.length)
    level = level_from_cave(cave, goal)
    check_level(level, cave)
    print("seed %d: %d columns, %d wall rects, %d climbs, %d spike runs, %d worms, %d bats, %d gems, %d savepoints"
          % (a.seed, len(cave.cols), len(level["walls"]), len(level["climbables"]), len(level["spikes"]),
             sum(e["type"] == "worm" for e in level["enemies"]), sum(e["type"] == "bat" for e in level["enemies"]),
             len(level["gems"]), len(level["savepoints"])))
    print("player can: jump %.1f high, clear %.1f wide; generator uses step<=%d gap<=%d climb>=%d"
          % (cave.apex, cave.jump_dist, cave.max_step, cave.safe_gap, cave.climb_min))
    if a.level_json:
        with open(a.level_json, "w") as f:
            json.dump(level, f, indent=1)
    if a.preview:
        preview(level, a.preview)
        print("preview:", a.preview)
    files = gs.write_project(a.out, level, a.unity_version, input_system=a.input_system, force=a.force)
    print("wrote Unity project:", a.out)
    if a.check:
        probs = gs.check_csharp(files)
        if probs is None:
            print("tree-sitter not installed (pip install tree-sitter tree-sitter-c-sharp)")
        elif probs:
            print("\n".join(probs))
            return 1
        else:
            print("C# syntax check passed (%d files)" % len(files))
    return 0


if __name__ == "__main__":
    sys.exit(main())

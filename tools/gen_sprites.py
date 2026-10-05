#!/usr/bin/env python3
"""gen_sprites.py - ASCII art -> pixel sprites, tileset, atlas (PIL).

The art lives as plain text. One character is one pixel and one character always means
one colour (shared palette.txt), so art is easy to author, diff and review in git.

PIPELINE
    1. write the source ASCII into  <out>/ascii/   (palette.txt + sprites/*.txt)
       (or use --src DIR to render hand-edited ASCII instead of the built-in art)
    2. render with PIL into         <out>/png/<name>.png        one PNG per sprite
                                    <out>/atlas.png + atlas.json  packed atlas (any engine)
                                    <out>/tileset.png            the 8x8 tiles, in a row
                                    <out>/sprites.json           what gen_slime.py consumes
                                    <out>/preview.png            labelled, scaled contact sheet

SPRITE FILE FORMAT  (ascii/sprites/<name>.txt)
    ; comment
    @name  slime            sprite name (defaults to the file name)
    @ppu   14               pixels per world unit
    @pivot 0.5 0.5          x y, 0..1 from the bottom-left
    @kind  sprite|tile      tiles also go into tileset.png and repeat when drawn
    @pal   x #rrggbb        optional per-sprite colour override/addition
    <rows>                  top row first; '.' is transparent; no spaces

USAGE
    python3 tools/gen_sprites.py [--out /tmp/slimejump_art] [--src DIR] [--scale 8]
"""
import argparse
import json
import os
import sys

from PIL import Image, ImageDraw

DEFAULT_OUT = "/tmp/slimejump_art"

# --------------------------------------------------------------------------------------
# Shared palette: one character = one colour in every sprite.
# --------------------------------------------------------------------------------------
PALETTE = [
    ("k", "#101010", "black"), ("w", "#ffffff", "white"),
    ("g", "#7be07b", "slime body"), ("G", "#3f9a4a", "slime outline"), ("h", "#c9ffc9", "slime highlight"),
    ("p", "#e3a6c0", "worm body"), ("P", "#9c5a7a", "worm outline"),
    ("B", "#5a3d7a", "bat body"), ("b", "#3b2750", "bat shade"), ("e", "#ff4040", "red eye"),
    ("r", "#4a4258", "rock"), ("R", "#3a3347", "rock dark"), ("q", "#5c5470", "rock light"),
    ("m", "#57914c", "moss"), ("M", "#2d4f2e", "moss dark"), ("l", "#7bbf63", "moss light"),
    ("c", "#6ff0ff", "gem"), ("C", "#2ab0d0", "gem dark"),
    ("a", "#d8d8e8", "metal light"), ("A", "#9a9ab0", "metal dark"),
    ("S", "#6b6478", "stone"), ("y", "#ffe45e", "lit crystal"), ("n", "#6a5a8a", "unlit crystal"),
    ("v", "#7a4fd0", "portal dark"), ("V", "#b58cff", "portal"), ("F", "#f2e6ff", "portal core"),
    ("x", "#3fbf5f", "green shot rim"), ("X", "#d8ffd8", "green shot core"),
    ("z", "#d03030", "red shot rim"), ("Z", "#ffd8d8", "red shot core"),
]

# --------------------------------------------------------------------------------------
# Built-in art. Edit here, or dump with this script and edit the .txt files.
# --------------------------------------------------------------------------------------
SPRITES = {}

SPRITES["slime"] = """
@ppu 14
@pivot 0.5 0.5
....GGGGGG....
..GGhggggggG..
.GhggggggggggG
.GggwwggwwggG.
.GggwkggwkggG.
.GggggggggggG.
GggggggggggggG
GggggggggggggG
GGggggggggggGG
.GGGGGGGGGGGG.
"""

SPRITES["worm"] = """
@ppu 16
@pivot 0.5 0.5
..........PPPP..
...PPP...PppppP.
..PpppP.PpwkppP.
.PpppppPPppppppP
PpppppppppppppP.
PPpppppppppppPP.
.PPPPPPPPPPPPP..
................
"""

SPRITES["bat"] = """
@ppu 16
@pivot 0.5 0.5
......B..B......
B....BBBBBB....B
BB..BBBBBBBB..BB
BBBBBBeBBeBBBBBB
.BBBBBBBBBBBBBB.
..bBBBBBBBBBBb..
...b.BBBBBB.b...
.......BB.......
"""

SPRITES["gem"] = """
@ppu 10
@pivot 0.5 0.5
...cc...
..cwcc..
.cwccCc.
cccccccC
.cccccC.
..ccCC..
...cC...
........
"""

SPRITES["savepoint"] = """
@ppu 10
@pivot 0.5 0
....nn....
...nnnn...
..nnwnnn..
.nnwnnnnn.
.nnnnnnnn.
.nnnnnnnn.
..nnnnnn..
..nnnnnn..
...nnnn...
....nn....
..SSSSSS..
.SSSSSSSS.
SSSSSSSSSS
SSSSSSSSSS
"""

SPRITES["savepoint_on"] = """
@ppu 10
@pivot 0.5 0
....yy....
...yyyy...
..yywyyy..
.yywyyyyy.
.yyyyyyyy.
.yyyyyyyy.
..yyyyyy..
..yyyyyy..
...yyyy...
....yy....
..SSSSSS..
.SSSSSSSS.
SSSSSSSSSS
SSSSSSSSSS
"""

SPRITES["goal"] = """
@ppu 8
@pivot 0.5 0.5
....vvvv....
..vvVVVVvv..
.vVVVVVVVVv.
.vVVFFFFVVv.
vVVFFFFFFVVv
vVFFFFFFFFVv
vVFFFFFFFFVv
vVFFFFFFFFVv
vVFFFFFFFFVv
vVFFFFFFFFVv
vVVFFFFFFVVv
.vVVFFFFVVv.
.vVVVVVVVVv.
..vvVVVVvv..
....vvvv....
............
"""

SPRITES["bullet_green"] = """
@ppu 10
@pivot 0.5 0.5
.xx.
xXXx
xXXx
.xx.
"""

SPRITES["bullet_red"] = """
@ppu 10
@pivot 0.5 0.5
.zz.
zZZz
zZZz
.zz.
"""

SPRITES["fragment"] = """
@ppu 8
@pivot 0.5 0.5
gg
gg
"""

# Tiles (repeat when drawn). Edges are seamless: the left/right and top/bottom pairs match.
SPRITES["rock"] = """
@kind tile
@ppu 8
rrrrqrrr
rrRrrrrr
rqrrrrRr
rrrrrqrr
RrrrrrrR
rrrqrrrr
rrrrrrRr
rRrrrqrr
"""

SPRITES["moss"] = """
@kind tile
@ppu 8
mmlmmmlm
mMmmmlmm
mmmMmmmm
lmmmmMmm
mmlmmmmM
mMmmmlmm
mmmmMmmm
lmmmmmlm
"""

SPRITES["spike"] = """
@kind tile
@ppu 8
.aA..aA.
.aA..aA.
.aA..aA.
aaAAaaAA
aaAAaaAA
aaAAaaAA
aaAAaaAA
aaAAaaAA
"""

TILE_ORDER = ["rock", "moss", "spike"]    # gid 1,2,3 in tileset.png / the .tmj export


# --------------------------------------------------------------------------------------
# Parsing
# --------------------------------------------------------------------------------------
def _hex(s):
    s = s.lstrip("#")
    return tuple(int(s[i:i + 2], 16) for i in (0, 2, 4))


def parse_palette(text):
    pal = {}
    for line in text.splitlines():
        line = line.split(";")[0].strip()
        if line:
            ch, col = line.split()[:2]
            pal[ch] = col
    return pal


def palette_text():
    return "; one character = one colour, shared by every sprite\n" + "".join(
        "%s %s ; %s\n" % (c, h, d) for c, h, d in PALETTE)


def parse_sprite(text, palette, default_name="sprite"):
    sp = {"name": default_name, "ppu": 16, "pivot": (0.5, 0.5), "kind": "sprite", "rows": [], "pal": dict(palette)}
    for line in text.splitlines():
        line = line.rstrip()
        if not line or line.startswith(";"):
            continue
        if line.startswith("@"):
            parts = line[1:].split()
            key, args = parts[0], parts[1:]
            if key == "name":
                sp["name"] = args[0]
            elif key == "ppu":
                sp["ppu"] = int(args[0])
            elif key == "pivot":
                sp["pivot"] = (float(args[0]), float(args[1]))
            elif key == "kind":
                sp["kind"] = args[0]
            elif key == "pal":
                sp["pal"][args[0]] = args[1]
            else:
                raise ValueError("%s: unknown directive @%s" % (default_name, key))
        else:
            sp["rows"].append(line)
    rows = sp["rows"]
    if not rows:
        raise ValueError("%s: no pixel rows" % sp["name"])
    w = len(rows[0])
    for i, r in enumerate(rows):
        if len(r) != w:
            raise ValueError("%s: row %d is %d wide, expected %d" % (sp["name"], i, len(r), w))
        for ch in r:
            if ch != "." and ch not in sp["pal"]:
                raise ValueError("%s: row %d uses '%s' which is not in the palette" % (sp["name"], i, ch))
    sp["w"], sp["h"] = w, len(rows)
    return sp


def builtin_sprites():
    pal = parse_palette(palette_text())
    return [parse_sprite(t, pal, n) for n, t in SPRITES.items()]


def load_dir(src):
    """Load palette.txt + sprites/*.txt from an ASCII source directory."""
    with open(os.path.join(src, "palette.txt")) as f:
        pal = parse_palette(f.read())
    out = []
    sdir = os.path.join(src, "sprites")
    for fn in sorted(os.listdir(sdir)):
        if fn.endswith(".txt"):
            with open(os.path.join(sdir, fn)) as f:
                out.append(parse_sprite(f.read(), pal, fn[:-4]))
    return out


def write_ascii(src):
    os.makedirs(os.path.join(src, "sprites"), exist_ok=True)
    with open(os.path.join(src, "palette.txt"), "w") as f:
        f.write(palette_text())
    for n, t in SPRITES.items():
        with open(os.path.join(src, "sprites", n + ".txt"), "w") as f:
            f.write("; %s\n@name %s\n%s" % (n, n, t.lstrip("\n")))


# --------------------------------------------------------------------------------------
# Rendering
# --------------------------------------------------------------------------------------
def sprite_image(sp):
    img = Image.new("RGBA", (sp["w"], sp["h"]), (0, 0, 0, 0))
    px = img.load()
    for y, row in enumerate(sp["rows"]):
        for x, ch in enumerate(row):
            if ch != ".":
                px[x, y] = _hex(sp["pal"][ch]) + (255,)
    return img


def sprite_sheet(sprites):
    """The sprites.json structure gen_slime.py / Unity consume (rows + per-sprite palette)."""
    out = []
    for sp in sprites:
        used = sorted({c for r in sp["rows"] for c in r if c != "."})
        out.append({"name": sp["name"], "ppu": sp["ppu"], "pivotX": sp["pivot"][0], "pivotY": sp["pivot"][1],
                    "palette": [c + sp["pal"][c] for c in used], "rows": sp["rows"]})
    return {"sprites": out}


def default_sprite_sheet():
    """In-memory built-in sprite sheet (used by gen_slime.py without touching disk)."""
    return sprite_sheet(builtin_sprites())


def build_atlas(sprites, max_w=128, pad=1):
    """Shelf-pack all sprites into one image. Returns (image, frames dict)."""
    order = sorted(sprites, key=lambda s: (-s["h"], s["name"]))
    x = y = shelf_h = 0
    placed = {}
    for sp in order:
        if x + sp["w"] + pad > max_w:
            x, y, shelf_h = 0, y + shelf_h + pad, 0
        placed[sp["name"]] = (x + pad, y + pad)
        x += sp["w"] + pad
        shelf_h = max(shelf_h, sp["h"] + pad)
    W = max_w
    H = y + shelf_h + pad
    atlas = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    frames = {}
    for sp in sprites:
        ax, ay = placed[sp["name"]]
        atlas.paste(sprite_image(sp), (ax, ay))
        frames[sp["name"]] = {"x": ax, "y": ay, "w": sp["w"], "h": sp["h"], "ppu": sp["ppu"],
                              "pivot": list(sp["pivot"]), "kind": sp["kind"]}
    return atlas, frames


def build_tileset(sprites):
    by = {s["name"]: s for s in sprites}
    tiles = [by[n] for n in TILE_ORDER if n in by]
    ts = max(t["w"] for t in tiles)
    img = Image.new("RGBA", (ts * len(tiles), ts), (0, 0, 0, 0))
    for i, t in enumerate(tiles):
        img.paste(sprite_image(t), (i * ts, 0))
    return img, ts, [t["name"] for t in tiles]


def preview_sheet(sprites, scale):
    cell_pad, label_h = 6, 12
    cells = []
    for sp in sprites:
        cells.append((sp, sprite_image(sp).resize((sp["w"] * scale, sp["h"] * scale), Image.NEAREST)))
    cw = max(c[1].width for c in cells) + cell_pad * 2
    ch = max(c[1].height for c in cells) + cell_pad * 2 + label_h
    cols = 4
    rows = (len(cells) + cols - 1) // cols
    sheet = Image.new("RGBA", (cols * cw, rows * ch), (28, 22, 38, 255))
    d = ImageDraw.Draw(sheet)
    for i, (sp, im) in enumerate(cells):
        cx, cy = (i % cols) * cw, (i // cols) * ch
        # checker behind the sprite so transparency is visible
        for yy in range(0, im.height, 8):
            for xx in range(0, im.width, 8):
                col = (46, 40, 58, 255) if (xx // 8 + yy // 8) % 2 else (38, 32, 50, 255)
                d.rectangle([cx + cell_pad + xx, cy + cell_pad + yy,
                             cx + cell_pad + xx + 7, cy + cell_pad + yy + 7], fill=col)
        sheet.alpha_composite(im, (cx + cell_pad, cy + cell_pad))
        d.text((cx + cell_pad, cy + ch - label_h - 2), "%s %dx%d" % (sp["name"], sp["w"], sp["h"]),
               fill=(200, 200, 215, 255))
    return sheet


def render_all(sprites, out, scale=8):
    os.makedirs(os.path.join(out, "png"), exist_ok=True)
    for sp in sprites:
        sprite_image(sp).save(os.path.join(out, "png", sp["name"] + ".png"))
    atlas, frames = build_atlas(sprites)
    atlas.save(os.path.join(out, "atlas.png"))
    with open(os.path.join(out, "atlas.json"), "w") as f:
        json.dump({"image": "atlas.png", "size": [atlas.width, atlas.height], "frames": frames}, f, indent=1)
    ts_img, ts, names = build_tileset(sprites)
    ts_img.save(os.path.join(out, "tileset.png"))
    with open(os.path.join(out, "tileset.json"), "w") as f:
        json.dump({"image": "tileset.png", "tile": ts, "tiles": names, "firstgid": 1}, f, indent=1)
    with open(os.path.join(out, "sprites.json"), "w") as f:
        json.dump(sprite_sheet(sprites), f, indent=1)
    preview_sheet(sprites, scale).save(os.path.join(out, "preview.png"))


def main(argv=None):
    ap = argparse.ArgumentParser(description="ASCII art -> sprites/atlas/tileset (PIL)")
    ap.add_argument("--out", default=DEFAULT_OUT)
    ap.add_argument("--src", help="render this ASCII source dir instead of the built-in art")
    ap.add_argument("--scale", type=int, default=8, help="preview scale")
    a = ap.parse_args(argv)
    if a.src:
        sprites = load_dir(a.src)
    else:
        write_ascii(os.path.join(a.out, "ascii"))
        sprites = load_dir(os.path.join(a.out, "ascii"))
    render_all(sprites, a.out, a.scale)
    print("rendered %d sprites -> %s" % (len(sprites), a.out))
    return 0


if __name__ == "__main__":
    sys.exit(main())

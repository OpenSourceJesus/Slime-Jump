#!/usr/bin/env python3
"""gen_unity_scene.py - write a real Unity project with a .unity scene, for Crust's Unity_Pack.

gen_slime.py emits C# that builds its scene at run time, so there is no scene *file* for
unity_pack.py to read. This tool writes the other kind of project: an authored scene
(Assets/Scenes/Menu.unity, plain Unity YAML) holding a camera, a uGUI Canvas with a
"Start Game" button, and the slime sprite (hidden until the button is pressed).

    python3 tools/gen_unity_scene.py [--out /tmp/SlimeJumpMenu] [--art /tmp/slimejump_art]
                                     [--onclick script|setactive] [--text "Start Game"]
                                     [--size 960x540] [--force]
    python3 /path/to/crust/tools/unity_pack.py /tmp/SlimeJumpMenu -o /tmp/SlimeJumpMenu_pack

What it writes
    Assets/Scenes/Menu.unity        Main Camera, Canvas > Menu > StartButton > Label, Slime, GameManager
    Assets/Scripts/GameManager.cs   StartGame(): hides the menu, shows the slime  (+ .meta)
    Assets/Sprites/slime.png        from gen_sprites.py (+ .meta, pixels-per-unit from the art)
    Assets/Fonts/SlimeUI SDF.asset  TextMeshPro font asset for the button label (+ .meta)
    ProjectSettings/ProjectSettings.asset   productName and default window size

--onclick script    the button's persistent onClick calls GameManager.StartGame()   (default)
--onclick setactive the onClick is two direct GameObject.SetActive calls, no script involved
                    (the smallest thing Unity_Pack can run; useful to tell scene problems from
                    script-lowering problems)

Only the standard library is needed to write the project; PIL is needed only when the art has
to be rendered (gen_sprites.py does that, once, into --art).
"""
import argparse
import hashlib
import json
import os
import shutil
import subprocess
import sys

HERE = os.path.dirname(os.path.abspath(__file__))

# Script GUIDs of Unity's built-in uGUI components (UnityEngine.UI.dll / TextMeshPro): the
# scene refers to them by these, and Unity_Pack recognises them by the same values.
IMAGE_GUID = "fe87c0e1cc204ed48ad3b37840f39efc"
BUTTON_GUID = "4e29b1a8efbd4b44bb3f3716e73f07ff"
TMP_GUID = "f4688fdb7df04437aeb418b961361dc5"
CANVAS_SCALER_GUID = "0cd44c1031e13a943bb63640046fad76"
# Unity's built-in "UISprite" (the rounded button background).
UISPRITE = "{fileID: 10905, guid: 0000000000000000f000000000000000, type: 0}"

GAME_MANAGER_CS = """using UnityEngine;

// Scene wiring: the Start Game button's onClick calls StartGame().
public class GameManager : MonoBehaviour
{
    public GameObject menu;
    public GameObject slime;

    public void StartGame()
    {
        menu.SetActive(false);
        slime.SetActive(true);
        Debug.Log("game started");
    }
}
"""


def guid_of(name):
    """A stable 32-hex GUID per asset name, so regenerating a project does not change its metas."""
    return hashlib.md5(("slimejump:" + name).encode()).hexdigest()


class Scene:
    """Collects Unity YAML documents and hands out fileIDs."""

    def __init__(self):
        self.docs = []
        self._next = 100

    def fid(self):
        self._next += 1
        return self._next

    def add(self, class_id, file_id, body):
        self.docs.append("--- !u!%d &%d\n%s" % (class_id, file_id, body))

    def text(self):
        return "%YAML 1.1\n%TAG !u! tag:unity3d.com,2011:\n" + "".join(self.docs)


def vec(x, y, z=None):
    return "{x: %s, y: %s%s}" % (_n(x), _n(y), "" if z is None else ", z: %s" % _n(z))


def _n(v):
    return ("%g" % v)


def build_scene(onclick, button_text, sprite_guid, font_guid, size):
    s = Scene()
    # ids are reserved up front so objects can point at each other
    go_camera, xf_camera, cam = s.fid(), s.fid(), s.fid()
    go_slime, xf_slime, sr_slime = s.fid(), s.fid(), s.fid()
    go_mgr, xf_mgr, mb_mgr = s.fid(), s.fid(), s.fid()
    go_canvas, rt_canvas, cv, scaler = s.fid(), s.fid(), s.fid(), s.fid()
    go_menu, rt_menu = s.fid(), s.fid()
    go_btn, rt_btn, img_btn, btn = s.fid(), s.fid(), s.fid(), s.fid()
    go_lbl, rt_lbl, tmp_lbl = s.fid(), s.fid(), s.fid()

    # ---- Main Camera ---------------------------------------------------------------------
    s.add(1, go_camera, "GameObject:\n  m_ObjectHideFlags: 0\n  serializedVersion: 6\n"
          "  m_Component:\n  - component: {fileID: %d}\n  - component: {fileID: %d}\n"
          "  m_Layer: 0\n  m_Name: Main Camera\n  m_TagString: MainCamera\n  m_IsActive: 1\n"
          % (xf_camera, cam))
    s.add(4, xf_camera, "Transform:\n  m_GameObject: {fileID: %d}\n  m_LocalRotation: {x: 0, y: 0, z: 0, w: 1}\n"
          "  m_LocalPosition: %s\n  m_LocalScale: {x: 1, y: 1, z: 1}\n  m_Children: []\n"
          "  m_Father: {fileID: 0}\n" % (go_camera, vec(0, 0, -10)))
    s.add(20, cam, "Camera:\n  m_GameObject: {fileID: %d}\n  m_Enabled: 1\n"
          "  clear flags: 2\n  m_BackGroundColor: {r: 0.0784, g: 0.0627, b: 0.1098, a: 1}\n"
          "  orthographic: 1\n  orthographic size: 5\n  near clip plane: 0.3\n  far clip plane: 1000\n"
          % go_camera)

    # ---- Slime: hidden until StartGame() --------------------------------------------------
    s.add(1, go_slime, "GameObject:\n  m_ObjectHideFlags: 0\n  serializedVersion: 6\n"
          "  m_Component:\n  - component: {fileID: %d}\n  - component: {fileID: %d}\n"
          "  m_Layer: 0\n  m_Name: Slime\n  m_TagString: Untagged\n  m_IsActive: 0\n"
          % (xf_slime, sr_slime))
    s.add(4, xf_slime, "Transform:\n  m_GameObject: {fileID: %d}\n  m_LocalRotation: {x: 0, y: 0, z: 0, w: 1}\n"
          "  m_LocalPosition: %s\n  m_LocalScale: %s\n  m_Children: []\n  m_Father: {fileID: 0}\n"
          % (go_slime, vec(0, 0, 0), vec(4, 4, 1)))
    s.add(212, sr_slime, "SpriteRenderer:\n  m_GameObject: {fileID: %d}\n  m_Enabled: 1\n"
          "  m_Sprite: {fileID: 21300000, guid: %s, type: 3}\n  m_Color: {r: 1, g: 1, b: 1, a: 1}\n"
          % (go_slime, sprite_guid))

    # ---- GameManager ------------------------------------------------------------------------
    s.add(1, go_mgr, "GameObject:\n  m_ObjectHideFlags: 0\n  serializedVersion: 6\n"
          "  m_Component:\n  - component: {fileID: %d}\n%s  m_Layer: 0\n  m_Name: GameManager\n"
          "  m_TagString: Untagged\n  m_IsActive: 1\n"
          % (xf_mgr, "  - component: {fileID: %d}\n" % mb_mgr if onclick == "script" else ""))
    s.add(4, xf_mgr, "Transform:\n  m_GameObject: {fileID: %d}\n  m_LocalRotation: {x: 0, y: 0, z: 0, w: 1}\n"
          "  m_LocalPosition: %s\n  m_LocalScale: {x: 1, y: 1, z: 1}\n  m_Children: []\n"
          "  m_Father: {fileID: 0}\n" % (go_mgr, vec(0, 0, 0)))
    if onclick == "script":
        s.add(114, mb_mgr, "MonoBehaviour:\n  m_GameObject: {fileID: %d}\n  m_Enabled: 1\n"
              "  m_Script: {fileID: 11500000, guid: %s, type: 3}\n"
              "  menu: {fileID: %d}\n  slime: {fileID: %d}\n"
              % (go_mgr, guid_of("GameManager.cs"), go_menu, go_slime))

    # ---- Canvas > Menu > StartButton > Label -----------------------------------------------
    w, h = size
    s.add(1, go_canvas, "GameObject:\n  m_ObjectHideFlags: 0\n  serializedVersion: 6\n"
          "  m_Component:\n  - component: {fileID: %d}\n  - component: {fileID: %d}\n"
          "  - component: {fileID: %d}\n  m_Layer: 5\n  m_Name: Canvas\n  m_TagString: Untagged\n"
          "  m_IsActive: 1\n" % (rt_canvas, cv, scaler))
    s.add(224, rt_canvas, "RectTransform:\n  m_GameObject: {fileID: %d}\n  m_LocalRotation: {x: 0, y: 0, z: 0, w: 1}\n"
          "  m_LocalPosition: %s\n  m_LocalScale: {x: 1, y: 1, z: 1}\n  m_Children:\n  - {fileID: %d}\n"
          "  m_Father: {fileID: 0}\n  m_AnchorMin: {x: 0, y: 0}\n  m_AnchorMax: {x: 0, y: 0}\n"
          "  m_AnchoredPosition: {x: 0, y: 0}\n  m_SizeDelta: %s\n  m_Pivot: {x: 0, y: 0}\n"
          % (go_canvas, vec(0, 0, 0), rt_menu, vec(w, h)))
    s.add(223, cv, "Canvas:\n  m_GameObject: {fileID: %d}\n  m_Enabled: 1\n  serializedVersion: 3\n"
          "  m_RenderMode: 0\n  m_SortingLayerID: 0\n  m_SortingOrder: 0\n" % go_canvas)
    s.add(114, scaler, "MonoBehaviour:\n  m_GameObject: {fileID: %d}\n  m_Enabled: 1\n"
          "  m_Script: {fileID: 11500000, guid: %s, type: 3}\n  m_UiScaleMode: 1\n"
          "  m_ReferencePixelsPerUnit: 100\n  m_ScaleFactor: 1\n  m_ReferenceResolution: %s\n"
          "  m_ScreenMatchMode: 0\n  m_MatchWidthOrHeight: 0.5\n"
          % (go_canvas, CANVAS_SCALER_GUID, vec(w, h)))

    # Menu: full-screen container, so one SetActive(false) hides the whole menu.
    s.add(1, go_menu, "GameObject:\n  m_ObjectHideFlags: 0\n  serializedVersion: 6\n"
          "  m_Component:\n  - component: {fileID: %d}\n  m_Layer: 5\n  m_Name: Menu\n"
          "  m_TagString: Untagged\n  m_IsActive: 1\n" % rt_menu)
    s.add(224, rt_menu, "RectTransform:\n  m_GameObject: {fileID: %d}\n  m_LocalRotation: {x: 0, y: 0, z: 0, w: 1}\n"
          "  m_LocalPosition: %s\n  m_LocalScale: {x: 1, y: 1, z: 1}\n  m_Children:\n  - {fileID: %d}\n"
          "  m_Father: {fileID: %d}\n  m_AnchorMin: {x: 0, y: 0}\n  m_AnchorMax: {x: 1, y: 1}\n"
          "  m_AnchoredPosition: {x: 0, y: 0}\n  m_SizeDelta: {x: 0, y: 0}\n  m_Pivot: {x: 0.5, y: 0.5}\n"
          % (go_menu, vec(0, 0, 0), rt_btn, rt_canvas))

    # StartButton: Image (the rounded UISprite, 9-sliced) + Button
    s.add(1, go_btn, "GameObject:\n  m_ObjectHideFlags: 0\n  serializedVersion: 6\n"
          "  m_Component:\n  - component: {fileID: %d}\n  - component: {fileID: %d}\n"
          "  - component: {fileID: %d}\n  m_Layer: 5\n  m_Name: StartButton\n  m_TagString: Untagged\n"
          "  m_IsActive: 1\n" % (rt_btn, img_btn, btn))
    s.add(224, rt_btn, "RectTransform:\n  m_GameObject: {fileID: %d}\n  m_LocalRotation: {x: 0, y: 0, z: 0, w: 1}\n"
          "  m_LocalPosition: %s\n  m_LocalScale: {x: 1, y: 1, z: 1}\n  m_Children:\n  - {fileID: %d}\n"
          "  m_Father: {fileID: %d}\n  m_AnchorMin: {x: 0.5, y: 0.5}\n  m_AnchorMax: {x: 0.5, y: 0.5}\n"
          "  m_AnchoredPosition: {x: 0, y: 0}\n  m_SizeDelta: {x: 260, y: 64}\n  m_Pivot: {x: 0.5, y: 0.5}\n"
          % (go_btn, vec(0, 0, 0), rt_lbl, rt_menu))
    s.add(114, img_btn, "MonoBehaviour:\n  m_GameObject: {fileID: %d}\n  m_Enabled: 1\n"
          "  m_Script: {fileID: 11500000, guid: %s, type: 3}\n  m_Material: {fileID: 0}\n"
          "  m_Color: {r: 1, g: 1, b: 1, a: 1}\n  m_RaycastTarget: 1\n  m_Sprite: %s\n  m_Type: 1\n"
          "  m_PreserveAspect: 0\n  m_FillCenter: 1\n" % (go_btn, IMAGE_GUID, UISPRITE))

    if onclick == "script":
        calls = _call(mb_mgr, "GameManager, Assembly-CSharp", "StartGame", mode=1, flag=0)
    else:
        calls = (_call(go_menu, "UnityEngine.GameObject, UnityEngine", "SetActive", mode=6, flag=0) +
                 _call(go_slime, "UnityEngine.GameObject, UnityEngine", "SetActive", mode=6, flag=1))
    s.add(114, btn, "MonoBehaviour:\n  m_GameObject: {fileID: %d}\n  m_Enabled: 1\n"
          "  m_Script: {fileID: 11500000, guid: %s, type: 3}\n  m_Navigation:\n    m_Mode: 3\n"
          "  m_Transition: 1\n  m_Colors:\n    m_NormalColor: {r: 1, g: 1, b: 1, a: 1}\n"
          "    m_HighlightedColor: {r: 0.9607843, g: 0.9607843, b: 0.9607843, a: 1}\n"
          "    m_PressedColor: {r: 0.78431374, g: 0.78431374, b: 0.78431374, a: 1}\n"
          "    m_SelectedColor: {r: 0.9607843, g: 0.9607843, b: 0.9607843, a: 1}\n"
          "    m_DisabledColor: {r: 0.78431374, g: 0.78431374, b: 0.78431374, a: 0.5019608}\n"
          "    m_ColorMultiplier: 1\n    m_FadeDuration: 0.1\n  m_Interactable: 1\n"
          "  m_TargetGraphic: {fileID: %d}\n  m_OnClick:\n    m_PersistentCalls:\n      m_Calls:\n%s"
          % (go_btn, BUTTON_GUID, img_btn, calls))

    # Label: TextMeshPro text filling the button
    s.add(1, go_lbl, "GameObject:\n  m_ObjectHideFlags: 0\n  serializedVersion: 6\n"
          "  m_Component:\n  - component: {fileID: %d}\n  - component: {fileID: %d}\n"
          "  m_Layer: 5\n  m_Name: Label\n  m_TagString: Untagged\n  m_IsActive: 1\n" % (rt_lbl, tmp_lbl))
    s.add(224, rt_lbl, "RectTransform:\n  m_GameObject: {fileID: %d}\n  m_LocalRotation: {x: 0, y: 0, z: 0, w: 1}\n"
          "  m_LocalPosition: %s\n  m_LocalScale: {x: 1, y: 1, z: 1}\n  m_Children: []\n"
          "  m_Father: {fileID: %d}\n  m_AnchorMin: {x: 0, y: 0}\n  m_AnchorMax: {x: 1, y: 1}\n"
          "  m_AnchoredPosition: {x: 0, y: 0}\n  m_SizeDelta: {x: 0, y: 0}\n  m_Pivot: {x: 0.5, y: 0.5}\n"
          % (go_lbl, vec(0, 0, 0), rt_btn))
    s.add(114, tmp_lbl, "MonoBehaviour:\n  m_GameObject: {fileID: %d}\n  m_Enabled: 1\n"
          "  m_Script: {fileID: 11500000, guid: %s, type: 3}\n  m_Material: {fileID: 0}\n"
          "  m_Color: {r: 1, g: 1, b: 1, a: 1}\n  m_RaycastTarget: 0\n  m_text: %s\n"
          "  m_fontAsset: {fileID: 11400000, guid: %s, type: 2}\n"
          "  m_fontSize: 32\n  m_fontColor: {r: 0.1, g: 0.1, b: 0.1, a: 1}\n"
          "  m_HorizontalAlignment: 2\n  m_VerticalAlignment: 512\n  m_overflowMode: 0\n"
          % (go_lbl, TMP_GUID, button_text, font_guid))
    return s.text()


def make_font_asset(text_chars, point_size=48, atlas=512):
    """A TextMeshPro Font Asset (YAML) drawn with Pillow's built-in scalable font.

    Unity_Pack reads the atlas (Alpha8, hex in _typelessdata), the glyph table and the character
    table, and bakes label text from them. As in Unity, Texture2D rows are stored bottom first and
    a GlyphRect's y counts up from the bottom of the atlas. Metrics are in point-size units
    (y up from the baseline), as TMP stores them.
    """
    from PIL import Image, ImageDraw, ImageFont
    font = ImageFont.load_default(point_size)
    ascent, descent = font.getmetrics()
    pad = 2
    img = Image.new("L", (atlas, atlas), 0)
    draw = ImageDraw.Draw(img)
    glyphs, chars = [], []
    x = y = pad
    row_h = 0
    for index, ch in enumerate(sorted(set(text_chars) | {" "})):
        x0, y0, x1, y1 = font.getbbox(ch, anchor="ls")           # relative to the baseline origin
        w, h = max(0, x1 - x0), max(0, y1 - y0)
        if x + w + pad > atlas:
            x, y, row_h = pad, y + row_h + pad, 0
        if y + h + pad > atlas:
            raise ValueError("font atlas %d is too small for %d glyphs" % (atlas, len(text_chars)))
        if w and h:
            draw.text((x - x0, y - y0), ch, fill=255, font=font, anchor="ls")
        glyphs.append((index, w, h, x0, -y0, font.getlength(ch), x, y))
        chars.append((ord(ch), index))
        x += w + pad
        row_h = max(row_h, h)
    hexdata = img.transpose(Image.FLIP_TOP_BOTTOM).tobytes().hex()      # bottom row first, as Unity stores it
    out = ["%YAML 1.1", "%TAG !u! tag:unity3d.com,2011:", "--- !u!114 &11400000", "MonoBehaviour:",
           "  m_ObjectHideFlags: 0", "  m_Name: SlimeUI SDF", "  m_FaceInfo:",
           "    m_FamilyName: SlimeUI", "    m_PointSize: %d" % point_size,
           "    m_AscentLine: %d" % ascent, "    m_DescentLine: -%d" % descent,
           "    m_LineHeight: %d" % (ascent + descent), "  m_AtlasWidth: %d" % atlas,
           "  m_AtlasHeight: %d" % atlas, "  m_GlyphTable:"]
    for index, w, h, bx, by, adv, gx, gy in glyphs:
        out += ["  - m_Index: %d" % index, "    m_Metrics:", "      m_Width: %d" % w, "      m_Height: %d" % h,
                "      m_HorizontalBearingX: %d" % bx, "      m_HorizontalBearingY: %d" % by,
                "      m_HorizontalAdvance: %g" % adv, "    m_GlyphRect:", "      m_X: %d" % gx,
                "      m_Y: %d" % (atlas - gy - h), "      m_Width: %d" % w, "      m_Height: %d" % h]
    out.append("  m_CharacterTable:")
    for code, index in chars:
        out += ["  - m_ElementType: 1", "    m_Unicode: %d" % code, "    m_GlyphIndex: %d" % index,
                "    m_Scale: 1"]
    out += ["  m_AtlasTextures:", "  - m_Width: %d" % atlas, "    m_Height: %d" % atlas,
            "    m_TextureFormat: 1", "    image data:", "      _typelessdata: " + hexdata]
    return "\n".join(out) + "\n"


def _call(target, assembly_type, method, mode, flag):
    """One persistent UnityEvent call. Mode 1 = void, 6 = bool (m_BoolArgument)."""
    return ("      - m_Target: {fileID: %d}\n        m_TargetAssemblyTypeName: %s\n"
            "        m_MethodName: %s\n        m_Mode: %d\n        m_Arguments:\n"
            "          m_ObjectArgument: {fileID: 0}\n"
            "          m_ObjectArgumentAssemblyTypeName: UnityEngine.Object, UnityEngine\n"
            "          m_IntArgument: 0\n          m_FloatArgument: 0\n          m_StringArgument: \n"
            "          m_BoolArgument: %d\n        m_CallState: 2\n" % (target, assembly_type, method, mode, flag))


def write(path, text):
    os.makedirs(os.path.dirname(path), exist_ok=True)
    with open(path, "w") as f:
        f.write(text)


def ensure_art(art):
    """sprites.json + png/slime.png from gen_sprites.py, rendered once."""
    if not os.path.exists(os.path.join(art, "png", "slime.png")):
        subprocess.run([sys.executable, os.path.join(HERE, "gen_sprites.py"), "--out", art], check=True)
    with open(os.path.join(art, "sprites.json")) as f:
        sprites = json.load(f)["sprites"]
    slime = next(x for x in sprites if x["name"] == "slime")
    return os.path.join(art, "png", "slime.png"), int(slime.get("ppu", 14))


def main(argv=None):
    ap = argparse.ArgumentParser(description=__doc__.split("\n")[0])
    ap.add_argument("--out", default="/tmp/SlimeJumpMenu")
    ap.add_argument("--art", default="/tmp/slimejump_art")
    ap.add_argument("--onclick", choices=["script", "setactive"], default="script")
    ap.add_argument("--text", default="Start Game", help="button label")
    ap.add_argument("--size", default="960x540", help="default window size, WxH")
    ap.add_argument("--force", action="store_true", help="replace an existing --out")
    a = ap.parse_args(argv)
    if os.path.exists(a.out):
        if not a.force:
            sys.exit("%s exists (use --force)" % a.out)
        shutil.rmtree(a.out)
    w, h = (int(v) for v in a.size.lower().split("x"))

    png, ppu = ensure_art(a.art)
    sprite_guid, font_guid = guid_of("slime.png"), guid_of("SlimeUI SDF.asset")
    write(os.path.join(a.out, "Assets/Scenes/Menu.unity"),
          build_scene(a.onclick, a.text, sprite_guid, font_guid, (w, h)))
    write(os.path.join(a.out, "Assets/Fonts/SlimeUI SDF.asset"), make_font_asset(a.text))
    write(os.path.join(a.out, "Assets/Fonts/SlimeUI SDF.asset.meta"),
          "fileFormatVersion: 2\nguid: %s\nNativeFormatImporter:\n  mainObjectFileID: 11400000\n" % font_guid)
    os.makedirs(os.path.join(a.out, "Assets/Sprites"), exist_ok=True)
    shutil.copyfile(png, os.path.join(a.out, "Assets/Sprites/slime.png"))
    write(os.path.join(a.out, "Assets/Sprites/slime.png.meta"),
          "fileFormatVersion: 2\nguid: %s\nTextureImporter:\n  spriteImportMode: 1\n  textureType: 8\n"
          "  spritePixelsToUnits: %d\n  filterMode: 0\n" % (sprite_guid, ppu))
    if a.onclick == "script":
        write(os.path.join(a.out, "Assets/Scripts/GameManager.cs"), GAME_MANAGER_CS)
        write(os.path.join(a.out, "Assets/Scripts/GameManager.cs.meta"),
              "fileFormatVersion: 2\nguid: %s\nMonoImporter:\n  serializedVersion: 2\n" % guid_of("GameManager.cs"))
    write(os.path.join(a.out, "ProjectSettings/ProjectSettings.asset"),
          "%%YAML 1.1\n%%TAG !u! tag:unity3d.com,2011:\n--- !u!129 &1\nPlayerSettings:\n"
          "  companyName: SlimeJump\n  productName: SlimeJumpMenu\n"
          "  defaultScreenWidth: %d\n  defaultScreenHeight: %d\n" % (w, h))
    print("wrote %s (onclick=%s)" % (a.out, a.onclick))
    return 0


if __name__ == "__main__":
    sys.exit(main())

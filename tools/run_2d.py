#!/usr/bin/env python3
"""run_2d.py - build and run Slime Jump on Prowl2D and Stride2D, and compare.

For each engine: generate the subset-C# game from a level (gen_slime.py --target), translate it
to C, build the native player, run it, run the same source on .NET, and require identical output
(that is the engines' own `--verify`). Then require both engines to print the same thing, and the
bot to have reached the goal.

    python3 tools/run_2d.py --level /tmp/slimejump_art/levels/tutorial.json
    python3 tools/run_2d.py --level .../cave.json --max-frames 12000 --engines prowl2d

Engine repos are looked up as ../Prowl2D and ../stride2D next to this repository, or under
--engines-root / $SLIME_ENGINES_ROOT. Each needs `python3 build.py deps && python3 build.py native`
once (and `python3 build.py ccsharp` for Stride2D; see their READMEs). Needs the .NET 10 SDK.
"""
import argparse
import json
import os
import subprocess
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
import gen_slime as gs  # noqa: E402

REPOS = {"prowl2d": "Prowl2D", "stride2d": "stride2D"}


def ensure_renderer(repo):
    """Prowl2D's renderer library is not built by the engine's own build.py (see build_gfx2d.py)."""
    lib = os.path.join(repo, "Libraries", "linux-x64", "native", "libgfx2d.so")
    if not os.path.exists(lib):
        import build_gfx2d
        build_gfx2d.main(["--prowl2d", repo])


def make_gif(frames_dir, path, width=400, fps=12):
    from PIL import Image
    import glob
    files = sorted(glob.glob(os.path.join(frames_dir, "frame_*.ppm")))
    if not files:
        return 0
    frames = []
    for f in files:
        im = Image.open(f).convert("RGB")
        frames.append(im.resize((width, im.height * width // im.width), Image.LANCZOS))
    frames[0].save(path, save_all=True, append_images=frames[1:], duration=int(1000 / fps), loop=0, optimize=True)
    return len(frames)


def run_engine(engine, repo, level, work, max_frames, timeout, render=False, render_every=8):
    out = os.path.join(work, engine + ("_render" if render else ""))
    gs.write_subset_project(out, level, engine, max_frames, force=True, render=render, render_every=render_every)
    if render:
        ensure_renderer(repo)
    p = subprocess.run([sys.executable, "build.py", "player", out, "--verify", "--run"], cwd=repo,
                       capture_output=True, text=True, timeout=timeout)
    text = p.stdout + p.stderr
    # the .NET reference run writes its frames into the engine repo's working directory: tidy them away
    import glob
    for stray in glob.glob(os.path.join(repo, "frame_*.ppm")):
        os.remove(stray)
    game_out = [l for l in p.stdout.splitlines()
                if l.startswith(("t=", "won=", "first jump", "max x", "frame ", "renderer="))]
    verified = "verify    ok" in text
    return {"engine": engine, "rc": p.returncode, "verified": verified, "output": game_out, "log": text,
            "frames_dir": os.path.join(repo, "Build", "Player", os.path.basename(out))}


def main(argv=None):
    ap = argparse.ArgumentParser(description=__doc__.split("\n")[0])
    ap.add_argument("--level", required=True, help="level .json (from gen_levels.py)")
    ap.add_argument("--engines", default="prowl2d,stride2d")
    ap.add_argument("--engines-root", default=os.environ.get("SLIME_ENGINES_ROOT"))
    ap.add_argument("--work", default="/tmp/SlimeJump2D")
    ap.add_argument("--max-frames", type=int, default=3000)
    ap.add_argument("--render", action="store_true", help="Prowl2D only: draw frames (Native/Gfx2D), saved as PPMs")
    ap.add_argument("--render-every", type=int, default=8, help="save a frame every N game frames")
    ap.add_argument("--gif", help="with --render: also write an animated GIF of the frames here")
    ap.add_argument("--timeout", type=int, default=600)
    ap.add_argument("-v", "--verbose", action="store_true", help="print the full build log on failure")
    a = ap.parse_args(argv)
    root = a.engines_root or os.path.dirname(os.path.dirname(HERE))
    with open(a.level) as f:
        level = json.load(f)
    results = []
    engines = a.engines.split(",")
    if a.render:
        engines = [e for e in engines if e == "prowl2d"]      # Stride2D has no renderer yet
        if not engines:
            sys.exit("--render needs the prowl2d engine")
    for engine in engines:
        repo = os.path.join(root, REPOS[engine])
        if not os.path.isdir(repo):
            sys.exit("%s: no engine repo at %s (use --engines-root)" % (engine, repo))
        r = run_engine(engine, repo, level, os.path.join(a.work, os.path.basename(a.level)[:-5]), a.max_frames, a.timeout,
                       a.render, a.render_every)
        results.append(r)
        if a.render and a.gif and r["verified"]:
            print("gif       %s (%d frames)" % (a.gif, make_gif(r["frames_dir"], a.gif)))
        won = any(l.startswith("won=1") for l in r["output"])
        summary = next((l for l in r["output"] if l.startswith("won=")), "(no result line)")
        if a.render:
            summary += "  [%d frames, every frame hash identical native==.NET]" % sum(l.startswith("frame ") for l in r["output"])
        print("%-9s native==.NET: %-5s  %s" % (engine, "yes" if r["verified"] else "NO", summary))
        if not r["verified"] and a.verbose:
            print(r["log"])
    ok = all(r["verified"] for r in results) and all(any(l.startswith("won=1") for l in r["output"]) for r in results)
    if len(results) > 1:
        same = all(r["output"] == results[0]["output"] for r in results)
        print("engines print identical output: %s" % ("yes" if same else "NO"))
        ok = ok and same
    print("RESULT:", "PASS" if ok else "FAIL")
    return 0 if ok else 1


if __name__ == "__main__":
    sys.exit(main())

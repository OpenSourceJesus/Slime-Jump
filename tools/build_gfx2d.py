#!/usr/bin/env python3
"""build_gfx2d.py - build Prowl2D's headless renderer library (Native/Gfx2D/gfx2d.c).

The Prowl2D repo ships gfx2d.c and its README says `python3 build.py gfx`, but this repo version has
no such build step, so a game that draws (names `GFX.`) cannot link. This builds what it needs, into
the untracked Libraries/<rid>/native directory of the engine checkout (nothing tracked is touched):

    libgfx2d.so        for the .NET reference run  ([LibraryImport("gfx2d")])
    libgfx2d_static.a  for the translated C player (player_build.py packages it)

Needs: gcc, the EGL/GLES development headers (apt install libegl-dev libgles-dev), Mesa's software
GL at run time (apt install libegl-mesa0 libgl1-mesa-dri), and a crust checkout (for gles3_batch.h).

    python3 tools/build_gfx2d.py [--prowl2d ../Prowl2D] [--crust ../crust]
"""
import argparse
import glob
import os
import subprocess
import sys

HERE = os.path.dirname(os.path.abspath(__file__))


def main(argv=None):
    root = os.path.dirname(os.path.dirname(HERE))
    ap = argparse.ArgumentParser(description=__doc__.split("\n")[0])
    ap.add_argument("--prowl2d", default=os.path.join(root, "Prowl2D"))
    ap.add_argument("--crust", default=os.path.join(root, "crust"))
    a = ap.parse_args(argv)
    src = os.path.join(a.prowl2d, "Native", "Gfx2D")
    unity_pack = os.path.join(a.crust, "examples", "unity_pack")
    for p in (os.path.join(src, "gfx2d.c"), os.path.join(unity_pack, "gles3_batch.h")):
        if not os.path.exists(p):
            sys.exit("build_gfx2d: missing %s" % p)
    # the runtime dir the engine itself uses: the one holding the built libprowl_box2d
    rid = [d for d in sorted(glob.glob(os.path.join(a.prowl2d, "Libraries", "*", "native")))
           if any(os.path.exists(os.path.join(d, n)) for n in ("libprowl_box2d.so", "libprowl_box2d.dylib"))]
    if not rid:
        sys.exit("build_gfx2d: no Libraries/<rid>/native with libprowl_box2d in %s (run `python3 build.py native` there first)" % a.prowl2d)
    out = rid[0]
    cflags = ["-O2", "-Wno-unused-function", "-I" + src, "-I" + unity_pack]
    obj = os.path.join(out, "gfx2d_pic.o")
    run = lambda cmd: subprocess.run(cmd, check=True)
    run(["gcc", "-fPIC", "-c"] + cflags + [os.path.join(src, "gfx2d.c"), "-o", obj])
    run(["gcc", "-shared", obj, "-o", os.path.join(out, "libgfx2d.so"), "-lEGL", "-lGLESv2", "-lm"])
    run(["ar", "rcs", os.path.join(out, "libgfx2d_static.a"), obj])
    os.remove(obj)
    print("built libgfx2d.so and libgfx2d_static.a in", out)
    return 0


if __name__ == "__main__":
    sys.exit(main())

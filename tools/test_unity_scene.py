#!/usr/bin/env python3
"""test_unity_scene.py - generate the menu scene, pack it with Crust's Unity_Pack, click the button.

For each --onclick variant of gen_unity_scene.py:
  1. write the Unity project            (tools/gen_unity_scene.py)
  2. pack it with unity_pack.py         (crust checkout; engine.c / data.c -> native player objects)
  3. link a small driver that ticks the packed engine, presses the pointer on the Start Game
     button's centre, releases it, and counts what is drawn before and after
  4. require: before the click the menu is drawn (button + label, slime hidden),
              after it the menu is gone and the slime is drawn.

    python3 tools/test_unity_scene.py [--crust ../crust] [--work /tmp/SJ_test]

The crust checkout is --crust, else $CRUST_ROOT, else ../crust next to this repository.
Needs gcc and Pillow. Exit status 0 = PASS.
"""
import argparse
import os
import re
import subprocess
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
SIZE = (960, 540)           # gen_unity_scene.py's default window; the button sits at its centre

DRIVER_C = r'''
#include <stdio.h>
#include "engine_draw.h"
extern float Time_deltaTime;
extern float engine_pointer_x, engine_pointer_y;
extern int engine_pointer_down;

static int draws(void) { EngineDraw buf[256]; return engine_collect_draws(buf, 256); }
static void ticks(int n) { while (n-- > 0) engine_tick(); }

int main(int argc, char **argv) {
    engine_apply_argv(argc, argv);
    Time_deltaTime = 0.0166667f;
    ticks(5);
    int before = draws();
    engine_pointer_x = %(cx)d.0f;  engine_pointer_y = %(cy)d.0f;     /* screen px, origin bottom-left */
    engine_pointer_down = 1;  ticks(2);
    engine_pointer_down = 0;  ticks(2);
    int after = draws();
    printf("before=%%d after=%%d\n", before, after);
    return 0;
}
'''


def run(cmd, **kw):
    p = subprocess.run(cmd, capture_output=True, text=True, **kw)
    return p.returncode, p.stdout + p.stderr


def check_variant(onclick, crust, work):
    proj = os.path.join(work, "proj_" + onclick)
    pack = os.path.join(work, "pack_" + onclick)
    rc, out = run([sys.executable, os.path.join(HERE, "gen_unity_scene.py"), "--out", proj, "--onclick", onclick,
                   "--force"])
    if rc:
        return False, "generate failed:\n" + out
    rc, out = run([sys.executable, os.path.join(crust, "tools", "unity_pack.py"), proj, "-o", pack], cwd=crust)
    if rc:
        return False, "unity_pack failed:\n" + out[-3000:]
    driver = os.path.join(pack, "click_driver.c")
    with open(driver, "w") as f:
        f.write(DRIVER_C % {"cx": SIZE[0] // 2, "cy": SIZE[1] // 2})
    exe = os.path.join(pack, "click_driver")
    rc, out = run(["gcc", "-O1", "-I", pack, driver, os.path.join(pack, "engine.o"), os.path.join(pack, "data.o"),
                   "-lm", "-o", exe])
    if rc:
        return False, "driver build failed:\n" + out[-3000:]
    rc, out = run([exe])
    m = re.search(r"before=(\d+) after=(\d+)", out)
    if rc or not m:
        return False, "driver run failed (rc=%d):\n%s" % (rc, out)
    before, after = int(m.group(1)), int(m.group(2))
    ok = before == 2 and after == 1
    return ok, "draws before click: %d (button + label), after: %d (slime)" % (before, after)


def main(argv=None):
    ap = argparse.ArgumentParser(description=__doc__.split("\n")[0])
    ap.add_argument("--crust", default=os.environ.get("CRUST_ROOT") or
                    os.path.join(os.path.dirname(os.path.dirname(HERE)), "crust"))
    ap.add_argument("--work", default="/tmp/SJ_test")
    a = ap.parse_args(argv)
    if not os.path.exists(os.path.join(a.crust, "tools", "unity_pack.py")):
        sys.exit("no crust checkout at %s (use --crust or $CRUST_ROOT)" % a.crust)
    os.makedirs(a.work, exist_ok=True)
    good = True
    for onclick in ("setactive", "script"):
        ok, note = check_variant(onclick, os.path.abspath(a.crust), os.path.abspath(a.work))
        print("%-10s %s  %s" % (onclick, "ok  " if ok else "FAIL", note))
        good = good and ok
    print("RESULT:", "PASS" if good else "FAIL")
    return 0 if good else 1


if __name__ == "__main__":
    sys.exit(main())

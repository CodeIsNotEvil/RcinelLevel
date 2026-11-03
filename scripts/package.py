#!/usr/bin/env python3
"""Build RcinelLevel and write the Thunderstore package.

  scripts/package.py            # dist/CodeIsNotEvil-RcinelLevel-<version>.zip

Run from anywhere; needs the .NET SDK and the game (Environment.props), because the plugin compiles
against Valheim's assemblies, which can't be in the repository or in CI.

One version, in Directory.Build.props: the script stops if RcinelLevelPlugin.Version or the newest
CHANGELOG.md section say something else, or the section isn't dated. It writes manifest.json itself
(UTF-8 without a BOM, which Thunderstore's parser needs) from that version.

Layout, as mod managers expect it: manifest.json, icon.png, README.md and CHANGELOG.md at the root,
the plugin under plugins/, which they unpack into BepInEx/plugins/CodeIsNotEvil-RcinelLevel/ with
models/ and icons/ next to the DLL, where the plugin looks for them.
"""
import json
import re
import struct
import subprocess
import sys
import xml.etree.ElementTree as ET
import zipfile
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
TEAM = "CodeIsNotEvil"
NAME = "RcinelLevel"
DESCRIPTION = (
  "Probe an area for the height where digging equals filling, then level it with straight walls: an "
  "off-hand ground probe, a level hoe that fills for stone, a pickaxe depth lock and moats for a drawbridge."
)
# BepInExPack as Jotunn asks for it; Jotunn as built against.
DEPENDENCIES = ["denikson-BepInExPack_Valheim-5.4.2333", "ValheimModding-Jotunn-2.30.2"]


def fail(message: str) -> None:
  sys.exit(f"package.py: {message}")


def version() -> str:
  props = ET.parse(ROOT / "Directory.Build.props").getroot()
  found = props.find("./PropertyGroup/Version")
  if found is None or not found.text:
    fail("no <Version> in Directory.Build.props")
  v = found.text.strip()
  plugin = (ROOT / "src" / "RcinelLevel" / "RcinelLevelPlugin.cs").read_text(encoding="utf-8")
  if f'public const string Version = "{v}";' not in plugin:
    fail(f"RcinelLevelPlugin.Version doesn't say {v}")
  changelog = (ROOT / "CHANGELOG.md").read_text(encoding="utf-8")
  newest = re.search(r"^## (\S+) - (\S+)$", changelog, re.MULTILINE)
  if not newest or newest.group(1) != v or not re.fullmatch(r"\d{4}-\d{2}-\d{2}", newest.group(2)):
    fail(f"CHANGELOG.md's newest section must be '## {v} - <yyyy-mm-dd>'")
  return v


def check_icon(path: Path) -> None:
  head = path.read_bytes()[:24]
  if head[:8] != b"\x89PNG\r\n\x1a\n" or struct.unpack(">II", head[16:24]) != (256, 256):
    fail(f"{path} must be a 256×256 PNG")


def main() -> None:
  v = version()
  if len(DESCRIPTION) > 250:
    fail("the description is longer than Thunderstore's 250 characters")
  icon = ROOT / "package" / "icon.png"
  check_icon(icon)

  # Release build, not copied into the test instance.
  subprocess.run(["dotnet", "build", str(ROOT / "RcinelLevel.slnx"), "-c", "Release", "-p:TestInstance=", "-nologo", "-v:q"], check=True)
  out = ROOT / "src" / "RcinelLevel" / "bin" / "Release" / "net472"

  manifest = {
    "name": NAME,
    "version_number": v,
    "website_url": "https://github.com/CodeIsNotEvil/RcinelLevel",
    "description": DESCRIPTION,
    "dependencies": DEPENDENCIES,
  }
  files = {
    "manifest.json": json.dumps(manifest, indent=2, ensure_ascii=False).encode("utf-8") + b"\n",
    "icon.png": icon.read_bytes(),
    "README.md": (ROOT / "package" / "README.md").read_bytes(),
    "CHANGELOG.md": (ROOT / "CHANGELOG.md").read_bytes(),
    "LICENSE": (ROOT / "LICENSE").read_bytes(),
    "plugins/RcinelLevel.dll": (out / "RcinelLevel.dll").read_bytes(),
    "plugins/RcinelLevel.pdb": (out / "RcinelLevel.pdb").read_bytes(),
  }
  for name in ("probe.obj", "level-hoe.obj", "atlas.png"):
    files[f"plugins/models/{name}"] = (ROOT / "assets" / "models" / name).read_bytes()
  for name in ("probe-128.png", "level-hoe-128.png"):
    files[f"plugins/icons/{name}"] = (ROOT / "assets" / "items" / name).read_bytes()

  dist = ROOT / "dist"
  dist.mkdir(exist_ok=True)
  target = dist / f"{TEAM}-{NAME}-{v}.zip"
  # Zip entry names use "/" by the format's definition, on every platform.
  with zipfile.ZipFile(target, "w", zipfile.ZIP_DEFLATED) as z:
    for name, data in files.items():
      z.writestr(name, data)
  print(f"{target} ({target.stat().st_size // 1024} KiB, {len(files)} files)")


if __name__ == "__main__":
  main()

#!/usr/bin/env python3
from pathlib import Path
import json
import os
import sys

ROOT = Path(__file__).resolve().parents[1]
PKG = ROOT / "Packages" / "com.kubonsang.vfx-forge"
IGNORED_DIRECTORIES = {
    ".git",
    "Artifacts",
    "Library",
    "Logs",
    "Obj",
    "Temp",
    "UserSettings",
}


def iter_project_files():
    for current, directories, filenames in os.walk(ROOT):
        directories[:] = [
            name for name in directories if name not in IGNORED_DIRECTORIES
        ]
        current_path = Path(current)
        for filename in filenames:
            yield current_path / filename


required = [
    ROOT / "AGENTS.md",
    ROOT / "feature_list.json",
    ROOT / ".agent" / "START_PROMPT.md",
    PKG / "package.json",
    PKG / "Runtime" / "VFXForge.Runtime.asmdef",
    PKG / "Editor" / "VFXForge.Editor.asmdef",
    PKG / "Schemas" / "vfx-recipe-1.0.schema.json",
    PKG / "Samples~" / "BasicRecipe" / "sample_arcane_impact.json",
]

errors = []
project_files = list(iter_project_files())
for path in required:
    if not path.exists():
        errors.append(f"missing: {path.relative_to(ROOT)}")

for path in (candidate for candidate in project_files if candidate.suffix == ".json"):
    try:
        json.loads(path.read_text(encoding="utf-8"))
    except Exception as exc:
        errors.append(f"invalid json: {path.relative_to(ROOT)}: {exc}")

package = json.loads((PKG / "package.json").read_text(encoding="utf-8"))
if package.get("name") != "com.kubonsang.vfx-forge":
    errors.append("package name mismatch")

recipe = json.loads((PKG / "Samples~" / "BasicRecipe" / "sample_arcane_impact.json").read_text(encoding="utf-8"))
if not str(recipe.get("outputPath", "")).startswith("Assets/"):
    errors.append("sample outputPath must be under Assets/")

for forbidden in ("prd", "srs"):
    for path in project_files:
        if forbidden in path.name.lower():
            errors.append(f"forbidden requested document included: {path.relative_to(ROOT)}")

if errors:
    print("STARTER VALIDATION FAILED")
    for error in errors:
        print(f"- {error}")
    sys.exit(1)

print("STARTER VALIDATION PASSED")
print(f"files={len(project_files)}")

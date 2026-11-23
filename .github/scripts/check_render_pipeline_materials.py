#!/usr/bin/env python3
"""Check that the package's materials work in both BIRP and URP projects (VR2Gather#431, #433).

VR2Gather supports both render pipelines, so its materials must be *dual*: VRT/Lit, cwipc's
shaders, or shaders that both pipelines can draw. A material on a shader that only one pipeline
can draw renders pink in the other one. The likely regressions, both from Unity's defaults for
new objects in the development project:

- a material on Standard or Standard (Specular setup), or a reference to Unity's built-in
  Default-Material (also Standard): Unity's defaults in a BIRP project; pink in URP.
- a material on one of URP's own shaders (Lit, Unlit, ...): Unity's defaults in a URP project;
  pink in BIRP.

This reads the .mat, .prefab and .unity files as text; it doesn't need Unity. Usage:
check_render_pipeline_materials.py [directory ...] (default: nl.cwi.dis.vr2gather).
Exit status 1 if anything is found that isn't in KNOWN_EXCEPTIONS.
"""
import re
import sys
from pathlib import Path

BUILTIN_GUID = "0000000000000000f000000000000000"
# Unity's built-in Standard shaders (fileID in the built-in extra resources).
BIRP_ONLY_BUILTIN_SHADERS = {"46": "Standard", "45": "Standard (Specular setup)"}
# Unity's built-in Default-Material, a Standard material.
DEFAULT_MATERIAL = f"fileID: 10303, guid: {BUILTIN_GUID}"
# URP's own material shaders (GUIDs from com.unity.render-pipelines.universal 17.3.0).
URP_ONLY_SHADERS = {
    "933532a4fcc9baf4fa0491de14d08ed7": "Universal Render Pipeline/Lit",
    "8d2bb70cbf9db8d4da26e15b26e74248": "Universal Render Pipeline/Simple Lit",
    "ee7e4c9a5f6364b688a332c67fc32cca": "Universal Render Pipeline/Complex Lit",
    "0ca6dca7396eb48e5849247ffd444914": "Universal Render Pipeline/Baked Lit",
    "650dd9526735d5b46b79224bc6e94025": "Universal Render Pipeline/Unlit",
    "b7839dad95683814aa64166edc107ae2": "Universal Render Pipeline/Particles/Lit",
    "8516d7a69675844a7a0b7095af7c46af": "Universal Render Pipeline/Particles/Simple Lit",
    "0406db5a14f94604a8c57ccfbc9f3b46": "Universal Render Pipeline/Particles/Unlit",
    "69c1f799e772cb6438f56c23efccb782": "Universal Render Pipeline/Terrain/Lit",
}
# Known and tracked elsewhere: path suffix -> issue. Remove the entry when the issue is fixed.
KNOWN_EXCEPTIONS = {
    "Runtime/VRTCommon/Prefabs/01_Player/P_Player.prefab": "#430 (webcam screen should get Webcam.mat)",
}

SHADER_REF = re.compile(r'^\s*m_Shader: \{fileID: (-?\d+), guid: ([0-9a-f]{32})', re.M)


def check_material(path, text):
    m = SHADER_REF.search(text)
    if not m:
        return []
    file_id, guid = m.groups()
    if guid == BUILTIN_GUID and file_id in BIRP_ONLY_BUILTIN_SHADERS:
        return [f"{path}: shader {BIRP_ONLY_BUILTIN_SHADERS[file_id]} is BIRP-only (pink in URP); use VRT/Lit"]
    if guid in URP_ONLY_SHADERS:
        return [f"{path}: shader {URP_ONLY_SHADERS[guid]} is URP-only (pink in BIRP); use VRT/Lit"]
    return []


def check_scene_or_prefab(path, text):
    n = text.count(DEFAULT_MATERIAL)
    if n == 0:
        return []
    return [f"{path}: {n} reference(s) to Unity's built-in Default-Material (Standard, pink in URP); use VRTDefault.mat"]


def main(args):
    roots = [Path(a) for a in args] or [Path('nl.cwi.dis.vr2gather')]
    problems, known, files = [], [], 0
    for root in roots:
        for path in sorted(root.rglob('*')):
            if path.suffix not in ('.mat', '.prefab', '.unity'):
                continue
            files += 1
            text = path.read_text(encoding='utf-8', errors='replace')
            found = check_material(path, text) if path.suffix == '.mat' else check_scene_or_prefab(path, text)
            exception = next((why for suffix, why in KNOWN_EXCEPTIONS.items() if path.as_posix().endswith(suffix)), None)
            if found and exception:
                known.extend(f"{p} [known: {exception}]" for p in found)
            else:
                problems.extend(found)
    for p in known:
        print(p)
    for p in problems:
        print(p)
    print(f'check_render_pipeline_materials: {files} file(s), {len(problems)} problem(s), {len(known)} known exception(s)')
    return 1 if problems else 0


if __name__ == '__main__':
    sys.exit(main(sys.argv[1:]))

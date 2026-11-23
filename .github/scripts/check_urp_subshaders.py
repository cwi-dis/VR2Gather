#!/usr/bin/env python3
"""Check that every URP SubShader in the package can be skipped in projects without URP.

Unity compiles every SubShader, also the URP ones when BIRP is active, so a URP SubShader
that includes URP's files breaks projects without the URP package (VR2Gather#365,
cwi-dis/cwipc_unity#55, where this script comes from).
A PackageRequirements block for com.unity.render-pipelines.universal makes Unity skip it.

A SubShader counts as URP when it has the tag "RenderPipeline" = "UniversalPipeline" or
includes a file from Packages/com.unity.render-pipelines.universal/. Every such SubShader
must have a PackageRequirements block that names com.unity.render-pipelines.universal.

This doesn't compile anything: Unity only reports the #55 error when it really compiles
shaders, which needs a graphics device. It catches the likely regression, a new URP
SubShader without the block. Usage: check_urp_subshaders.py [shader or directory ...]
(default: nl.cwi.dis.vr2gather). Exit status 1 if any SubShader fails the check.
"""
import re
import sys
from pathlib import Path

URP_PACKAGE = "com.unity.render-pipelines.universal"
URP_TAG = re.compile(r'"RenderPipeline"\s*=\s*"UniversalPipeline"')
URP_INCLUDE = re.compile(r'#include\s+"Packages/' + re.escape(URP_PACKAGE) + r'/')
PACKAGE_REQUIREMENTS = re.compile(r'\bPackageRequirements\s*\{([^}]*)\}')


def strip_comments(text):
    """Remove // and /* */ comments, keeping strings and line numbers intact."""
    out = []
    i = 0
    in_string = False
    while i < len(text):
        c = text[i]
        if in_string:
            out.append(c)
            if c == '\\' and i + 1 < len(text):
                out.append(text[i + 1])
                i += 2
                continue
            if c == '"':
                in_string = False
            i += 1
        elif c == '"':
            in_string = True
            out.append(c)
            i += 1
        elif text.startswith('//', i):
            while i < len(text) and text[i] != '\n':
                i += 1
        elif text.startswith('/*', i):
            end = text.find('*/', i + 2)
            end = len(text) if end < 0 else end + 2
            out.append('\n' * text.count('\n', i, end))
            i = end
        else:
            out.append(c)
            i += 1
    return ''.join(out)


def subshaders(text):
    """Yield (line number, body) for every SubShader block, by brace matching."""
    for m in re.finditer(r'\bSubShader\s*\{', text):
        start = m.end()
        depth = 1
        i = start
        in_string = False
        while i < len(text) and depth > 0:
            c = text[i]
            if in_string:
                if c == '"':
                    in_string = False
            elif c == '"':
                in_string = True
            elif c == '{':
                depth += 1
            elif c == '}':
                depth -= 1
            i += 1
        yield text.count('\n', 0, m.start()) + 1, text[start:i - 1]


def check_file(path):
    """Return a list of error messages for one .shader file."""
    errors = []
    text = strip_comments(path.read_text(encoding='utf-8', errors='replace'))
    for line, body in subshaders(text):
        if not (URP_TAG.search(body) or URP_INCLUDE.search(body)):
            continue
        requirements = PACKAGE_REQUIREMENTS.search(body)
        if requirements is None or URP_PACKAGE not in requirements.group(1):
            errors.append(f'{path}:{line}: URP SubShader without PackageRequirements {{ "{URP_PACKAGE}" }} (see VR2Gather#365)')
    return errors


def main(args):
    roots = [Path(a) for a in args] or [Path('nl.cwi.dis.vr2gather')]
    files = []
    for root in roots:
        files.extend(sorted(root.rglob('*.shader')) if root.is_dir() else [root])
    if not files:
        print('check_urp_subshaders: no .shader files found', file=sys.stderr)
        return 1
    errors = []
    for f in files:
        errors.extend(check_file(f))
    for e in errors:
        print(e)
    print(f'check_urp_subshaders: {len(files)} shader file(s), {len(errors)} problem(s)')
    return 1 if errors else 0


if __name__ == '__main__':
    sys.exit(main(sys.argv[1:]))

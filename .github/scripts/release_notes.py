#!/usr/bin/env python3
"""Release notes for a tag, from nl.cwi.dis.vr2gather/CHANGELOG.md (#417).

    release_notes.py <tag> [--check]

- vX.Y.Z: package.json must have version X.Y.Z and the changelog a `## [X.Y.Z]` section;
  the notes are that section. Exits with an error otherwise, so a release with a forgotten
  version bump fails in the first minute instead of after the player builds.
- exp-*: an experiment build, not a release; the notes are a line saying so plus the
  `## [next]` section, if there is one.

With --check only the checks run, nothing is printed.
"""

import json
import os
import re
import sys

PACKAGE = os.path.join(os.path.dirname(__file__), "..", "..", "nl.cwi.dis.vr2gather")


def section(changelog, name):
    """The body of the `## [name]` section, or None."""
    m = re.search(r"^## \[" + re.escape(name) + r"\][^\n]*\n(.*?)(?=^## |\Z)", changelog, re.M | re.S)
    return m.group(1).strip() if m else None


def main():
    args = sys.argv[1:]
    check_only = "--check" in args
    args = [a for a in args if a != "--check"]
    if len(args) != 1:
        sys.exit(__doc__)
    tag = args[0]
    with open(os.path.join(PACKAGE, "CHANGELOG.md"), encoding="utf-8") as f:
        changelog = f.read()

    if re.fullmatch(r"v\d+\.\d+\.\d+", tag):
        version = tag[1:]
        with open(os.path.join(PACKAGE, "package.json"), encoding="utf-8") as f:
            package_version = json.load(f)["version"]
        if package_version != version:
            sys.exit(f"Tag {tag}, but package.json has version {package_version}")
        notes = section(changelog, version)
        if not notes:
            sys.exit(f"Tag {tag}, but CHANGELOG.md has no (or an empty) [{version}] section")
    elif tag.startswith("exp-"):
        sha = os.environ.get("GITHUB_SHA", "")[:9]
        notes = f"Experiment build {tag}" + (f" ({sha})" if sha else "") + ", not a release."
        unreleased = section(changelog, "next")
        if unreleased:
            notes += "\n\nChanges since the last release:\n\n" + unreleased
    else:
        sys.exit(f"Tag {tag}: not vX.Y.Z or exp-*")

    if not check_only:
        print(notes)


if __name__ == "__main__":
    main()

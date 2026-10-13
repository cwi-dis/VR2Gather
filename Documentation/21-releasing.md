# VR2Gather - Making a release

A release consists of a version tag in this repository, a GitHub release with built players, and an updated `VR2Gather_sample`.

## 1. Prepare

- The milestone for the release has no open issues left (move anything that isn't done to the next milestone).
- `nl.cwi.dis.vr2gather/CHANGELOG.md`: review the section of the new version. One short line per change, with the issue number; leave out things that are too small to mention.
- Anything that still needs a test on another platform or in VR has been tested. This includes building the players (step 3) on every platform, before tagging.

## 2. Version and tag

In `nl.cwi.dis.vr2gather/`:

- `package.json`: set `"version"` to the new version.
- `CHANGELOG.md`: change `## [X.Y.Z] - unreleased` to `## [X.Y.Z] - <date>`.

Commit both ("Bump version to X.Y.Z, update changelog"), add an annotated tag, and push both:

```
git tag -a vX.Y.Z -m "VR2Gather X.Y.Z"
git push origin master vX.Y.Z
```

## 3. Build the players

For now, by hand (some day CI will do this, see #355). Build `VRTApp-Develop` on each platform with `VRTApp-Develop/scripts/build.sh`, from the tagged commit, with the Editor closed (see [Building the application](20-building.md)):

- Windows: on a Windows machine, from Git Bash.
- macOS: on a Mac.
- Linux: on a Linux machine.

Each build leaves a zip in `VRTApp-Develop/Builds/`. Run each player once (login scene, and into a session) before releasing it.

## 4. GitHub release

Create a release for the tag, with the changelog section as text and the zips attached:

```
gh release create vX.Y.Z --title vX.Y.Z --notes-file <changelog section> VRTApp-Develop/Builds/*.zip
```

## 5. Update VR2Gather_sample

`VR2Gather_sample` uses the package from GitHub (not pinned to a version), plus a copy of its samples.

- Open the project in Unity. In the Package Manager, update VR2Gather to the new version; this changes `Packages/packages-lock.json`.
- In the Package Manager, VR2Gather, *Samples* tab: import the samples again (*VRT Essential Assets*). This replaces `Assets/Samples/VR2Gather/<old version>` by `<new version>`; check that the old folder is gone.
- Check that the login scene, Pilot0 and TechnicalPlayground still work.
- Commit ("Updated to VR2Gather X.Y.Z"), tag `vX.Y.Z`, and push both.

## 6. Clean up

- Close the milestone on GitHub, and create the next one if needed.
- Add a `## [next] - unreleased` section to `CHANGELOG.md`.

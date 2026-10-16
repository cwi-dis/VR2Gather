# VR2Gather - Making a release

A release consists of a version tag in this repository, a GitHub release with built players, and an updated `VR2Gather_sample`.

## 1. Prepare

- The milestone for the release has no open issues left (move anything that isn't done to the next milestone).
- `nl.cwi.dis.vr2gather/CHANGELOG.md`: while working, the `unreleased` section gets one short line per change, with the issue number. For the release, regroup it into a few themed lines, each with all its issue numbers, plus a final catch-all like "Various minor bugs fixed (#…, #…)". A reader should see at a glance what the release is about; the issues have the details.
- Anything that still needs a test on another platform or in VR has been tested. This includes building the players (step 4) on every platform, before tagging.

## 2. Version and tag

In `nl.cwi.dis.vr2gather/`:

- `package.json`: set `"version"` to the new version.
- `CHANGELOG.md`: change `## [X.Y.Z] - unreleased` to `## [X.Y.Z] - <date>`.

Commit both ("Bump version to X.Y.Z, update changelog"), add an annotated tag, and push both:

```
git tag -a vX.Y.Z -m "VR2Gather X.Y.Z"
git push origin master vX.Y.Z
```

## 3. Draft GitHub release

On GitHub, create a release for the tag (*Releases*, *Draft a new release*), titled `vX.Y.Z`, and save it as a **draft**: nobody sees it yet, and the players get added in the next step. As release notes, copy the themed lines of the changelog section, optionally with a sentence on what the release is mainly about.

Or from the command line, with the release notes in a file:

```
gh release create vX.Y.Z --draft --title vX.Y.Z --notes-file <release notes>
```

## 4. Build the players and publish

For now, by hand (some day GitHub Actions will do this, see #355). Build `VRTApp-Develop` on each platform with `VRTApp-Develop/scripts/build.sh`, from the tagged commit, with the Editor closed (see [Building the application](20-building.md)):

- Windows: on a Windows machine, from Git Bash.
- macOS: on a Mac.
- Linux: on a Linux machine.

Each build leaves a zip in `VRTApp-Develop/Builds/`. Run each player once (login scene, and into a session) before releasing it.

Rename the zips to the names earlier releases used (until `build.sh` makes them, see #367), and attach them to the draft release:

| `build.sh` makes | Upload as |
|------------------|-----------|
| `VR2Gather-windows.zip` | `VRTApp-Develop-built.zip` |
| `VR2Gather-mac.zip` | `VRTApp-Develop-built-mac.zip` |
| `VR2Gather-linux.zip` | `VRTApp-Develop-built-linux.zip` |

When all three are there, publish the release and set it as the latest release.

Or from the command line, after renaming the zips:

```
gh release upload vX.Y.Z VRTApp-Develop/Builds/VRTApp-Develop-built*.zip
gh release edit vX.Y.Z --draft=false --latest
```

## 5. Update VR2Gather_sample

`VR2Gather_sample` uses the package from GitHub (not pinned to a version), plus a copy of its samples.

- Update VR2Gather and its git dependencies (`nl.cwi.dis.cwipc`, `nl.cwi.dis.vr2gather.nativelibraries`, `nl.cwi.dis.vr2gather.nativelibraries.webrtc`) to the versions `VRTApp-Develop` was tested with: compare the hashes in both `Packages/packages-lock.json` files. Either use *Update* in the Package Manager for each, or delete their entries from the sample's `packages-lock.json` (with Unity closed) so Unity fetches the current master when it opens the project. Updating only VR2Gather leaves the others at their old versions.
- Packages that `VRTApp-Develop` has and the sample needs too: for now that includes the URP package, also for built-in render pipeline projects (#365).
- In the Package Manager, VR2Gather, *Samples* tab: import the samples again (*VRT Essential Assets*). This replaces `Assets/Samples/VR2Gather/<old version>` by `<new version>`; check that the old folder is gone.
- Check that the login scene, Pilot0 and TechnicalPlayground still work.
- Commit ("Updated to VR2Gather X.Y.Z"), tag `vX.Y.Z`, and push both.

## 6. Clean up

- Close the milestone on GitHub, and create the next one if needed.
- Add a `## [next] - unreleased` section to `CHANGELOG.md`.

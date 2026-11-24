# VR2Gather - Making a release

A release consists of a version tag in this repository, a GitHub release with built players, and an updated `VR2Gather_sample`.

## 1. Prepare

- The milestone for the release has no open issues left (move anything that isn't done to the next milestone).
- `nl.cwi.dis.vr2gather/CHANGELOG.md`: while working, the `unreleased` section gets one short line per change, with the issue number. For the release, regroup it into a few themed lines, each with all its issue numbers, plus a final catch-all like "Various minor bugs fixed (#…, #…)". A reader should see at a glance what the release is about; the issues have the details.
- Anything that still needs a test on another platform or in VR has been tested. The release players themselves are tried in step 4, before publishing.

## 2. Version and tag

In `nl.cwi.dis.vr2gather/`:

- `package.json`: set `"version"` to the new version.
- `CHANGELOG.md`: change `## [next] - unreleased` to `## [X.Y.Z] - <date>`.

Commit both ("Bump version to X.Y.Z, update changelog"), add an annotated tag, and push both:

```
git tag -a vX.Y.Z -m "VR2Gather X.Y.Z"
git push origin master vX.Y.Z
```

The tag starts the CI workflow (`.github/workflows/ci.yml`, issue #417): it builds the players from the tagged commit, runs the tests, and drafts the release. Its static checks fail within a minute if `package.json` doesn't have the tag's version or the changelog has no `[X.Y.Z]` section.

## 3. The draft release

When the workflow is green, GitHub has a **draft** release `vX.Y.Z`: nobody sees it yet. Its release notes are the changelog's `[X.Y.Z]` section; add a sentence on what the release is mainly about if you like. The players are attached:

- `VR2Gather-windows-x64.zip`
- `VR2Gather-linux-x64.zip`
- `VR2Gather-macos-universal.zip`: Intel and Apple silicon, but on Intel Macs not everything works (the ffmpeg libraries that Dash needs are Apple silicon only).
- `VR2Gather-quest-arm64.apk`: for Meta Quest, to sideload.

`build.sh` uses the same names for local builds. Up to 1.4.5 the zips were `VR2Gather-windows.zip`, `VR2Gather-mac.zip` and `VR2Gather-linux.zip`, and up to 1.4.4 `VRTApp-Develop-built*.zip` (#367); nothing depends on these names.

If a job fails for a passing reason, re-run the failed jobs: the release job then still drafts the release, or updates the draft. If the tagged commit itself needs a fix, delete the draft release and the tag (`gh release delete vX.Y.Z --cleanup-tag`), fix it, and tag again.

A test of the release workflow, or an experiment build: push a tag `exp-<something>`, also on a branch. It gives a draft pre-release, with the `[next]` section of the changelog as notes. Delete it afterwards with `gh release delete exp-<something> --cleanup-tag`.

## 4. Try the players and publish

Download each player from the draft release and run it once (login scene, and into a session), on its platform; install the Quest player on a Quest.

When they work, publish the release and set it as the latest release (*Edit*, *Publish release*), or from the command line:

```
gh release edit vX.Y.Z --draft=false --latest
```

## 5. Update VR2Gather_sample

`VR2Gather_sample` uses the package from GitHub (not pinned to a version), plus a copy of its samples.

- Update VR2Gather and its git dependencies (`nl.cwi.dis.cwipc`, `nl.cwi.dis.vr2gather.nativelibraries`, `nl.cwi.dis.vr2gather.nativelibraries.webrtc`) to the versions `VRTApp-Develop` was tested with: compare the hashes in both `Packages/packages-lock.json` files. Either use *Update* in the Package Manager for each, or delete their entries from the sample's `packages-lock.json` (with Unity closed) so Unity fetches the current master when it opens the project. Updating only VR2Gather leaves the others at their old versions.
- Packages that `VRTApp-Develop` has and the sample needs too: for now that includes the URP package, also for built-in render pipeline projects (#365).
- In the Package Manager, VR2Gather, *Samples* tab: import the samples again (*VRT Essential Assets*). This replaces `Assets/Samples/VR2Gather/<old version>` by `<new version>`; check that the old folder is gone.
- Run the Porting Check (*Tools → VR2Gather Porting Check*) on the login scene, Pilot0 and TechnicalPlayground: on freshly imported samples it should find no errors and offer no Fix buttons.
- Check that the login scene, Pilot0 and TechnicalPlayground still work, and that the Editor log has no new warnings (for example `UI-Overlay layer not found`: the sample's layers must match `VRTApp-Develop`'s, #411).
- If `VRTApp-Develop/scripts/build.sh` changed, copy it to the sample's `scripts/build.sh` (it's the same script in every project).
- Commit ("Updated to VR2Gather X.Y.Z"), tag `vX.Y.Z`, and push both.

## 6. Clean up

- Close the milestone on GitHub, and create the next one if needed.
- Add a `## [next] - unreleased` section to `CHANGELOG.md`.

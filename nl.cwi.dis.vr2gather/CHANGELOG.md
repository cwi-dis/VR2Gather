# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.2.8] - 2025-05-16

- Virtual hands are now "sticky" by default (#229)
- InputActions have been moved to the Samples, so they can be modified (#226)

## [1.2.7] - 2025-05-14

- Fixed config file location when running under VRTrunserver (#228)

## [1.2.6] - 2025-05-07

- Fixed config-user.json location for VRTrun-based runs (#225)

## [1.2.5] - 2025-05-01

- Allow auto-invention of usernames, rationalized auto-session (#224)

## [1.2.4] - 2025-04-18

- Cleaned up VRTLogin scene. Got rid of video, box, disabled teleportation. (#221)

## [1.2.3] - 2025-04-14

- Grabbing objects and releasing them again was broken (for at least the last year). Fixed. (#218)

## [1.2.1] - 2025-03-25

- VRTrunserver support integrated into built Windows players (#205)

## [1.2.0] - 2025-03-05

- VR2Gather_sample moved to its own repository
- cwipc updated to 7.6.0
- Built players now include native libraries needed (Windows only)
- WebRTC protocol support
- Low-Latency DASH support is being revived.

## [1.1.0] - 2024-10-31

- Preliminary WebRTC protocol support added (#166, #161, #102)
- Tile quality selector added. Decisions are probably wrong. (#182)

## [1.0.4] - 2024-10-23

- Bug fixes (#178, #189)
- Audio latency issues fixed (#144)
- PositionTracker added (#184, #191)
- Recording of point cloud raw video during session added

## [1.0.3] - 2024-06-02

- Initial view position for non-HMD use fixed (#177)

## [1.0.2] - 2024-06-02

- Skewed teleport line fixed (#175)
- Strange center-screen shape when not using HMD fixed (#135)

## [1.0.0] - 2024-05-28

- BestHTTP socketio replaced by open source package from itisnajim (#169)

## [0.9.8] - 2024-05-26

- Implemented tcpreflector transport protocol (#168)
- Implemented abstract transport protocol handlers, to enable easy addition of protocols (#150)
- Single port specified for tcp (point to point) audio transport, other ports are computed from that

## [0.9.7] - 2024-05-17

- Cleaner reaction to orchestrator disconnection (#173)
- Changed assembly names to match namespace names, at least somewhat. (#157)

## [0.9.6] - 2024-05-06

- Conversational audio handling implemented differently. (partial #144)
- In the editor there's a context menu on VRTConfig to create a config.json.

## [0.9.5] - 2024-04-19

- Usage of git-lfs rationalized. Should not affect usage (#163)
- Refactored LoginManager scene and renamed to VRTLoginManager (#164)
- Renamed and refactored voice-handling classes (#156)
- Fixed accidental clearing of user name (#152)
- Changes in LoginManager Settings panel take effect immedeately (#145)

## [0.9.4] - 2024-04-10

- Got rid of some spurious error messages
- Minor fixes to template project

## [0.9.3] - 2024-04-03

First public release.

## [0.9.2] - Unreleased

First restructuring that is ready for testing.
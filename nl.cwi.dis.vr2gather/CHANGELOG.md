# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

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
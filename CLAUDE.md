# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Collaborator Preferences

**Jack** (main contributor): If Jack suggests an approach or implementation strategy that you think isn't the best way, say so and explain why — don't just implement it silently.

## Project Overview

VR2Gather is a Unity package (`nl.cwi.dis.vr2gather`) for collaborative networked social VR with live volumetric video (point clouds). Participants appear as real-time 3D reconstructions in shared virtual spaces. Built for Unity 6000.3+.

- **Main package**: `nl.cwi.dis.vr2gather/`
- **Development project**: `VRTApp-Develop/` (Unity project that imports the package via relative path for in-place editing)
- **Orchestrator**: External cloud service for session management; local URL configurable in `config.json`

## Build and Development Commands

**Building** (from repo root):
```bash
./build-VR2Gather.sh          # Builds for current platform
./build-scripts/mac-fix-quarantine.sh  # macOS: fix code signing after build
```

Logs to `buildlog.txt`. Output goes to `Builds/`.

**Git setup** (required on Windows for symlinks in VRTApp-Develop):
```bash
git config --global core.symlinks true
git config --local core.symlinks true
```

**WebRTC helpers** (run once after clone):
```bash
cd webRTC-helpers && ./get_peer.sh && ./get_connector.sh
```

**CI/CD**: GitHub Actions (`.github/workflows/main.yml`) builds Windows standalone on branches matching `deployment/**` or tags matching `build*`.

There are no automated test commands — testing is done by running sample scenes in the Unity Editor (see Samples below).

## Architecture

### Key Concepts

VR2Gather diverges from standard Unity VR in important ways (see `Documentation/11-differences.md`):
- **No built-in MainCamera** — camera is created at runtime after scene load
- **One application instance per participant** — all run full business logic locally
- **Master/Follower pattern** — one instance (session creator) is designated master; critical actions (object creation) go through master first

### Package Structure (`nl.cwi.dis.vr2gather/Runtime/`)

16 assembly modules with clear dependency layers (bottom-up):

| Layer | Assemblies |
|-------|-----------|
| Foundation | `VRTCore`, `VRTInitializer`, `VRTProfiler`, `VRTDeprecated` |
| Framework | `VRTCommon` (48 files), `VRTOrchestrator` |
| User representation | `VRTUserPointClouds`, `VRTUserVoice`, `VRTUserWebCam` |
| Transport | `VRTTransportDash`, `VRTTransportSocketIO`, `VRTTransportTCP`, `VRTTransportTCPReflector`, `VRTTransportWebRTC` |
| Features | `VRTLogin`, `VRTVideo` |

### Scene Structure

Every VR2Gather scene requires the **`Tool_scenesetup`** prefab, which contains:
- `SessionController` — orchestrator communication
- `PilotController` — scene lifecycle (subclass this for custom behavior)
- `SessionPlayersManager` — spawns player prefabs per participant
- `TilingConfigDistributor`, `SyncConfigDistributor` — ensure consistency across instances

Player representations created at runtime:
- `P_Self_player` — local user's self-representation
- `P_Player` — one instance per remote participant

### User Representations

Configurable via `config-user.json`:
- `PointCloud` (0) — live 3D volumetric reconstruction from RGBD camera (CWIPC)
- `SimpleAvatar` (1) — minimal body + tracked head direction
- `WebCam` (2) — body + screen showing camera feed

### Networking

- `NetworkTrigger` — synchronized remote method calls
- `NetworkInstantiator` — master-controlled object creation
- `Grabbable` — hand sync for held objects
- `RigidBodyNetworkController` — physics state sync
- NTP-based time synchronization via `VRTSynchronizer`

### Configuration

Two JSON files in `VRTApp-Develop/`:
- `config.json` — shared defaults (orchestrator URL, codec settings, transport config)
- `config-user.json` — per-device overrides (camera, microphone, representation type)

`VRTConfig.cs` manages loading and merging these at runtime.

## Creating New Experiences

1. Copy the `Pilot0` sample scene as a starting point
2. Subclass `PilotController` for custom scene logic
3. Register the new scenario in `ScenarioRegistry`
4. Add a `LoginManager` scene as the entry point

See `Documentation/10-createnew.md` for the full walkthrough.

## Sample Scenes (in `Samples~/VRTAssets/Scenes/`)

| Scene | Purpose |
|-------|---------|
| `LoginManager/` | Entry point for all VR2Gather apps |
| `Pilot0/` | Minimal "Hello World" experience |
| `TechnicalPlayground/` | Full feature showcase (requires orchestrator) |
| `SoloPlayground/` | Test locally without orchestrator |
| `Empty/` | Blank template |

## Editor Utilities

In `nl.cwi.dis.vr2gather/Editor/Tools/`:
- `DependenciesHunter.cs` — analyze asset dependencies
- `MissingReferencesHunter.cs` — find broken asset references
- `FindMissingScriptsRecursively.cs` — find GameObjects with missing scripts

## External Dependencies

- **CWIPC** (`nl.cwi.dis.cwipc`) — point cloud codec (native libraries)
- **XR Interaction Toolkit** (`com.unity.xr.interaction.toolkit` v3.3.1) — core XR input
- **OpenXR / Oculus** — headset support
- **lldash** (`nl.cwi.dis.vr2gather.nativelibraries`) — DASH streaming native libs
- **WebRTC** (`nl.cwi.dis.vr2gather.nativelibraries.webrtc`) — P2P transport
- **SocketIO** (`com.itisnajim.socketiounity`) — orchestrator WebSocket communication

Git LFS is used for large binary assets — run `git lfs install` before first clone.

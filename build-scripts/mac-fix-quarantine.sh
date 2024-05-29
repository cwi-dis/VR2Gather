#!/bin/sh
#
# This script clears the quarantine bits on downloaded dylibs and executables
#
xattr -d com.apple.quarantine webRTC-helpers/webRTC-peer-macos.exe
xattr -d com.apple.quarantine nl.cwi.dis.vr2gather/Runtime/VRTTransportWebRTC/Plugins/macos/WebRTCConnector.dylib

#!/bin/bash
# Usage
case x$1 in
x)
	echo Usage: $0 release
	echo Will download distributions from https://github.com/jvdrhoof/WebRTCConnector/releases/download/release
	echo and install the dynamic libraries in the correct place in nl.cwi.dis.vr2gather.
	exit 1
	;;
x*)
	release=$1
	;;
esac
set -x
mkdir -p tmp
curl --location --output tmp/webrtcconnector-macos.tgz https://github.com/jvdrhoof/WebRTCConnector/releases/download/${release}/webrtcconnector-x86_64-apple-darwin.tgz
curl --location --output tmp/webrtcconnector-win.tgz https://github.com/jvdrhoof/WebRTCConnector/releases/download/${release}/webrtcconnector-x86_64-unknown-windows.tgz
curl --location --output tmp/webrtcconnector-linux.tgz https://github.com/jvdrhoof/WebRTCConnector/releases/download/${release}/webrtcconnector-x86_64-unknown-linux.tgz
rm -rf tmp/win tmp/macos tmp/linux
mkdir -p tmp/win tmp/macos tmp/linux
(cd tmp/win ; tar xfv ../webrtcconnector-win.tgz)
(cd tmp/macos ; tar xfv ../webrtcconnector-macos.tgz)
(cd tmp/linux ; tar xfv ../webrtcconnector-linux.tgz)
case `uname` in
Darwin)
	xattr -d com.apple.quarantine tmp/macos/lib/libWebRTCConnector.dylib
	;;
esac
cp tmp/macos/lib/libWebRTCConnector.dylib ../nl.cwi.dis.vr2gather/Runtime/VRTTransportWebRTC/Plugins/macos/WebRTCConnector.dylib
cp tmp/win/bin/WebRTCConnector.dll ../nl.cwi.dis.vr2gather/Runtime/VRTTransportWebRTC/Plugins/win/WebRTCConnector.dll
# Linux to be done
rm -r tmp

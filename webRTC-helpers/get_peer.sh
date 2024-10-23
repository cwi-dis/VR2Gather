#!/bin/bash
# Usage
case x$1 in
x)
	echo Usage: $0 release
	echo Will download binaries from https://github.com/jvdrhoof/WebRTCSFU/releases/download/release
	echo and install them in the correct place here
	exit 1
	;;
x*)
	release=$1
	;;
esac
set -x
mkdir -p tmp
curl --location --output tmp/webrtcsfu-macos.tgz https://github.com/jvdrhoof/WebRTCSFU/releases/download/${release}/webrtcsfu-x86_64-apple-darwin.tgz
curl --location --output tmp/webrtcsfu-win.tgz https://github.com/jvdrhoof/WebRTCSFU/releases/download/${release}/webrtcsfu-x86_64-unknown-windows.tgz
curl --location --output tmp/webrtcsfu-linux.tgz https://github.com/jvdrhoof/WebRTCSFU/releases/download/${release}/webrtcsfu-x86_64-unknown-linux.tgz
rm -rf win macos linux
mkdir win macos linux
(cd win ; tar xfv ../tmp/webrtcsfu-win.tgz)
(cd macos ; tar xfv ../tmp/webrtcsfu-macos.tgz)
(cd linux ; tar xfv ../tmp/webrtcsfu-linux.tgz)
case `uname` in
Darwin)
	xattr -d com.apple.quarantine macos/bin/*
	;;
esac
rm -rf tmp

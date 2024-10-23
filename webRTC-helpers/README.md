# webRTC-helpers

This directory contains executables for the helper programs for webRTC.

They are named `win/bin/webRTCSFU-peer.exe`,  `macos/bin/webRTCSFU-peer` and `linux/bin/webRTCSFU-peer` for the respective platforms.

They are built from repository <https://github.com/jvdrhoof/WebRTCSFU>.

The helper programs are used by the connector DLL (see below). Usually the `peer` program and the `connector` dll need to be updated at the same time.

## Installing new versions

Whenever there is a new release _vX.Y.Z_ done on the _WebRTCSFU_ github page, install the executables here by running

```
./get_peer.sh vX.Y.Z
```

After that you may need to make the new version available to everyone by doing

```
git commit -a -m "Installed new webrtc peer versions"
git push
```

## Installing locally-built peer binary

If you're developing the WebRTC peer locally you can simply copy your executable to the right place.

## Installing the connector DLL

The connector DLL is built and released from a different repository, <https://github.com/jvdrhoof/WebRTCConnector>.

But the procedure for installing a new release _vX.Y.Z_ is very similar

```
./get_connector.sh vX.Y.Z
```

The DLLs will be unpacked here but then copied to somewhere in `../nl.cwi.dis.vr2gather/Runtime/VRTTransportWebRTC/Plugins`.

So, after that you may need to make the new version available to everyone you do

```
cd ..
git commit -a -m "Installed new webrtc peer versions"
git push
```




using System.Net;
using System.Net.Sockets;
using UnityEngine;

public class VoiceSender {
    SocketIOServer fakeServer;
    int playerID;
    public VoiceSender(int playerID, SocketIOServer fakeServer) {
        this.playerID = playerID;
        this.fakeServer = fakeServer;

    }

    // Multy-threader function
    public async void Send(float[] buffer) {
        // Do a lot of stuff.
        if (fakeServer != null) fakeServer.Send(buffer);
    }
}

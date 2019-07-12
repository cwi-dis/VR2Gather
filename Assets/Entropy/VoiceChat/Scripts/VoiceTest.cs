using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VoiceTest : MonoBehaviour {
    public VoicePlayer player;
    SpeeX compressor;
    // Start is called before the first frame update
    IEnumerator Start() {
        int userID = 0;
        string url = "http://localhost:9000/";
        //string url = "https://vrt-evanescent.viaccess-orca.com/audio/";
        SocketIOServer.player[userID] = player;
        compressor = new SpeeX();        
        MicroRecorder.Instance.Init(userID, true, true); // userID, useEcho, useSocket
        if (SocketIOServer.Instance != null) {
            yield return null;
            player.Init();
        }
        else {
            yield return new WaitForSeconds(1);
            player.Init($"player_{userID}", $"{url}player_{userID}.mpd");
        }
    }

    // Update is called once per frame
    void Update() {
        
    }
}

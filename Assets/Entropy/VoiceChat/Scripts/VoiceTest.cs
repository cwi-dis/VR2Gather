using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VoiceTest : MonoBehaviour
{
    public VoicePlayer player;
    SpeeX compressor;
    // Start is called before the first frame update
    IEnumerator Start()
    {
        SocketIOServer.player[0] = player;


        compressor = new SpeeX();
        MicroRecorder.Instance.Init(0, true);

        yield return new WaitForSeconds(1);
        player.Init("player_1", "http://localhost:9000/player_1.mpd");
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}

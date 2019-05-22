using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VoiceSender
{
    int playerID;
    public VoiceSender(int playerID) {
        this.playerID = playerID;
    }

    // Multy-threader function
    public void Send(float[] buffer) {

    }
}

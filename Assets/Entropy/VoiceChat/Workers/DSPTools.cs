using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DSPTools : MonoBehaviour {
    void Awake() {
        var ac = AudioSettings.GetConfiguration();
        ac.sampleRate = 16000 * 3;
        ac.dspBufferSize = 320 * 3;
        AudioSettings.Reset(ac);
    }
}
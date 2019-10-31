using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TriggerHelper : MonoBehaviour {

    int value;
    OscJack.OscPropertySender sender;

    private void Start() {
        value = 0;
        sender = GameObject.Find("Sender").GetComponent<OscJack.OscPropertySender>();
        sender._oscAddress = "";
    }

    public void SendPlay(string str) {
        if (value == 0) value = 1;
        else value = 0;
        sender._oscAddress = str;
        sender.Send(value);
    }

    public void SendPause(string str) {
        if (value == 0) value = 1;
        else value = 0;
        sender._oscAddress = str;
        sender.Send(value);
    }

}

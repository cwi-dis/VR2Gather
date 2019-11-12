using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Subtitles : MonoBehaviour
{
    SRTParser subtitles;
    UnityVideoPlayer player;
    Text text;
    // Use this for initialization
    public void Load(UnityVideoPlayer player) {
        text = GetComponentInChildren<Text>();
        text.text = "";
        this.player = player;
        string fileName = player.URI + ".srt";
        try {
            subtitles = new SRTParser(System.IO.File.ReadAllText(fileName));
        } catch {
            Debug.LogError("No subtitles " + fileName);
            // no valid file or error.
        }
    }

    int currentId = -1;
    // Update is called once per frame
    void Update() {
        if (player!=null && subtitles!=null) {
            var current = subtitles.GetForTime((float)player.Position);
            if (current.Index != currentId) {
                text.text = current.Text;
            }
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChromaVideoPlayer : MonoBehaviour {
    public double fastforward;
    public string fileName;
    UnityEngine.Video.VideoPlayer vp;
    public bool play;
    bool oldPlay =false;

    // Use this for initialization
    void Start () {
        vp = GetComponent<UnityEngine.Video.VideoPlayer>();
        vp.url = "file:///" + Application.streamingAssetsPath + "/" + fileName;
        vp.time = fastforward;
        vp.Prepare();
       // gameObject.SetActive(false);

    }
	
	// Update is called once per frame
	void Update () {
        if (play != vp.isPlaying) {
            if (play) vp.Play();
            else vp.Pause();
        }
	}

    public void OnPlay() {
        gameObject.SetActive(true);
        play = true;
    }
}

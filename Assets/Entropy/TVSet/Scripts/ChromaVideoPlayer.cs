using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChromaVideoPlayer : MonoBehaviour {
    public double fastforward;
    public string fileName;
	// Use this for initialization
	void Start () {
        var vp = GetComponent<UnityEngine.Video.VideoPlayer>();
        vp.url = "file:///" + Application.streamingAssetsPath + "/" + fileName;
        vp.time = fastforward;
        vp.Play();
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}

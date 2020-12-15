using UnityEngine;
using UnityEngine.Video;

public class InitializeVideo : MonoBehaviour {

    VideoPlayer video;
    public string url;

    // Start is called before the first frame update
    void Awake() {
        video = gameObject.GetComponent<VideoPlayer>();
        url = Application.streamingAssetsPath + "/" + url;
        video.url = url;
    }
}

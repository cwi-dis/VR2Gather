using UnityEngine;

public class VideoForwardTool : MonoBehaviour {

    UnityEngine.Video.VideoPlayer videoPlayer;
    [SerializeField] double forward;

    // Start is called before the first frame update
    void Start() {
        videoPlayer = GetComponent<UnityEngine.Video.VideoPlayer>();
    }

    private void Update() {
        if (videoPlayer.isPlaying && videoPlayer.time >= forward) {
            forward = videoPlayer.time;
        }
    }

    public void OnPlay() {
        videoPlayer.time = forward;
    }

}

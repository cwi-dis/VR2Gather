using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;

public class PresenterController : MonoBehaviour {
    
    public void PlayVideo(VideoPlayer video) {
        video.Play();
    }

    public void PauseVideo(VideoPlayer video) {
        video.Pause();
    }

}

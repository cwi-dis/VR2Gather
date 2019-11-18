using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;

public class UnityVideoPlayer : MonoBehaviour {
    VideoPlayer     video;

    public Texture  Texture { get { return video.texture; } }
    public string   URI { get; private set; }
    public virtual float Position { get { return video!=null?(float)video.time:0; } }

    public virtual bool IsPlaying { get { return video!= null && video.isPlaying; } }

    public virtual void Play() { video.Play(); }
    public virtual void Pause() { video.Pause(); }
    public virtual void Stop() { video.Stop(); }

    public Subtitles     subtitles;

    bool isPrepared = false;

    // Use this for initialization
    public virtual void Initialize(string url) {
        URI = Application.streamingAssetsPath + "/videos/" + url;
        var renderer = GetComponent<Renderer>();
        video = GetComponent<VideoPlayer>();
        video.prepareCompleted += Video_prepareCompleted;
        if (video != null) {
            video.url = URI + ".mp4";
            subtitles?.Load(this);
            video.Prepare();
        }
    }

    public virtual void OnApplicationQuit() {
        video.prepareCompleted -= Video_prepareCompleted;
        video.Stop();
    }

    protected void Video_prepareCompleted(VideoPlayer source) {
        MainSceneController.Instance.OnPrepare();
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;

public class UnityMeshPlayer: UnityVideoPlayer {

    public override float    Position { get { return 0; } }

    public override bool IsPlaying { get { return true; } }

    public override void Play() { }
    public override void Pause() { }
    public override void Stop() { }
    
    // Use this for initialization
    public override void Initialize(string url) {

    }

}

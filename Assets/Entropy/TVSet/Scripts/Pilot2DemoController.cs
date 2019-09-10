using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Pilot2DemoController : MonoBehaviour
{
    public float             initialWaitTime;
    public HeaderController  Header;
    public ChromaVideoPlayer Presenter;
    public float             timeFromPresenterToHoward;
    public ChromaVideoPlayer Howard;
    public float             timeFromPresenterToVRSphere;
    public ChromaVideoPlayer VRSphere;
    public Animation         openPlato;
    // Start is called before the first frame update
    void Start()
    {
        OnIntro();
    }

    // Update is called once per frame
    void Update()
    {
        if (Presenter.play && !Howard.play) {
            timeFromPresenterToHoward -= Time.deltaTime;
            if (timeFromPresenterToHoward < 0)
                Howard.OnPlay();
        }
        if (Presenter.play && !VRSphere.play) {
            timeFromPresenterToVRSphere -= Time.deltaTime;
            if (timeFromPresenterToVRSphere < 0)
                OnDirectConnection();
        }
    }

    public void OnIntro() {
        Header.OnPlay(()=> { OnPresenter();  });

    }

    public void OnPresenter() {
        Presenter.OnPlay();
    }

    public void OnDirectConnection() {
        openPlato.Play("Take 001");
        VRSphere.OnPlay();
    }
}

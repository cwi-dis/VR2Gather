using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Pilot2DemoController : MonoBehaviour
{
    public float             initialWaitTime;
    public HeaderController  Header;
    public ChromaVideoPlayer Presenter;
    public float             timeFromPresenterToVRSphere;
    public ChromaVideoPlayer VRSphere;
    public Animation         openPlato;
    public bool              platoIsOpened;
    // Start is called before the first frame update
    void Start()
    {
        OnIntro();
    }

    // Update is called once per frame
    void Update()
    {
        
        if (Presenter.play && !platoIsOpened) {
            timeFromPresenterToVRSphere -= Time.deltaTime;
            if (timeFromPresenterToVRSphere < 0)
                OnDirectConnection();
        }
    }

    public void OnIntro() {
	    OnPresenter();
        Header.OnPlay(()=> { });
    }

    public void OnPresenter() {
        Presenter.OnPlay();
        VRSphere.OnPlay();
    }

    public void OnDirectConnection() {
        openPlato.Play("Take 001");
        platoIsOpened = true;
        //
    }
}

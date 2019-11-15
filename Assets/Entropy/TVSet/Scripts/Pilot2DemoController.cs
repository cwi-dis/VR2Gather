using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;

public class Pilot2DemoController : MonoBehaviour
{
    public float             initialWaitTime;
    public HeaderController  Header;
    public ChromaVideoPlayer PresenterLocal;
    public GameObject        PresenterGub;
    public GameObject HowardLocal;
    public GameObject HowardLive;
    public GameObject DomeLocal;
    public GameObject DomeLive;
    public GameObject Advisor1;
    public GameObject Advisor2;
    public GameObject Advisor3;
    public float             timeFromPresenterToVRSphere;
    public ChromaVideoPlayer VRSphere;
    public Animation         openPlato;
    public VideoPlayer domeVideo;
    public GameObject domeWindow;
    public bool              platoIsOpened;
    public bool              isLive = false;
    // Start is called before the first frame update
    void Start() {
        OnIntro();
    }

    // Update is called once per frame
    void Update() {
        if (!isLive) {
            if (PresenterLocal.play && !platoIsOpened) {
                timeFromPresenterToVRSphere -= Time.deltaTime;
                if (timeFromPresenterToVRSphere < 0)
                    OnDirectConnection();
            }
        }
        else {
            if (platoIsOpened && !domeVideo.isPlaying && domeVideo.frame >= 60) {
                OnDirectDisconnection();
            }
        }
    }

    public void OnIntro() {
	    OnPresenter();
        Header.OnPlay(()=> { });
    }

    public void OnPresenter() {
        if (!isLive) {
            PresenterLocal.OnPlay();
            VRSphere.OnPlay();
        }
        else {
            PresenterLocal.gameObject.SetActive(false);
            HowardLocal.SetActive(false);
            DomeLocal.SetActive(false);
            Advisor1.SetActive(false);
            Advisor2.SetActive(false);
            Advisor3.SetActive(false);
            PresenterGub.SetActive(true);
        }
    }

    public void OnDirectConnection() {
        openPlato.Play("Take 001");
        platoIsOpened = true;
    }

    public void OnDirectDisconnection() {
        domeWindow.SetActive(true);
        platoIsOpened = false;
    }
}

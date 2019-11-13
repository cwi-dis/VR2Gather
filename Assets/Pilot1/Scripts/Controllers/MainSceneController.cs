using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

public class MainSceneController : MonoBehaviour {
    public static MainSceneController Instance { get; private set; }

    public string           videoPlayer1URI = "player1";
    public string           videoPlayer2URI = "player1";
    public float            EndTime = 414.0f;
    public AudioSource      introAudioSource;

    public GameObject player1;
    public GameObject player2;

    public UnityVideoPlayer player1VideoPlayer;
    public UnityVideoPlayer player2VideoPlayer;
    public Renderer         player1MonitorRenderer;
    public Renderer         player2MonitorRenderer;
    public Camera           player1Camera;
    public Camera           player2Camera;
    
    new Animation animation;
    AnimationState animationState;

    public UnityVideoPlayer currentVideoPlayer { get; private set; }

    void Awake() {
        Instance = this;
    }

    private void OnEnable() {
        RenderSettings.fog = true;

        animation = GetComponent<Animation>();        

        player1.SetActive(PoliceController.Instance.my_id == 0);
        player2.SetActive(PoliceController.Instance.my_id == 1);

        string url = PoliceController.Instance.my_id == 0 ? videoPlayer1URI : videoPlayer2URI;

        currentVideoPlayer = PoliceController.Instance.my_id == 0 ? player1VideoPlayer : player2VideoPlayer;

        player1VideoPlayer.transform.position -= Vector3.forward * 0.55f;
        player2VideoPlayer.transform.position -= Vector3.forward * 0.55f;
        player1VideoPlayer.transform.localScale *= 1.5f;
        player2VideoPlayer.transform.localScale *= 1.5f;

        currentVideoPlayer.Initialize(url);
    }

    public void OnPrepare() {
        (PoliceController.Instance.my_id == 0 ? player1MonitorRenderer : player2MonitorRenderer).sharedMaterial.mainTexture = currentVideoPlayer.Texture;
        IsPrepared(null, 0, 0);
    }

    IEnumerator AudioIntroPlay() {
        animation.clip.legacy=true;
        animation.Play("Doors");
        
        currentVideoPlayer.Play();
        yield return new WaitForSeconds(0.5f);
        currentVideoPlayer.Pause();
        animation["Doors"].speed = 0;
        PoliceController.Instance.Continue();
            introAudioSource.Play();
            yield return new WaitForSeconds(introAudioSource.clip.length - 10);
        currentVideoPlayer.Play();
        animation["Doors"].time = currentVideoPlayer.Position;
        animation["Doors"].speed = 1;
    }

    void Update() {
       // animation.clip.SampleAnimation(gameObject, currentVideoPlayer.Position);
        if (currentVideoPlayer.Position >= EndTime ) {
            PoliceController.Instance.Continue();
        }
	}

    public void IsPrepared(byte[] buffer, int offset, int size) {
        StartCoroutine(AudioIntroPlay());
    }
}

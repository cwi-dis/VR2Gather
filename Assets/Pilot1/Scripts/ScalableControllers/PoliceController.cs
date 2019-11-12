using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class PoliceController : MonoBehaviour {

    private bool waiting;
    public int my_id;

    GameObject IntroScene;
    GameObject MainScene;
	GameObject CreditsScene;
    [SerializeField] GameObject[] TVM;

    public static PoliceController Instance { get; private set; }

    public UnityVideoPlayer currentVideoPlayer { get; private set; }

    public Material FaderMaterial;

    // Start is called before the first frame update
    void Awake() {
        if (Application.isEditor) Application.runInBackground = true;

        Instance = this;
    }

    IEnumerator Start() {
        IntroScene = transform.Find("IntroScene").gameObject;
		MainScene = transform.Find("MainScene").gameObject;
		CreditsScene = transform.Find("CreditsScene").gameObject;

        FaderMaterial.color = Color.black;

        // Intro Cars Scene
        IntroScene.SetActive(true);
		//Move Intro to see the player 2 in cars scene.
		if (my_id == 1) IntroScene.transform.Translate(new Vector3(-5,0,0), Space.World);
        yield return FaderTo(0);
        yield return new WaitForSeconds(IntroSceneController.Instance.introDuration);
        for (int i = 0; i < 2; ++i) {
            setRenderes(TVM[i], false);
            if (i == my_id) setRenderes(TVM[i], true);
        }
        yield return new WaitForSeconds(IntroSceneController.Instance.introDuration);
        yield return FaderTo(1);

        // Prepare Main Scene
        IntroScene.SetActive(false);
        for (int i = 0; i < 2; ++i) { 
            setRenderes(TVM[i], true); 
        };        
        MainScene.SetActive(true);
        Debug.Log("Waiting");
        yield return FaderTo(0);
        // Esperar a que todos los usuarios esten
        waiting = true; while (waiting) { yield return null; }
        Debug.Log("InGame");

        // Espera final de la experiencia y fade
        waiting = true; while (waiting) { yield return null; if (Input.GetKeyDown(KeyCode.Space)) waiting = false; }
        yield return FaderTo(1);

		// Credits
		transform.Find("MainScene/InterrogatoryRoom").gameObject.SetActive(false);
		transform.Find("MainScene/Events").gameObject.SetActive(false);
		transform.Find("MainScene/Player1/VideoSprites").gameObject.SetActive(false);
		transform.Find("MainScene/Player1/VideoWindow").gameObject.SetActive(false);
		transform.Find("MainScene/Player1/CC").gameObject.SetActive(false);
		transform.Find("MainScene/Player2/VideoSprites").gameObject.SetActive(false);
		transform.Find("MainScene/Player2/VideoWindow").gameObject.SetActive(false);
		transform.Find("MainScene/Player2/CC").gameObject.SetActive(false);
		CreditsScene.SetActive(true);

		yield return FaderTo(0);
	}

    void setRenderes(GameObject TVMs, bool useTVMs) {
        Renderer[] renderers = TVMs.GetComponentsInChildren<Renderer>();
        foreach (var renderer in renderers)
            renderer.enabled = useTVMs;
    }


    int cnt = 0;
    void Update() {
    }

    void OnApplicationQuit() {
    }

    public void Continue() {
        waiting = false;
    }

    IEnumerator FaderTo(float dst, float time = 0.5f) {
        Color color = FaderMaterial.color;
        float org = color.a;
        float cnt = 0;
        float inc = 1 / time;
        while (cnt < 1) {
            cnt += Time.deltaTime * inc;
            color.a = Mathf.Lerp(org, dst, cnt);
            AudioListener.volume = 1 - color.a;
            FaderMaterial.color = color;
            yield return null;
        }
        color.a = dst;
        AudioListener.volume = 1 - color.a;
        FaderMaterial.color = color;
    }

    public IEnumerator WaitForSeconds(float seconds) {
        float timer = 0;
        while (timer < seconds) {
            if (!XRDevice.isPresent || XRDevice.userPresence == UserPresenceState.Present) timer += Time.deltaTime;
            else timer = 0;
            yield return null;
        }
    }
}

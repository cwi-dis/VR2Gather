using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HeaderController : MonoBehaviour
{
    public delegate void Callback();
    new public Animation animation;
    public AudioSource audioSource;
    new public Camera camera;
    int orgCullingMask;
    // Start is called before the first frame update
    void Awake() {
        orgCullingMask = camera.cullingMask;
        Debug.Log($"orgCullingMask {orgCullingMask}");
    }

    public void OnPlay(Callback callback ) {
        Debug.Log($"OnPlay ");
        gameObject.SetActive(true);
        camera.cullingMask = LayerMask.GetMask("Header");
        animation.Play("Take 001");
        audioSource.Play();
        StartCoroutine(WaitStop(animation["Take 001"].length, callback));
    }

    IEnumerator WaitStop(float time, Callback callback) {
        Debug.Log($"Time {time}");
        while (time > 0) {
            time -= Time.deltaTime;
            yield return null;
        }
        Debug.Log($"OnEnd ");
        gameObject.SetActive(false);
        camera.cullingMask = orgCullingMask;
        Debug.Log($"camera.cullingMask {camera.cullingMask} ");
        if (callback != null) callback();
    }
}

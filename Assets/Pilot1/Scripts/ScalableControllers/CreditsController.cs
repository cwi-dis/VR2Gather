using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CreditsController : MonoBehaviour {
    public Renderer     Credits;
    public float        creditTime = 2f;
    public Texture[]    Secuence;
    Material            material;
	bool exit = false;

    int                 index = 0;

    // Use this for initialization
    private void OnEnable() {
        Credits.material = material = Instantiate(Credits.material);
        material.color = Color.black;
        StartCoroutine(ShowCredits());
    }

    IEnumerator ShowCredits() {
        yield return new WaitForSeconds(1);
        while (index < Secuence.Length) {
            material.mainTexture = Secuence[index++];
            yield return FaderTo(1);
            yield return new WaitForSeconds(creditTime);
            yield return FaderTo(0);
			if (index == Secuence.Length) yield return new WaitForSeconds(300);
		}
		yield return new WaitForSeconds(300);
		//SceneManager.LoadScene("00.TVMCalibration");
	}


    IEnumerator FaderTo(float dst, float time = 0.5f) {
        float org = material.color.r;
        float cnt = 0;
        float inc = 1 / time;
        while (cnt < 1) {
            cnt += Time.deltaTime * inc;
            material.color = Color.white * Mathf.Lerp(org, dst, cnt);
            yield return null;
        }
        material.color = Color.white * dst;
    }

}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RandomAnimation : MonoBehaviour {
	void OnEnable() {
        Animation animation = GetComponent<Animation>();
        animation[animation.clip.name].normalizedTime = Random.value;
    }
}

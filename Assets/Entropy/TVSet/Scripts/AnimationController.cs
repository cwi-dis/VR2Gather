using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimationController : MonoBehaviour {
    Animator animator;
    // Use this for initialization
    void Start () {
        animator = GetComponent<Animator>();
        animator.speed = 0;

    }
	
	// Update is called once per frame
	void Update () {
        if( Input.GetKeyDown(KeyCode.Space))
            animator.speed = 1;
    }
}

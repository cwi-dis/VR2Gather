using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EventManager : MonoBehaviour
{
    [System.Serializable]
    public class ActiveObjectEvent
    {
        public float time;
        public GameObject gameObject;
        public bool active;
    };

    public ActiveObjectEvent[] events;
    int current = 0;

    UnityVideoPlayer videoplayer;
    // Use this for initialization
    void Start() {
        videoplayer = FindObjectOfType<UnityVideoPlayer>();
        ProcessEvents();
        current = 0;
    }

    // Update is called once per frame
    void Update() {
        ProcessEvents();
    }

    void ProcessEvents() {
        while (current < events.Length && events[current].time <= videoplayer.Position ) {
            events[current].gameObject.SetActive(events[current].active);
            current++;
        }
    }
}


using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TimersControllPanel : MonoBehaviour
{
    private bool start = false;
    private float time;
    public delegate void timerEnd();
    public event timerEnd OnTimerEnd;
    public string SceneName;
    void ChangeSceneAdditive(string Scene)
    {
        SceneManager.LoadScene(Scene,LoadSceneMode.Additive);
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }
    public void setTimer(float timeToWait )
    {
        if (!start)
        {
            time = timeToWait;
            start = true;
            Debug.Log("Timer started");
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (start)
        {
            Debug.Log("Remaining time: " + time.ToString());
            if (time > 0)
            {
                time -= Time.deltaTime;
                
            }
            else
            {
                Debug.Log("Time has run out!");
                OnTimerEnd();
                time = 0;
                start = false;
            }
        }

    }
}

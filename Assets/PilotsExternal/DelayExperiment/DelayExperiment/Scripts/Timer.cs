using UnityEngine;


public class Timer : MonoBehaviour
{
    public delegate void OnTimerEvent();

    private OnTimerEvent CallBackFunction;          // the function that is called when the time is up
    private float OriginalTimeInterval;                 // used for resetting
    private float TimeLeft;                                 // used for counting down
    private bool Active = false;


    // setup the timer: how long should the timer wait and which function should it call when the event is triggered
    public void SetTimer(float TimeInterval, OnTimerEvent NewCallBackFunction)
    {
        TimeLeft = TimeInterval;
        CallBackFunction = NewCallBackFunction;
    }


    // actually start the timer:
    public void StartTimer()
    {
        Active = true;
    }


    // I'm not using this, but whatever:
    public void StopTimer()
    {
        Active = false;
    }


    // ohwell
    public void ResetTimer()
    {
        TimeLeft = OriginalTimeInterval;
    }


    public void DestroyTimer()
    {
        Destroy(this);
    }


    // TimeLeft is decreased by Time.deltaTime every tick, if it hits 0 then the CallBackFunction is called
    void Update()
    {
        if (Active == true)
        {
            TimeLeft -= Time.deltaTime;
            if (TimeLeft <= 0)
            {
                CallBackFunction();
                StopTimer();
                DestroyTimer();

            }
        }
    }
}

/* This class is for mixing coroutine with regular actions. */
     
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ViveSR_Experience_ActionSequence : MonoBehaviour
{
    List<System.Action> ActionSequence = new List<System.Action>();
    bool isCurrentActionDone = true;
    bool isRunning;

    public static ViveSR_Experience_ActionSequence CreateActionSequence(GameObject sender)
    {
        return sender.AddComponent<ViveSR_Experience_ActionSequence>();
    }

    public void ActionFinished()
    {
        isCurrentActionDone = true;
    }

    public void AddAction(System.Action Action)
    {
        ActionSequence.Add(Action);
    }

    public void StartSequence()
    {
        StartCoroutine(StartActionSequence());
    }

    public void StopSequence()
    {
        isRunning = false;
    }

    IEnumerator StartActionSequence()
    {
        isRunning = true;
        int Num = 0;
        while (Num < ActionSequence.Count && isRunning)
        {
            if (isCurrentActionDone)
            {
                isCurrentActionDone = false;
                ActionSequence[Num]();
                Num += 1;
            }
            yield return new WaitForEndOfFrame();
        }

        isRunning = false;
        Destroy(this);
    }                  
}

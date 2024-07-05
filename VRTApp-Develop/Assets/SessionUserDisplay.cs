using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRT.Core;
using VRT.Pilots.Common;

public class SessionUserDisplay : MonoBehaviour
{
    /*
    private float interval = 10.0f; // Interval in seconds
    private float nextTime = 0.0f;  // Next time to print the user list

    // Start is called before the first frame update
    void Start()
    {
        nextTime = Time.time + interval;
    }

    // Update is called once per frame
    void Update()
    {
        if (Time.time >= nextTime)
        {
            GetAllUserIDs();
            nextTime += interval; // Update the next time to print
        }
    }

    void GetAllUserIDs()
    {
        var sessionPlayersManager = SessionPlayersManager.Instance;
        if (sessionPlayersManager != null)
        {
            List<PlayerNetworkControllerBase> allUsers = sessionPlayersManager.GetAllUsers();
            Debug.Log($"Total users in session: {allUsers.Count}");
            foreach (var user in allUsers)
            {
                Debug.Log($"User ID: {user.UserId}");
            }
        }
        else
        {
            Debug.LogWarning("SessionPlayersManager instance is not found.");
        }

    }*/

    private float interval = 10.0f; // Interval in seconds
    private float nextTime = 0.0f;  // Next time to print the user list

    // Values to set in the VRTSynchronizer script
    public int requestAudioBehindMs = 1000;
    public int requestNonAudioBehindMs = 2000;

    // Start is called before the first frame update
    void Start()
    {
        nextTime = Time.time + interval;
    }

    // Update is called once per frame
    void Update()
    {
        if (Time.time >= nextTime)
        {
            GetAllUserIDsAndUpdateSynchronizer();
            nextTime += interval; // Update the next time to print
        }
    }

    void GetAllUserIDsAndUpdateSynchronizer()
    {
        var sessionPlayersManager = SessionPlayersManager.Instance;
        if (sessionPlayersManager != null)
        {
            List<PlayerNetworkControllerBase> allUsers = sessionPlayersManager.GetAllUsers();
            Debug.Log($"Total users in session: {allUsers.Count}");
            foreach (var user in allUsers)
            {
                Debug.Log($"User ID: {user.UserId}");
            }

            if (allUsers.Count > 0)
            {
                var firstUser = allUsers[0];
                Debug.Log($"First User ID: {firstUser.UserId}");

                // Find the Synchronizer GameObject associated with the first user
                var synchronizer = GameObject.Find($"Player_{firstUser.UserId}/Synchronizer");

                if (synchronizer != null)
                {
                    var vrtSynchronizer = synchronizer.GetComponent<VRTSynchronizer>();
                    if (vrtSynchronizer != null)
                    {
                        // Set the values of the fields
                        vrtSynchronizer.requestAudioBehindMs = requestAudioBehindMs;
                        vrtSynchronizer.requestNonAudioBehindMs = requestNonAudioBehindMs;

                        Debug.Log("Updated VRTSynchronizer fields for the first user.");
                    }
                    else
                    {
                        Debug.LogWarning("VRTSynchronizer component not found on the Synchronizer GameObject.");
                    }
                }
                else
                {
                    Debug.LogWarning("Synchronizer GameObject not found for the first user.");
                }
            }
            else
            {
                Debug.LogWarning("No users found in the session.");
            }
        }
        else
        {
            Debug.LogWarning("SessionPlayersManager instance is not found.");
        }
    }
}


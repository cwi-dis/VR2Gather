using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VoicePlayer : MonoBehaviour {
    public VoiceReceiver    receiver { get; private set; }
    AudioSource             audioSource;
    AudioClip               audioClip;
    sub.connection          subHandle;

    bool isPlaying;

    public void Init(string streamName, string URL)
    {
        System.Threading.Thread.Sleep(2000);
        subHandle = sub.create(streamName);
        if (subHandle != null)
        {
            Debug.Log(">>> sub_create " + subHandle);
            isPlaying = subHandle.play(URL);
            Debug.Log(">>> sub_play " + isPlaying);
            if (isPlaying) {
                int count = subHandle.get_stream_count();
                Debug.Log(">>> sub_get_stream_count " + count);
            }
            else
                Debug.LogError("SUBD_ERROR: can't open URL " + URL);

        }
        else
            Debug.LogError("SUBD_ERROR: can't create streaming: " + streamName);

        Init();
    }

    public void Init()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        receiver = new VoiceReceiver();
        audioSource.clip = AudioClip.Create("clip0", BaseCodec.Instance.bufferLeght, 1, BaseCodec.Instance.playerFrequency, true, OnAudioRead);
        audioSource.loop = true;
        audioSource.Play();
    }


    byte[] data;
    System.IntPtr currentBufferPtr;

    void Update() {
        if (subHandle != null) {
            sub.FrameInfo info = new sub.FrameInfo();
            int bytesNeeded = subHandle.grab_frame(0, System.IntPtr.Zero, 0, ref info);
            if (bytesNeeded == 0)
                return;
            Debug.Log("DATA!!!!");

            if (data == null || bytesNeeded > data.Length) {
                Debug.Log("PCSUBReader: allocating more memory");
                data = new byte[bytesNeeded]; // Reserves 30% more.
                currentBufferPtr = System.Runtime.InteropServices.Marshal.UnsafeAddrOfPinnedArrayElement(data, 0);
            }

            int bytesRead = subHandle.grab_frame(0, currentBufferPtr, bytesNeeded, ref info);
            if (bytesRead != bytesNeeded) {
                Debug.LogError("PCSUBReader: sub_grab_frame returned " + bytesRead + " bytes after promising " + bytesNeeded);
                return;
            }
            ProcessData(data);

        }
    }


    NTPTools.NTPTime tempTime;
    void ProcessData(byte[] data)
    {
        int userID = data[0];
        tempTime.SetByteArray(data, 1);
        var lat = NTPTools.GetNTPTime().time - tempTime.time;
        SocketIOServer.player[userID].name = $"Player_{userID} Lat ({lat})";
        SocketIOServer.player[userID].receiver.ReceiveBuffer(BaseCodec.Instance.Uncompress(data, 1 + 8));
    }

    void OnAudioRead(float[] data) {
        receiver.GetBuffer(data, data.Length);
    }

}

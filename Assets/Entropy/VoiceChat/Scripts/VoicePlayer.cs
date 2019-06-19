using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VoicePlayer : MonoBehaviour {
    public VoiceReceiver    receiver { get; private set; }
    AudioSource             audioSource;
    AudioClip               audioClip;
    System.IntPtr           subHandle;

    bool isPlaying;

    public void Init(string streamName, string URL)
    {
        signals_unity_bridge_pinvoke.SetPaths();
        System.Threading.Thread.Sleep(2000);
        subHandle = signals_unity_bridge_pinvoke.sub_create(streamName);
        if (subHandle != System.IntPtr.Zero)
        {
            Debug.Log(">>> sub_create " + subHandle);
            isPlaying = signals_unity_bridge_pinvoke.sub_play(subHandle, URL);
            Debug.Log(">>> sub_play " + isPlaying);
            if (isPlaying) {
                int count = signals_unity_bridge_pinvoke.sub_get_stream_count(subHandle);
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
        if (subHandle != System.IntPtr.Zero) {
            signals_unity_bridge_pinvoke.FrameInfo info = new signals_unity_bridge_pinvoke.FrameInfo();
            int bytesNeeded = signals_unity_bridge_pinvoke.sub_grab_frame(subHandle, 0, System.IntPtr.Zero, 0, ref info);
            if (bytesNeeded == 0) return;

            if (data == null || bytesNeeded > data.Length) {
                Debug.Log("PCSUBReader: allocating more memory");
                data = new byte[bytesNeeded]; // Reserves 30% more.
                currentBufferPtr = System.Runtime.InteropServices.Marshal.UnsafeAddrOfPinnedArrayElement(data, 0);
            }

            int bytesRead = signals_unity_bridge_pinvoke.sub_grab_frame(subHandle, 0, currentBufferPtr, bytesNeeded, ref info);
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
        tempTime.T0 = data[1]; tempTime.T1 = data[2]; tempTime.T2 = data[3]; tempTime.T3 = data[4]; tempTime.T4 = data[5]; tempTime.T5 = data[6]; tempTime.T6 = data[7]; tempTime.T7 = data[8];
        var lat = NTPTools.GetNTPTime().time - tempTime.time;
        SocketIOServer.player[userID].name = $"Player_{userID} Lat ({lat})";
        SocketIOServer.player[userID].receiver.ReceiveBuffer(BaseCodec.Instance.Uncompress(data, 1 + 8));
    }

    void OnAudioRead(float[] data) {
        receiver.GetBuffer(data, data.Length);
    }

}

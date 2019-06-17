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


    byte[] currentBuffer;
    System.IntPtr currentBufferPtr;

    void Update() {
        if (subHandle != System.IntPtr.Zero) {
            signals_unity_bridge_pinvoke.FrameInfo info = new signals_unity_bridge_pinvoke.FrameInfo();
            int bytesNeeded = signals_unity_bridge_pinvoke.sub_grab_frame(subHandle, 0, System.IntPtr.Zero, 0, ref info);
            if (bytesNeeded == 0)
            {
                Debug.LogError($"No DaTA!! {bytesNeeded} ");
                return;
            }

            if (currentBuffer == null || bytesNeeded > currentBuffer.Length) {
                Debug.Log("PCSUBReader: allocating more memory");
                currentBuffer = new byte[bytesNeeded]; // Reserves 30% more.
                currentBufferPtr = System.Runtime.InteropServices.Marshal.UnsafeAddrOfPinnedArrayElement(currentBuffer, 0);
            }

            int bytesRead = signals_unity_bridge_pinvoke.sub_grab_frame(subHandle, 0, currentBufferPtr, bytesNeeded, ref info);
            if (bytesRead != bytesNeeded) {
                Debug.LogError("PCSUBReader: sub_grab_frame returned " + bytesRead + " bytes after promising " + bytesNeeded);
                return;
            }

            Debug.LogError($"DaTA!! {bytesRead} ");

        }
    }

    void OnAudioRead(float[] data) {
        receiver.GetBuffer(data, data.Length);
    }

}

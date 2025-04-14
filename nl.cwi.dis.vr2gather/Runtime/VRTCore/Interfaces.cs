using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cwipc;
using UnityEngine.XR.Interaction.Toolkit;

namespace VRT.Core
{
    using Timestamp = System.Int64;
    using Timedelta = System.Int64;

    /// <summary>
    /// Provide media objects (pointclouds, audio, video) to renderers or other consumers.
    /// This interface is provided by media stream readers and decoders and such.
    /// Base interface handles synchronization between mutiple media streams.
    /// </summary>
    public interface IPreparer : Cwipc.IPreparer
    {

    }

    /// <summary>
    /// Provide pointclouds to a renderer (or other consumer).
    /// </summary>
    public interface IPointCloudPreparer : Cwipc.IPointCloudPreparer
    {

    }

    public interface IAudioPreparer : IPreparer
    {
        /// <summary>
        /// Copy audio data to a c# float array. LatchFrame must be called first to lock the position.
        /// </summary>
        /// <param name="dst">Buffer</param>
        /// <param name="len">How many bytes to copy</param>
        /// <returns>How many bytes were unavailable (residual length)</returns>
        public int GetAudioBuffer(float[] dst, int len);
    }

    /// <summary>
    /// Provide Video frames and optional audio frames to a renderer.
    /// </summary>
    public interface IVideoPreparer : IAudioPreparer
    {
        public int availableVideo { get; }
        /// <summary>
        /// Return native pointer to video data. LatchFrame must be called first to lock the position.
        /// </summary>
        /// <param name="len">How many bytes are wanted</param>
        /// <returns>False if no data (or not enough data) was available</returns>
        public System.IntPtr GetVideoPointer(int len);


    }

    /// <summary>
    /// Interface to a grabbable object. Implemented by VRTGrabbableController and
    /// VRTFishnetGrabbable.
    /// </summary>
    public interface IVRTGrabbable
    {
        public GameObject gameObject { get; }
        public void OnSelectEnter(SelectEnterEventArgs args);
        public void OnSelectExit();

    }
}
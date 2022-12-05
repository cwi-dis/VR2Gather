using System.Collections.Generic;
using UnityEngine;

namespace Vive.Plugin.SR.Experience
{
    public enum AudioClipIndex
    {
        Slide,//rotator
        Click,//subbutn selection & panels
        Select_On,//select button
        Select_Off,
        EffectBall,
        Portal,
        Drag,
        FairyWalk,
        FairySit,
        MaxNum,
    }       

    public class ViveSR_Experience_SoundManager : MonoBehaviour
    {
        [SerializeField] bool isOn;
        [SerializeField] List<AudioClip> _AudioClips;
        Dictionary<AudioClipIndex, AudioClip> AudioClips = new Dictionary<AudioClipIndex, AudioClip>();
        Dictionary<AudioClipIndex, bool> IsPlaying = new Dictionary<AudioClipIndex, bool>();     // prevent overlap

        private void Awake()
        {
            if (!FindObjectOfType<AudioListener>()) ViveSR_Experience.instance.PlayerHeadCollision.AddComponent<AudioListener>();

            for (int i = 0; i < (int)AudioClipIndex.MaxNum; i++)
            {
                AudioClips[(AudioClipIndex)i] = _AudioClips[i];
                IsPlaying[(AudioClipIndex)i] = false;
            }
        }

        public void PlayAtAttachPoint(AudioClipIndex index)
        {
            if (isOn & !IsPlaying[index])
            {
                AudioSource.PlayClipAtPoint(AudioClips[index], ViveSR_Experience.instance.AttachPoint.transform.position);
                IsPlaying[index] = true;
                if (index == AudioClipIndex.Drag) this.Delay(() => { IsPlaying[index] = false; }, 0.1f);
                if (index == AudioClipIndex.FairyWalk) this.Delay(() => { IsPlaying[index] = false; }, 0.55f);
                else this.DelayOneFrame(() => { IsPlaying[index] = false; });
            }
        }
    }
}
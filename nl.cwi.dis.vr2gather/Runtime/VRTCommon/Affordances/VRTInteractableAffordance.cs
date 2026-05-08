using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace VRT.Pilots.Common
{
    /// <summary>
    /// Affordance component for interactable objects. Attach as a child GameObject
    /// alongside an AudioSource; the component finds the XRBaseInteractable in its parent.
    /// All clips are optional — assign only the events you want feedback for.
    /// </summary>
    public class VRTInteractableAffordance : MonoBehaviour
    {
        [Tooltip("AudioSource to use for playback. Defaults to AudioSource on this GameObject.")]
        public AudioSource audioSource;

        [Header("Audio Clips")]
        [Tooltip("Played when the first interactor starts hovering.")]
        public AudioClip hoverEnterClip;
        [Tooltip("Played when the last interactor stops hovering (and object is not grabbed).")]
        public AudioClip hoverExitClip;
        [Tooltip("Played when the object is grabbed/selected.")]
        public AudioClip selectEnterClip;
        [Tooltip("Played when the object is released.")]
        public AudioClip selectExitClip;

        XRBaseInteractable _interactable;

        void Awake()
        {
            _interactable = GetComponentInParent<XRBaseInteractable>();
            if (_interactable == null)
                Debug.LogWarning($"VRTInteractableAffordance({name}): no XRBaseInteractable found in parent hierarchy");
            if (audioSource == null)
                audioSource = GetComponent<AudioSource>();
        }

        void OnEnable()
        {
            if (_interactable == null) return;
            _interactable.firstHoverEntered.AddListener(OnFirstHoverEntered);
            _interactable.lastHoverExited.AddListener(OnLastHoverExited);
            _interactable.firstSelectEntered.AddListener(OnFirstSelectEntered);
            _interactable.lastSelectExited.AddListener(OnLastSelectExited);
        }

        void OnDisable()
        {
            if (_interactable == null) return;
            _interactable.firstHoverEntered.RemoveListener(OnFirstHoverEntered);
            _interactable.lastHoverExited.RemoveListener(OnLastHoverExited);
            _interactable.firstSelectEntered.RemoveListener(OnFirstSelectEntered);
            _interactable.lastSelectExited.RemoveListener(OnLastSelectExited);
        }

        void OnFirstHoverEntered(HoverEnterEventArgs args) => Play(hoverEnterClip);
        void OnLastHoverExited(HoverExitEventArgs args) => Play(hoverExitClip);
        void OnFirstSelectEntered(SelectEnterEventArgs args) => Play(selectEnterClip);
        void OnLastSelectExited(SelectExitEventArgs args) => Play(selectExitClip);

        void Play(AudioClip clip)
        {
            if (clip == null || audioSource == null) return;
            audioSource.PlayOneShot(clip);
        }
    }
}

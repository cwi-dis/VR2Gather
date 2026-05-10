using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace VRT.Pilots.Common
{
    /// <summary>
    /// Affordance component for interactable objects. Attach as a child GameObject
    /// alongside an AudioSource and (optionally) a SpriteRenderer.
    /// Finds the XRBaseInteractable in its parent hierarchy.
    /// All audio clips and sprites are optional.
    /// </summary>
    public class VRTInteractableAffordance : MonoBehaviour
    {
        public enum IconType { Auto, Grab, Press }

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

        [Header("Visual Icon")]
        [Tooltip("SpriteRenderer to show/hide on hover. Defaults to SpriteRenderer on this GameObject.")]
        public SpriteRenderer iconRenderer;
        [Tooltip("Sprite to show when the interactable is a grab target.")]
        public Sprite grabSprite;
        [Tooltip("Sprite to show when the interactable is a press/trigger target.")]
        public Sprite pressSprite;
        [Tooltip("Auto: detect from parent interactable type. Override to force a specific icon.")]
        public IconType iconType = IconType.Auto;
        [Tooltip("Icon size as a fraction of the parent collider's bounding box diagonal. 0 = no auto-scaling.")]
        public float iconScaleFactor = 1f;
        [Tooltip("Seconds to fade the icon in on hover enter.")]
        public float fadeInDuration = 0.15f;
        [Tooltip("Seconds to fade the icon out on hover exit.")]
        public float fadeOutDuration = 0.25f;

        XRBaseInteractable _interactable;
        Camera _camera;
        Coroutine _fadeCoroutine;

        void Awake()
        {
            _interactable = GetComponentInParent<XRBaseInteractable>();
            if (_interactable == null)
                Debug.LogWarning($"VRTInteractableAffordance({name}): no XRBaseInteractable found in parent hierarchy");

            if (audioSource == null)
                audioSource = GetComponent<AudioSource>();

            if (iconRenderer == null)
                iconRenderer = GetComponent<SpriteRenderer>();

            if (iconRenderer != null)
            {
                // Pick sprite: explicit override wins; otherwise detect from interactable type
                Sprite chosen = null;
                IconType resolved = iconType;
                if (resolved == IconType.Auto)
                    resolved = (_interactable is XRGrabInteractable) ? IconType.Grab : IconType.Press;

                chosen = (resolved == IconType.Grab) ? grabSprite : pressSprite;

                if (chosen != null)
                    iconRenderer.sprite = chosen;

                // Start hidden
                SetAlpha(0f);

                // Auto-scale icon relative to the parent collider's world-space size
                if (iconScaleFactor > 0f)
                {
                    Collider col = _interactable != null ? _interactable.GetComponent<Collider>() : null;
                    if (col == null && _interactable != null)
                        col = _interactable.GetComponentInChildren<Collider>();
                    float boundsSize = col != null ? col.bounds.size.magnitude : 0f;
                    if (boundsSize > 0.001f)
                    {
                        float worldSize = boundsSize * iconScaleFactor;
                        float parentScale = transform.parent != null ? transform.parent.lossyScale.x : 1f;
                        transform.localScale = Vector3.one * (worldSize / parentScale);
                    }
                    // If bounds are zero (physics not yet initialized) or no collider found,
                    // leave the prefab scale unchanged rather than zeroing it out.
                }
            }
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

        void Update()
        {
            if (iconRenderer == null || iconRenderer.color.a <= 0f) return;
            if (_camera == null)
                _camera = Camera.main;
            if (_camera != null)
                transform.LookAt(_camera.transform.position);
        }

        void OnFirstHoverEntered(HoverEnterEventArgs args)
        {
            Play(hoverEnterClip);
            FadeTo(1f, fadeInDuration);
        }

        void OnLastHoverExited(HoverExitEventArgs args)
        {
            Play(hoverExitClip);
            FadeTo(0f, fadeOutDuration);
        }

        void OnFirstSelectEntered(SelectEnterEventArgs args) => Play(selectEnterClip);
        void OnLastSelectExited(SelectExitEventArgs args) => Play(selectExitClip);

        void Play(AudioClip clip)
        {
            if (clip == null || audioSource == null) return;
            audioSource.PlayOneShot(clip);
        }

        void FadeTo(float targetAlpha, float duration)
        {
            if (iconRenderer == null) return;
            if (_fadeCoroutine != null)
                StopCoroutine(_fadeCoroutine);
            _fadeCoroutine = StartCoroutine(FadeCoroutine(targetAlpha, duration));
        }

        IEnumerator FadeCoroutine(float targetAlpha, float duration)
        {
            float startAlpha = iconRenderer.color.a;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                SetAlpha(Mathf.Lerp(startAlpha, targetAlpha, elapsed / duration));
                yield return null;
            }
            SetAlpha(targetAlpha);
            _fadeCoroutine = null;
        }

        void SetAlpha(float alpha)
        {
            if (iconRenderer == null) return;
            Color c = iconRenderer.color;
            c.a = alpha;
            iconRenderer.color = c;
        }
    }
}

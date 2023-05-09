using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace VRT.Pilots.Common
{
    /// <summary>
    /// Use this component in a scene to fade-in and fade-out the scene.
    /// </summary>
    public class CameraFader : MonoBehaviour
    {
        [Tooltip("How many seconds the fadein/fadeout takes")]
        public float FadeDuration = 1.0f;
        [Tooltip("Fade GameObject (disabled when not fading in/out)")]
        public GameObject FadeGO;
        [Tooltip("Image in FadeGO, its material will be cloned and animated for fading")]
        public Image FadeImage;
        private Text FadeText;

        [Tooltip("If true this scene fades in from black (otherwise it only fades out to black)")]
        public bool startFadedOut = false;
        private bool fadeInStarted = false;
        private Material _FadeMaterial;
        private bool _Fading = false;
        private float _Target;
        private float _Value;
        private float _Step;

        private static CameraFader _Instance;
        public static CameraFader Instance
        {
            get
            {
                if (_Instance == null)
                {
                    _Instance = FindObjectOfType<CameraFader>();
                }

                return _Instance;
            }
        }

        public void Awake()
        {
            PilotController ctrl = FindObjectOfType<PilotController>();
            if (ctrl != null)
            {
                startFadedOut = ctrl.startFadedOut;
            }
            _FadeMaterial = Instantiate(FadeImage.material);
            FadeImage.material = _FadeMaterial;
            _InitFade();

        }

        private void Start()
        {
            StartFadeIn();
        }

        void _InitFade()
        {
            if (startFadedOut)
            {
                _FadeMaterial.color = Color.black;
                _Value = 1f;
                if (FadeGO != null) FadeGO.SetActive(true);
            }
            else
            {
                _FadeMaterial.color = new Color(0f, 0f, 0f, 0f);
                _Value = 0f;
                if (FadeGO != null) FadeGO.SetActive(false);
            }
        }

        public void SetText(string text)
        {
            if (FadeText != null) FadeText.text = text;
        }

        public void ClearText()
        {
            if (FadeText != null) FadeText.text = "";
        }

        public IEnumerator FadeIn()
        {
            _InitFade();
            yield return null;

            ClearText();
            _Fading = true;
            _Target = 0.0f;
            _Step = -1.0f / FadeDuration;

            _Value = _FadeMaterial.color.a;

            while (_Fading)
            {
                _Value += Time.deltaTime * _Step;
                _Value = Mathf.Clamp01(_Value);

                _FadeMaterial.color = new Color(0f, 0f, 0f, _Value);
                if (_Value == _Target)
                {
                    _Fading = false;
                    if (FadeGO != null) FadeGO.SetActive(false);
                }

                yield return null;
            }
        }

        public IEnumerator FadeOut()
        {
            _Fading = true;
            FadeGO?.SetActive(true);
            _Target = 1.0f;
            _Step = 1.0f / FadeDuration;

            _Value = _FadeMaterial.color.a;

            while (_Fading)
            {
                _Value += Time.deltaTime * _Step;
                _Value = Mathf.Clamp01(_Value);
                _FadeMaterial.color = new Color(0f, 0f, 0f, _Value);

                if (_Value == _Target)
                {
                    _Fading = false;
                    // if (FadeGO != null) FadeGO.SetActive(false);
                }

                yield return null;
            }
        }

        public void StartFadeIn()
        {
            if (!startFadedOut || fadeInStarted) return;
            fadeInStarted = true;
            StartCoroutine(FadeIn());
        }
    }
}

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
        public Image FadeImage;
        public Text FadeText;

        [Tooltip("If true this scene fades in from black (otherwise it only fades out to black)")]
        public bool StartFadedOut = false;

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
            _FadeMaterial = Instantiate(FadeImage.material);
            FadeImage.material = _FadeMaterial;
            _InitFade();

        }

        void _InitFade()
        {
            if (StartFadedOut)
            {
                _FadeMaterial.color = Color.black;
                _Value = 1f;
            }
            else
            {
                _FadeMaterial.color = new Color(0f, 0f, 0f, 0f);
                _Value = 0f;
            }
        }

        public void SetText(string text)
        {
            FadeText.text = text;
        }

        public void ClearText()
        {
            FadeText.text = "";
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

                if (_Value == _Target)
                {
                    _Fading = false;
                }

                _FadeMaterial.color = new Color(0f, 0f, 0f, _Value);
                yield return null;
            }
        }

        public IEnumerator FadeOut()
        {
            _Fading = true;
            _Target = 1.0f;
            _Step = 1.0f / FadeDuration;

            _Value = _FadeMaterial.color.a;

            while (_Fading)
            {
                _Value += Time.deltaTime * _Step;
                _Value = Mathf.Clamp01(_Value);

                if (_Value == _Target)
                {
                    _Fading = false;
                }

                _FadeMaterial.color = new Color(0f, 0f, 0f, _Value);
                yield return null;
            }
        }
    }
}

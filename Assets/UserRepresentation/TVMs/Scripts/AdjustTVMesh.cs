using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRT.UserRepresentation.TVM
{
    public class AdjustTVMesh : MonoBehaviour
    {
        private bool _calibEnabled = false;
        private float _rotationStep = 5f, _rotationSlightStep = 1f;


        public float TranslationStep;

        public bool CalibEnabled
        {
            get
            {
                return _calibEnabled;
            }

            set
            {
                _calibEnabled = value;
            }
        }

        // Use this for initialization
        void Start()
        {
            transform.position = new Vector3(
                                PlayerPrefs.GetFloat("x_pos"),
                                PlayerPrefs.GetFloat("y_pos"),
                                PlayerPrefs.GetFloat("z_pos")
                                );

            transform.rotation = Quaternion.Euler(
                                PlayerPrefs.GetFloat("x"),
                                PlayerPrefs.GetFloat("y"),
                                PlayerPrefs.GetFloat("z")
                                );
        }

        // Update is called once per frame
        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Keypad0))
            {
                _calibEnabled = !_calibEnabled;

                if (!_calibEnabled)
                {
                    var pos = transform.position;

                    PlayerPrefs.SetFloat("x_pos", pos.x);
                    PlayerPrefs.SetFloat("y_pos", pos.y);
                    PlayerPrefs.SetFloat("z_pos", pos.z);

                    var rot = transform.rotation.eulerAngles;

                    PlayerPrefs.SetFloat("x", rot.x);
                    PlayerPrefs.SetFloat("y", rot.y);
                    PlayerPrefs.SetFloat("z", rot.z);
                }
            }


            if (_calibEnabled)
            {
                if (Input.GetKeyDown(KeyCode.Keypad4))
                {
                    transform.Rotate(Vector3.up, -_rotationStep);
                }
                else if (Input.GetKeyDown(KeyCode.Keypad6))
                {
                    transform.Rotate(Vector3.up, _rotationStep);

                }
                else if (Input.GetKeyDown(KeyCode.Keypad1))
                {
                    transform.Rotate(Vector3.up, -_rotationSlightStep);
                }
                else if (Input.GetKeyDown(KeyCode.Keypad3))
                {
                    transform.Rotate(Vector3.up, _rotationSlightStep);

                }

                if (Input.GetKeyDown(KeyCode.UpArrow))
                {
                    transform.Translate(new Vector3(0, 0, TranslationStep));
                }
                else if (Input.GetKeyDown(KeyCode.DownArrow))
                {
                    transform.Translate(new Vector3(0, 0, -TranslationStep));

                }
                else if (Input.GetKeyDown(KeyCode.LeftArrow))
                {
                    transform.Translate(new Vector3(-TranslationStep, 0, 0));
                }
                else if (Input.GetKeyDown(KeyCode.RightArrow))
                {
                    transform.Translate(new Vector3(TranslationStep, 0, 0));
                }
                else if (Input.GetKeyDown(KeyCode.KeypadMinus))
                {
                    transform.Translate(new Vector3(0, TranslationStep, 0));
                }
                else if (Input.GetKeyDown(KeyCode.KeypadPlus))
                {
                    transform.Translate(new Vector3(0, -TranslationStep, 0));
                }
            }
        }
    }
}
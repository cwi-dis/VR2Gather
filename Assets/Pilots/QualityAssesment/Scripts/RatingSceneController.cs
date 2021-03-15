using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using VRT.Core;
using VRT.UserRepresentation.PointCloud;

namespace QualityAssesment
{
    public class RatingSceneController : MonoBehaviour
    {
        private bool ratingRegistered = false;
        private bool camFound = false;
        private int ratingValue;
        //xxxshishir add rating variables and logging info here

        string Name()
        {
            return "RatingSceneController";
        }
        public void registerScore(int rating)
        {
            ratingValue = rating;
            Debug.Log("<color=green> Rating Registered:  </color>" + ratingValue);
            ratingRegistered = true;
            string statMsg = $"currentstimuli={StimuliController.getCurrentStimulus()}, RatingReceived={rating}";
            BaseStats.Output(Name(), statMsg);
        }
        private void Update()
        {
            float rightTrigger = Input.GetAxisRaw("PrimaryTriggerRight");
            float leftTrigger = Input.GetAxisRaw("PrimaryTriggerLeft");
            if (Input.GetKeyDown(KeyCode.Escape) || ratingRegistered==true)
            {
                StimuliController.loadnext();
                SceneManager.LoadScene("QualityAssesment");
            }
            if (camFound==false)
            {
                var cam = GameObject.FindWithTag("MainCamera").GetComponent<Camera>();
                Canvas ratingCanvas = GameObject.FindWithTag("MainRatingCanvas").GetComponent<Canvas>();
                ratingCanvas.worldCamera = cam;
                if (cam != null && ratingCanvas != null)
                {
                    camFound = true;
                }
                var legacyFadeCanvas = GameObject.Find("CameraFadeCanvas");
                legacyFadeCanvas.SetActive(false);
            }
        }
    }
}

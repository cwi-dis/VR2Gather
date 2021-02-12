using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Assets.Pilots.QualityAssesment.Scripts
{
    public class RatingSceneController : MonoBehaviour
    {
        private bool ratingRegistered = false;
        private bool camFound = false;
        private int ratingValue;
        //xxxshishir add rating variables and logging info here
        public void registerScore(int rating)
        {
            ratingValue = rating;
            Debug.Log("<color=green> Rating Registered:  </color>" + ratingValue);
            ratingRegistered = true;
        }
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape) || ratingRegistered==true)
            {
                SceneManager.LoadScene("QualityAssesment");
            }
            if (camFound==false)
            {
                var cam = FindObjectOfType<Camera>();
                Canvas ratingCanvas = FindObjectOfType<Canvas>();
                ratingCanvas.worldCamera = cam;
                camFound = true;
            }
        }
    }
}

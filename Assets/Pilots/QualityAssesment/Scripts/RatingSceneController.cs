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

        //xxxshishir add rating variables and logging info here

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                SceneManager.LoadScene("QualityAssesment");
            }
        }
    }
}

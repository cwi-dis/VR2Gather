//This script allows the player to simulate have a Vr HMD by movng the camera with the mouse
//and the position with the keyboard
using UnityEngine;
using UnityEngine.Video;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using Valve.VR;
//using cakeslice;





namespace Interactive360.Utils
{
    public class CameraEditorControlVoting : MonoBehaviour
    {
        [SerializeField] protected bool mouseControl = true;  //Should the mouse control the camera? Use this to easily disable this script
        [SerializeField] protected float camSpeed = 30f;      //The speed the camera rotates
        [SerializeField] protected float movementSpeed = 3f;  //The speed the player moves
        [SerializeField] protected uint numberofdisplays = 3;  //The speed the player moves
        List<bool> flags = new List<bool>();
		public delegate void returnflag(List<bool> tosend);
		public static event returnflag onreturnflag; 
		GameObject[] cubosarray;
		bool modofullscreen;
        string seleccion = "Start";
        string adentrarse = "Jump";
        int nquestions;
        int[] nquestionsinquest;
        string TAG;
        Transform vrCamera;
        Camera Rulecamera;
        GameObject[] Displays;
        SteamVR_Action_Boolean clicButton = SteamVR_Actions.default_GrabPinch;
        

        /*public void LaunchApp(string uri)
        {
            string bundleId = "com.bell_labs.drs360player"; // your target bundle id

            AndroidJavaClass up = new AndroidJavaClass("com.unity3d.player.UnityPlayer");

            AndroidJavaObject ca = up.GetStatic<AndroidJavaObject>("currentActivity");

            AndroidJavaObject packageManager = ca.Call<AndroidJavaObject>("getPackageManager");



            //if the app is installed, no errors. Else, doesn't get past next line

            AndroidJavaObject launchIntent = packageManager.Call<AndroidJavaObject>("getLaunchIntentForPackage", bundleId);
            
            launchIntent.Call<AndroidJavaObject>("putExtra","URI", uri);


            ca.Call("startActivity", launchIntent);



            up.Dispose();

            ca.Dispose();

            packageManager.Dispose();

            launchIntent.Dispose();
        }*/
        string ChangeScene(int ncuestion,int nquestion_2)
        {
            string cuestionario = GameObject.Find("ExperimentController").GetComponent<Randomizer>().secuencias[0].post_seq_questions[ncuestion];
            string escala = "acr";
            string[] puntuaciones = null;
            string tag= "";
            foreach (Cuestionarios item in GameObject.Find("ExperimentController").GetComponent<Randomizer>().cuestionarios)
            {
                if (item.name == cuestionario)
                {
                    GameObject.Find("Question").GetComponentInChildren<TextMesh>().text = item.items[nquestion_2].text;
                    escala = item.items[nquestion_2].scale;
                    tag = item.items[nquestion_2].tag;
                }
            }
            foreach (Escalas item in GameObject.Find("ExperimentController").GetComponent<Randomizer>().escalas)
            {
                if (item.name == escala) { puntuaciones = item.scores; }
            }
            
            foreach (GameObject cubo in cubosarray)
            {
                flags.Add(false);
                if (cubo.GetComponent<onoff>().isdisplay && (puntuaciones != null)) cubo.GetComponentInChildren<TextMesh>().text = puntuaciones[(int)cubo.GetComponent<GetCloser>().points - 1];
            }
            return tag;
        }

        public 
       	void Awake()
        {

            cubosarray = GameObject.FindGameObjectsWithTag("Pointable");


            modofullscreen = false;

            

            //vrCamera = GetComponentInChildren<Camera>().transform;


            TAG=ChangeScene(0,0);

            //GameObject.Find("Mobiliario").transform.Rotate(0, GameObject.Find("Camera").transform.eulerAngles.y, 0);
            nquestions = GameObject.Find("ExperimentController").GetComponent<Randomizer>().secuencias[0].post_seq_questions.Length;
            nquestionsinquest = new int[nquestions];
            for (int i = 0; i < nquestions; i++)
            {
                foreach (Cuestionarios item in GameObject.Find("ExperimentController").GetComponent<Randomizer>().cuestionarios)
                {
                    if (item.name == GameObject.Find("ExperimentController").GetComponent<Randomizer>().secuencias[0].post_seq_questions[i])
                    {
                        nquestionsinquest[i] = item.items.Length;
                    }
                }
                
            }
            Rulecamera = GameObject.Find("TrackedCamera (Left)").GetComponentInChildren<Camera>();
            StartCoroutine(ManageMovement());
          
        }
       
        
        //Detect mouse movements and move camera accordingly
        IEnumerator ManageMovement()
        {
            int nquestion = 0;
            int nquestion_2 = 0;
            string buffer = "";
        	GameObject selected = null;
            Debug.Log(Rulecamera.transform.eulerAngles.y);
            yield return new WaitForSeconds(0.3f);
            //GameObject.Find("CanvasHolder").transform.Translate(Rulecamera.transform.position.x, 0, Rulecamera.transform.position.z, Space.World);
            //GameObject.Find("CanvasHolder").transform.Rotate(0, Rulecamera.transform.eulerAngles.y, 0,Space.World);
            
            while (true)
            {
                
                if (/*GvrControllerInput.GetDevice(GvrControllerHand.Right).GetButtonDown(GvrControllerButton.TouchPadButton) ||*/ Input.GetKeyDown("space") || clicButton.GetStateDown(SteamVR_Input_Sources.Any))//&& modofullscreen == false)
                {

                    while (!Input.GetKeyDown("space") && !clicButton.GetStateDown(SteamVR_Input_Sources.Any)) // !Input.GetButtonUp("Button1") // !GvrControllerInput.GetDevice(GvrControllerHand.Right).GetButtonUp(GvrControllerButton.TouchPadButton) ||
                    { 
                        Debug.Log("Button pressed, stuck on a deadloop??"); 
                        yield return null; 
                    }

                    Debug.Log(selected.ToString());
                    GameObject ExperimentController = GameObject.Find("ExperimentController");
                    using (StreamWriter sw = File.AppendText(ExperimentController.GetComponent<Randomizer>().urlfile))
                    {
                        string videofile = ExperimentController.GetComponent<Randomizer>().playlist[0];
                        string segment = ExperimentController.GetComponent<Randomizer>().segment.ToString();
                         
                        sw.WriteLine((DateTime.Now.TimeOfDay.TotalMilliseconds * 1000000).ToString() + ","+segment+"," + videofile  + ","+ "questionnaire,"+ TAG + ","+ selected.GetComponent<GetCloser>().points.ToString() );
                       
                    }
                    Debug.Log(TAG);
                    if (nquestion_2 + 1 < nquestionsinquest[nquestion])
                    {
                        nquestion_2++;

                    }
                    else
                    {
                        nquestion++;
                        nquestions--;
                        nquestion_2 = 0;
                    }


                    if (nquestions == 0)
                    {
                        GameObject.Find("ExperimentController").GetComponent<Randomizer>().playlist.RemoveAt(0);
                        GameObject.Find("ExperimentController").GetComponent<Randomizer>().secuencias.RemoveAt(0);
                        if (GameObject.Find("ExperimentController").GetComponent<Randomizer>().playlist.Count == 0)
                        {

                            Application.Quit();

                        }
                        ExperimentController.GetComponent<Randomizer>().segment++;
                        AsyncOperation asyncLoad = SceneManager.UnloadSceneAsync("escena_votar_CWI"); //SceneManager.LoadSceneAsync(GameObject.Find("ExperimentController").GetComponent<Randomizer>().ExperimentScene);
                        while (!asyncLoad.isDone)
                        {
                            yield return null;
                        }
                    }
                    else
                    {
                        

                        TAG = ChangeScene(nquestion,nquestion_2);
                    }
                    
                    
                    
                    /*var scene = SceneManager.GetSceneByName("Video");
                    GameObject.Find("360Video").GetComponent<VideoPlayer>().Stop();
                    //GameObject.Find("360Video").GetComponent<VideoPlayer>().Play();
                    SceneManager.SetActiveScene(scene);
                    SceneManager.UnloadSceneAsync("escena_votar");*/


                }
                /*if (Input.GetButtonDown(seleccion) && modofullscreen == true)
                {
                    if(modofullscreen) selected.GetComponent<GetCloser>().Stopmotion();
                    while (!Input.GetButtonUp(seleccion))
                        yield return null;
                    modofullscreen = false;

            	}*/
                if (!modofullscreen){
                    if(cubosarray[0] != null) selected = selected_display(cubosarray);


                    //float moveLR = Input.GetAxis("Horizontal") * movementSpeed * Time.deltaTime;
                //float moveFB = Input.GetAxis("Vertical") * movementSpeed * Time.deltaTime*-1;
                //transform.Translate(moveLR, 0f, moveFB);

                    if (selected != null)
                    {
                        foreach (GameObject cubo in cubosarray)
                        {
                            if (cubo == selected) {
                                buffer = selected.GetComponent<GetCloser>().Description;
                                selected.GetComponentInChildren<onoff>().Highlight(true);
                                if (buffer.Length > 30)
                                {
                                    for (int i = 30; i < buffer.Length; i += 31) buffer = buffer.Insert(i - 1, "\n");
                                }
                             }
                            else cubo.GetComponentInChildren<onoff>().Highlight(false);  //cubo.GetComponentInChildren<cakeslice.Outline>().OnDisable(); 
                        }
                    }
                    

                if (Input.GetButtonDown("Cancel"))
                    UnlockCursor();
                    
                
            }else{
                    if (Input.GetButtonDown(adentrarse))
                    {
                        if (isDisplay(selected) == false)
                        {
                            selected.GetComponent<GetCloser>().Stopmotion();
                            modofullscreen = false;
                            for (int i = 1; i < Displays.Length; i++)
                            {
                                Destroy(Displays[i]);
                            }
                            yield return null;

                            //Displays = GameObject.Find("Mobiliario").GetComponent<bookcreator>().generatedisplays(selected.GetComponent<GetCloser>().URL, selected.GetComponent<GetCloser>().Description, selected.GetComponent<GetCloser>().URL360);
                            cubosarray = GameObject.FindGameObjectsWithTag("Pointable");
                            
                            
                        }
                        else
                        {
                            while (!Input.GetButtonUp(adentrarse))
                                yield return null;
                           // LaunchApp(selected.GetComponent<GetCloser>().URL);

                        }
                        
                    }

                }
                yield return null;
            }
        }
        bool isDisplay(GameObject target)
        {
            bool displayCheck = false;
            foreach (GameObject Display in Displays){
                if (target == Display) displayCheck = true;
            }
            return displayCheck;
        }
        GameObject selected_display(GameObject[] cubos){
        	Vector3 number = Rulecamera.WorldToViewportPoint(cubos[0].transform.position);
        	GameObject number2 = null; 
        	Vector2 center = new Vector2(0.5f,0.5f);// WorldToViewportPoint returns normalized values in x and y axes from 0 to 1
        	float dist = 100000;
        	foreach(GameObject cubo in cubos){
        		number = Rulecamera.WorldToViewportPoint(cubo.transform.position);
        		if (number.z > 0){// If the object is behind the camera a negative value will be returned in z
        			if(Vector2.Distance(center,new Vector2(number.x,number.y))<dist){
        				dist = Vector2.Distance(center,new Vector2(number.x,number.y));
        				number2 = cubo;
        			}
        		}
        	}
        	return number2;
        }
        void LockCursor()
        {
            //Lock the cursor to the middle of the screen and then hide it
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        
        void UnlockCursor()
        {
            //Release the cursor and show it
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}

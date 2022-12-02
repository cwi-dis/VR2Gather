using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;
using UnityEngine.InputSystem;


public class WorldSwapper : MonoBehaviour
    {
    // Start is called before the first frame update

    //public ;
    SteamVR_Action_Boolean gripbutton = SteamVR_Actions.srinput_grip;
    public float alphavalue = 0f;

     //we should modify all these things to rely on delegates.

    Shader ImagePlaneShader;
    int IsVirtualWorld = 0;

        void SwichWorld(float Value)
    {
        Shader.SetGlobalFloat("_Distance_TH", Value);
    }

    void Start()
        {
        ImagePlaneShader = Shader.Find("Custom/ChromaKeyRemove");
        Shader.SetGlobalFloat("_Distance_TH", IsVirtualWorld); //starts in real world
    }

        // Update is called once per frame
        void Update()
        {


        

        if (gripbutton.GetStateDown(SteamVR_Input_Sources.Any) || Keyboard.current[Key.F3].wasPressedThisFrame) //GetStateDown takes as input argument the source (Righthand, Camera, Head, or whatever... )
        {
            Debug.Log(IsVirtualWorld.ToString());
            IsVirtualWorld = (IsVirtualWorld+1)%2;
            SwichWorld((float)IsVirtualWorld);
            //Shader.SetGlobalFloat("_Alphavalue", alphavalue);
        }
        }




    }

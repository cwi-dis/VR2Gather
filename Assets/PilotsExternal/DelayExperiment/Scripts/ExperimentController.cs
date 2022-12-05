using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.IO;
using VRT.Core;
using VRT.Pilots.Common;
using VRT.UserRepresentation.PointCloud;
using VRT.Orchestrator.Wrapping;

public class ExperimentController : MonoBehaviour
{
    public float ExperimentDuration;
    public string State;
    TimersControllPanel timer;
    Randomizer playlist;
    public string VotationScene;
    int nConditions; //number of conditions in the experiment
    public GameObject CameraRendererRight;
    public GameObject CameraRight;
    public Shader shaderAuxSave;
    public Shader Shaderclean;
    public Dictionary<string, GameObject> ToDisable; // things to set to disable (right camera renderer and canvas, VRTogether main camera and self-poitcloudView)
    public Vector3 PCTranslationoffset;
    public Vector3 PCRotationoffset;
    public RenderTexture MaskTexture;
    public Material leftCameraMaterial;
    public OrchestratorController OrchControllerDelayExp;
    public string nameJSON;
    // Start is called before the first frame update

    void SwitchToSceneVotation()
    {
        //This method will try to load the scene SceneVotation in additive mode
        // Should check if the Scene is in the build list - https://docs.unity3d.com/ScriptReference/SceneManagement.Scene-buildIndex.html
        if (ToDisable.ContainsKey("OuterPlayer"))
        {
            ToDisable["OuterPlayer"].GetComponentInChildren<PointBufferRenderer>().material.shader = Shaderclean;
            ToDisable["OuterPlayer"].GetComponentInChildren<AudioSource>().mute = true;
        }
        SceneManager.LoadScene(VotationScene, LoadSceneMode.Additive);
        State = "Voting";

    }

    void SetExperienceDuration() // This void will be called for set a max duration of the experience
    {
        timer.setTimer(playlist.secuencias[0].duration);
    }



    void SetNewCondition(Secuencias sequence)
    {
        //timer.setTimer(sequence.duration); //timeout for the current experience
        State = "Sync";
        //here should be a method related to the experiment conditions
        if (ToDisable.ContainsKey("OuterPlayer"))
        {
            try
            {
                ToDisable["OuterPlayer"].GetComponentInChildren<AudioSource>().mute = false;
                ToDisable["OuterPlayer"].GetComponentInChildren<PointBufferRenderer>().material.shader = shaderAuxSave;
                ToDisable["OuterPlayer"].GetComponentInChildren<Synchronizer>().minPreferredLatency = (long)sequence.retardo_numerico;
            }
            catch (System.Exception)
            {

                Debug.Log("There were errors trying to stablish the new condition");
            }


        }

    }

    private void Awake()
    {
        this.gameObject.AddComponent<TimersControllPanel>(); //we put this in the Awake, because other scrips may require this components in their start voids.
        this.gameObject.AddComponent<Randomizer>();


        playlist = GetComponent<Randomizer>();
        playlist.Create_Playlist(new StreamReader("Assets/PilotsExternal/DelayExperiment/DelayExperiment/" + nameJSON));
    }
    void Start()
    {
        ToDisable = new Dictionary<string, GameObject>();
        //timer = GetComponent<TimersControllPanel>();
        //timer.OnTimerEnd += SwitchToSceneVotation;
        SetNewCondition(playlist.secuencias[0]);
        State = "Session";
        OrchControllerDelayExp = GameObject.Find("OrchestratorController").GetComponent<OrchestratorController>();
        nConditions = playlist.secuencias.Count;


        OrchestratorController.Instance.OnUserMessageReceivedEvent += OnMessageReceived;



    }
    private void findLocalUser()
    {
        try
        {
            GameObject AuxVar = GameObject.Find("Pilot0Controller").GetComponent<SessionPlayersManager>().AllUsers.Find(isLocal).gameObject;

            AuxVar.GetComponentInChildren<Camera>().enabled = false;
            AuxVar.GetComponentInChildren<PointBufferRenderer>().material.shader = Shaderclean;
            ToDisable.Add("SelfPlayer", AuxVar);

        }
        catch (System.Exception)
        {

            Debug.Log("Error Finding the self player, maybe next try...");
        }

    }
    private void OnDestroy()
    {

        OrchestratorController.Instance.OnUserMessageReceivedEvent -= OnMessageReceived;
    }
    private void OnMessageReceived(UserMessage userMessage)
    {
        if (userMessage.message.Substring(0, 7) == "ENDCON_")
        {
            Debug.Log("Experience Finished, Voting time!");
            SwitchToSceneVotation();
            State = "Voting";
        }
        else if ((userMessage.message.Substring(0, 7) == "ENDVOT_"))
        {
            
                nConditions = playlist.secuencias.Count;
                OrchControllerDelayExp.SendMessageToAll("NEWCON_");

        }
        else if ((userMessage.message.Substring(0, 7) == "NEWCON_"))
                {
                    SetNewCondition(playlist.secuencias[0]);

                }
            }

        
    
    private void findOuterUser()
    {
        try
        {
            GameObject AuxVar = GameObject.Find("Pilot0Controller").GetComponent<SessionPlayersManager>().AllUsers.Find(isnotLocal).gameObject;

            AuxVar.GetComponentInChildren<PointBufferRenderer>().transform.localRotation = Quaternion.Euler(PCRotationoffset);
            AuxVar.GetComponentInChildren<PointBufferRenderer>().transform.localPosition = PCTranslationoffset;
            ToDisable.Add("OuterPlayer", AuxVar);
            ToDisable["OuterPlayer"].GetComponentInChildren<Synchronizer>().minPreferredLatency = (long)playlist.secuencias[0].retardo_numerico;
            ToDisable["OuterPlayer"].GetComponentInChildren<Synchronizer>().latencyCatchup = 100;

        }
        catch (System.Exception)
        {

            Debug.Log("Error Finding the outer player, maybe next try...");
        }

    }
    private void FindRcamera()
    {
    }
    private void FindRcameraRenderer()
    {

    }
    private void findPointCloud()
    {

    }

    private static bool isLocal(VRT.Pilots.Common.NetworkPlayer NPlayer)
    {

        return (NPlayer.IsLocalPlayer);
    }

    private static bool isnotLocal(VRT.Pilots.Common.NetworkPlayer NPlayer)
    {

        return (!NPlayer.IsLocalPlayer);
    }



    // Update is called once per frame
    void Update()
    {
        if (ToDisable.ContainsKey("OuterPlayer") != true) findOuterUser();
        if (ToDisable.ContainsKey("SelfPlayer") != true) findLocalUser();
        else
        {
            CameraRendererRight.SetActive(false);
            CameraRight.SetActive(false);

        }

        leftCameraMaterial.SetTexture("_MainTex2", MaskTexture);
        if (Input.GetKeyDown("f1"))
        {
            OrchControllerDelayExp.SendMessageToAll("ENDCON_");
            //SwitchToSceneVotation();START_

        }
        if (Input.GetKeyDown("f2") )
        {
            if(OrchControllerDelayExp == null) OrchControllerDelayExp = GameObject.Find("OrchestratorController").GetComponent<OrchestratorController>();
            OrchControllerDelayExp.SendMessageToAll("ENDVOT_");
        }
       
        if (Input.GetKeyDown("f8"))
        {
            GameObject AuxVar = GameObject.Find("Pilot0Controller").GetComponent<SessionPlayersManager>().AllUsers.Find(isnotLocal).gameObject;
            AuxVar.GetComponentInChildren<PointBufferRenderer>().gameObject.transform.rotation = Quaternion.Euler(PCRotationoffset);
            AuxVar.GetComponentInChildren<PointBufferRenderer>().gameObject.transform.position = PCTranslationoffset;
        }
        Debug.Log("Distance between rendercamera and trackedcamera is " + (Vector3.Distance(CameraRendererRight.transform.position, CameraRight.transform.position).ToString()));

    }
}

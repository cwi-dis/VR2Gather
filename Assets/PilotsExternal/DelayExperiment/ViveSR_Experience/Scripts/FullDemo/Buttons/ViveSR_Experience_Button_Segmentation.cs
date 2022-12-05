using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

namespace Vive.Plugin.SR.Experience
{
    public class ViveSR_Experience_Button_Segmentation : ViveSR_Experience_IButton
    {
        ViveSR_Experience_NPCGenerator npcGenerator;

        [SerializeField] GameObject HintLocatorPrefab;

        List<ViveSR_Experience_Chair> MR_Chairs = new List<ViveSR_Experience_Chair>();
        List<GameObject> HintLocators = new List<GameObject>();

        ViveSR_Experience_StaticMeshToolManager StaticMeshTools;

        List<SceneUnderstandingDataReader.SceneUnderstandingObject> SegResults;
        ViveSR_Experience_ActionSequence ActionSequence;

        ViveSR_Experience_Portal PortalScript;

        public UnityEvent portalCamerasEnabledEvent = new UnityEvent();
        public UnityEvent portalCamerasDisabledEvent = new UnityEvent();

        protected override void AwakeToDo()
        {
            ButtonType = MenuButton.Segmentation;

            StaticMeshTools = ViveSR_Experience_Demo.instance.StaticMeshTools;    

            if (!ViveSR_Experience.instance.IsAMD)
                EnableButton(StaticMeshTools.SceneUnderstandingScript.CheckChairExist());

            StaticMeshTools = FindObjectOfType<ViveSR_Experience_StaticMeshToolManager>();
            npcGenerator = GetComponent<ViveSR_Experience_NPCGenerator>();

            PortalScript = ViveSR_Experience_Demo.instance.PortalScript;
        }

        public override void ActionToDo()
        {
            ViveSR_Experience_Demo.instance.realWorldFloor.SetActive(isOn);
            if (isOn)
            {
                //wait for tutorial segmentation handler to reaction on UI before turning it off
                this.DelayOneFrame(() =>
                {
                    ViveSR_Experience_Demo.instance.Rotator.RenderButtons(false);
                    ViveSR_Experience_Demo.instance.Tutorial.ToggleTutorial(false);
                });

                ActionSequence = ViveSR_Experience_ActionSequence.CreateActionSequence(gameObject);

                ActionSequence.AddAction(() => StaticMeshTools.StaticMeshScript.LoadMesh(true, false,
                        () => ViveSR_Experience_HintMessage.instance.SetHintMessage(hintType.onHeadSet, "Loading Mesh...", false),
                        () =>
                        {
                            ViveSR_Experience_HintMessage.instance.SetHintMessage(hintType.onHeadSet, "Mesh Loaded!", true, 0.5f);
                            ActionSequence.ActionFinished();
                        }
                    ));

                ActionSequence.AddAction(() =>
                {
                    ViveSR_Experience_Demo.instance.Rotator.RenderButtons(true);        
                    ViveSR_Experience_Demo.instance.Tutorial.ToggleTutorial(true);
                    SegResults = StaticMeshTools.SceneUnderstandingScript.GetSegmentationInfo(SceneUnderstandingObjectType.CHAIR);

                    StaticMeshTools.SceneUnderstandingScript.GenerateHintLocators(SegResults);
                    LoadChair();
                    ViveSR_Experience_ControllerDelegate.touchpadDelegate += handleTouchpad_Play;
                    ActionSequence.ActionFinished();
                });
                ActionSequence.StartSequence();
            }
            else
            {
                ActionSequence.StopSequence();
                StaticMeshTools.StaticMeshScript.LoadMesh(false);

                ViveSR_Experience_ControllerDelegate.touchpadDelegate -= handleTouchpad_Play;

                PortalScript.PortalManager.TurnOffCamera();

                PortalScript.PortalManager.gameObject.SetActive(false);

                StaticMeshTools.SceneUnderstandingScript.ClearHintLocators();
                npcGenerator.ClearScene();

                portalCamerasDisabledEvent.Invoke();
            }
        }

        public void handleTouchpad_Play(ButtonStage buttonStage, Vector2 axis)
        {
            TouchpadDirection touchpadDirection = ViveSR_Experience_ControllerDelegate.GetTouchpadDirection(axis, false);
            switch (buttonStage)
            {
                case ButtonStage.PressDown:   
                    switch (touchpadDirection)
                    {
                        case TouchpadDirection.Up:
                            this.DelayOneFrame(()=>
                            {
                                if (isOn)
                                {
                                    StaticMeshTools.SceneUnderstandingScript.ClearHintLocators();
                                    PortalScript.PortalManager.gameObject.SetActive(true);

                                    PortalScript.PortalManager.TurnOnCamera();

                                    portalCamerasEnabledEvent.Invoke();
                                    Transform controller_fwd = PlayerHandUILaserPointer.LaserPointer.gameObject.transform;
                                    npcGenerator.Play(controller_fwd.position + controller_fwd.forward * 8, controller_fwd.forward, MR_Chairs);
                                }
                            });
                            break;
                    }                             
                    break;
            }
        }

        void LoadChair()
        {
            foreach (ViveSR_Experience_Chair MR_Chair in MR_Chairs) Destroy(MR_Chair.gameObject);
            MR_Chairs.Clear();

            foreach (GameObject go in HintLocators) Destroy(go);
            HintLocators.Clear();

            List<SceneUnderstandingDataReader.SceneUnderstandingObject> ChairElements = StaticMeshTools.SceneUnderstandingScript.GetSegmentationInfo(SceneUnderstandingObjectType.CHAIR);

            for (int i = 0; i < ChairElements.Count; i++)
            {
                GameObject go = new GameObject("MR_Chair" + i, typeof(ViveSR_Experience_Chair));
                ViveSR_Experience_Chair chair = go.GetComponent<ViveSR_Experience_Chair>();
                chair.CreateChair(new Vector3(ChairElements[i].position[0].x, ChairElements[i].position[0].y, ChairElements[i].position[0].z), new Vector3(ChairElements[i].forward.x, ChairElements[i].forward.y, ChairElements[i].forward.z));
                MR_Chairs.Add(chair);
            }
        }
    }
}
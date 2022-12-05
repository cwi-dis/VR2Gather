using System.Collections.Generic;
using UnityEngine;

namespace Vive.Plugin.SR.Experience
{
    public class ViveSR_Experience_IDartGenerator : MonoBehaviour
    {
        public bool isHolding = true;
        public int currentDartPrefeb;
        [SerializeField] protected List<GameObject> dart_prefabs;
        public List<GameObject> InstantiatedDarts;
        protected GameObject currentGameObj;

        [SerializeField] bool deleteOnDisable;

        protected ViveSR_Experience_DartGeneratorMgr dartGeneratorMgr;

        [SerializeField] bool allowDefaultSwitchPlacementMode = true;
        [SerializeField] bool allowDefaultClear = true;

        protected ViveSR_Experience_DeerColorMgr deerMgr;

        TouchpadDirection touchpadDirection = TouchpadDirection.None;
        TouchpadDirection touchpadDirection_prev;

        private void Awake()
        {
            deerMgr = GetComponent<ViveSR_Experience_DeerColorMgr>();
            dartGeneratorMgr = GetComponent<ViveSR_Experience_DartGeneratorMgr>();
            AwakeToDo();
        }

        protected virtual void AwakeToDo() { } 

        private void OnEnable()
        {
            ViveSR_Experience_ControllerDelegate.triggerDelegate += HandleTrigger_DartGenerator;

            OnEnableToDo();
        }

        protected virtual void OnEnableToDo() { }
        protected virtual void OnDisableToDo() { }

        void HandleTrigger_DartGenerator(ButtonStage buttonStage, Vector2 axis)
        {
            if (!enabled) return;
            if (!ViveSR_RigidReconstruction.IsExporting && !ViveSR_RigidReconstruction.IsScanning && enabled)
            {
                switch (buttonStage)
                {
                    case ButtonStage.PressDown:
                        if (Time.timeSinceLevelLoad - dartGeneratorMgr.tempTime > dartGeneratorMgr.coolDownTime)
                        {
                            TriggerPress();
                        }
                        break;

                    case ButtonStage.Press:
                        if (isHolding)
                        {
                            TriggerHold();
                        }
                        break;

                    case ButtonStage.PressUp:
                        if (isHolding)
                        {
                            TriggerRelease();
                            dartGeneratorMgr.tempTime = Time.timeSinceLevelLoad;
                        }
                        break;
                }
            }
        }

        void HandleTouchpad_SwitchDart(ButtonStage buttonStage, Vector2 axis)
        {
            if (!enabled) return;
            touchpadDirection_prev = touchpadDirection;
            touchpadDirection = ViveSR_Experience_ControllerDelegate.GetTouchpadDirection(axis, true);
            #region VIVE PRO
            if (ViveSR_Experience.instance.CurrentDevice == DeviceType.VIVE_PRO)
            {
                switch (buttonStage)
                {
                    case ButtonStage.PressDown:

                        switch (touchpadDirection)
                        {
                            case TouchpadDirection.Right:
                                SwitchDart(true);
                                break;
                            case TouchpadDirection.Left:
                                SwitchDart(false);
                                break;
                            case TouchpadDirection.Up:
                                if (allowDefaultSwitchPlacementMode) dartGeneratorMgr.SwitchPlacementMode();
                                ViveSR_Experience_ControllerDelegate.touchpadDelegate -= HandleTouchpad_SwitchDart;
                                break;
                            case TouchpadDirection.Down:
                                if (allowDefaultClear) dartGeneratorMgr.DestroyObjs();
                                break;
                        }
                        break;
                }
            }
            #endregion
            #region VIVE COSMOS
            else if (ViveSR_Experience.instance.CurrentDevice == DeviceType.VIVE_COSMOS)
            {
                switch (buttonStage)
                {
                    case ButtonStage.PressDown:
                        switch (touchpadDirection)
                        {
                            case TouchpadDirection.Right: SwitchDart(true); break;
                            case TouchpadDirection.Left: SwitchDart(false); break;
                            case TouchpadDirection.Up:
                                if (allowDefaultSwitchPlacementMode) dartGeneratorMgr.SwitchPlacementMode();
                                ViveSR_Experience_ControllerDelegate.touchpadDelegate -= HandleTouchpad_SwitchDart;
                                break;
                            case TouchpadDirection.Down:
                                if (allowDefaultClear) dartGeneratorMgr.DestroyObjs();
                                break;
                        }
                        break;
                }
                if (touchpadDirection_prev != touchpadDirection)
                {
                    switch (touchpadDirection)
                    {
                        case TouchpadDirection.Right:
                            SwitchDart(true);
                            break;
                        case TouchpadDirection.Left:
                            SwitchDart(false);
                            break;
                        case TouchpadDirection.Up:
                            if (allowDefaultSwitchPlacementMode) dartGeneratorMgr.SwitchPlacementMode();
                            ViveSR_Experience_ControllerDelegate.touchpadDelegate -= HandleTouchpad_SwitchDart;
                            break;
                        case TouchpadDirection.Down:
                            if (allowDefaultClear) dartGeneratorMgr.DestroyObjs();
                            break;
                    }
                }
            }
            #endregion
        }

        public virtual void TriggerPress()
        {
            ViveSR_Experience_ControllerDelegate.touchpadDelegate += HandleTouchpad_SwitchDart;
        }
        protected virtual void TriggerHold() {}

        public virtual void TriggerRelease()
        {
            ViveSR_Experience_ControllerDelegate.touchpadDelegate -= HandleTouchpad_SwitchDart;
        }

        void SwitchDart(bool isAdd)
        {              
            int typeNum = currentDartPrefeb;

            if (isAdd) currentDartPrefeb = (typeNum + 1) % dart_prefabs.Count;
            else currentDartPrefeb = typeNum - 1 > -1 ? typeNum - 1 : dart_prefabs.Count - 1;

            Valve.VR.InteractionSystem.Interactable interactable = currentGameObj.GetComponent<Valve.VR.InteractionSystem.Interactable>();

            if (interactable != null && interactable.attachedToHand != null)
                interactable.attachedToHand.DetachObject(currentGameObj);

            Destroy(currentGameObj);
            InstantiatedDarts.RemoveAt(InstantiatedDarts.Count - 1);

            GenerateDart();

            InstantiatedDarts.Add(currentGameObj);
           
        }
        protected virtual void GenerateDart() { }

        private void OnDisable()
        {
            ViveSR_Experience_ControllerDelegate.triggerDelegate -= HandleTrigger_DartGenerator;

            if (deleteOnDisable)
            {
                foreach (GameObject obj in InstantiatedDarts) Destroy(obj);
                InstantiatedDarts.Clear();
            }

            OnDisableToDo();
        }

        public void DestroyObjs()
        {
            GameObject heldObj = null;
            if(InstantiatedDarts.Count > 0) heldObj = InstantiatedDarts[InstantiatedDarts.Count - 1];
            if (isHolding)
            {
                if (InstantiatedDarts.Count > 1)
                {
                    for (int i = InstantiatedDarts.Count - 2; i >= 0; i--)
                    {
                        Destroy(InstantiatedDarts[i]);
                    }
                }
                InstantiatedDarts.Clear();
                if(heldObj) InstantiatedDarts.Add(heldObj);
            }
            else
            {
                foreach (GameObject obj in InstantiatedDarts)
                    Destroy(obj);

                InstantiatedDarts.Clear();
            }
        }    
    }
}
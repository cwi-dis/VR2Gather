using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Vive.Plugin.SR.Experience
{
    public class ViveSR_Experience_Rotator : MonoBehaviour
    {
        public bool isRotateOn { get; private set; }

        float distToCenter = -0.034f;
        float rotateSpeed = 200;
        float buttonEnlargingSpeed = 1.7f;

        bool isTouchPositive = false;
        float localY, targetY;
        float rotateAngle;

        public ViveSR_Experience_IButton CurrentButton { get; private set; }

        Vector3 Vector_Enlarged, Vector_RegularSize;
        float EnlargedSize = 1.2f, RegularSize = 0.8f;

        List<ViveSR_Experience_IButton> offsetButtons = new List<ViveSR_Experience_IButton>();
        List<int> oldOffsetButtonDegrees = new List<int>();

        public bool isRotateDown { get; private set; }

        public List<ViveSR_Experience_IButton> IncludedBtns;

        TouchpadDirection touchpadDirection = TouchpadDirection.None;
        TouchpadDirection touchpadDirection_prev;

        public void SetRotator(bool isOn)
        {
            isRotateOn = isOn;
        }

        public void Init()
        {
            Vector_Enlarged = Vector3.one * EnlargedSize;
            Vector_RegularSize = Vector3.one * RegularSize;

            InitRotator();
            ViveSR_Experience_ControllerDelegate.touchpadDelegate += HandleTouchpad_RotatorControl;        
        }

        void InitRotator()
        {                   
            transform.localEulerAngles = Vector3.zero;

            Color fadedColor = ViveSR_Experience_Demo.instance.DisableColor;

            for (int i = 1; i < IncludedBtns.Count; i++)         // set default transparency. not changing depth image button.
                IncludedBtns[i].SetIconColor(fadedColor);

            //Rotate the Buttons and then expands them to form a circle.
            for (int i = 0; i < IncludedBtns.Count; i++)
            {
                //Add 90 degrees to match the controller's orientation.
                if (i == 0) rotateAngle = 90;

                //Rotate the Buttons.
                IncludedBtns[i].gameObject.transform.localEulerAngles += new Vector3(0, rotateAngle, 0);

                //Extend the Button from the geo center of all Buttons.
                IncludedBtns[i].gameObject.transform.GetChild(0).transform.localPosition += new Vector3(distToCenter, 0, 0);

                //Accumulate the degree number for the next button.
                rotateAngle += 360 / IncludedBtns.Count;

                IncludedBtns[i].rotatorIdx = i;
            }

            //Move the UI to Attachpoint.
            SetRotatorTransform();

            if (distToCenter >= 0) transform.localScale = new Vector3(-transform.localScale.x, transform.localScale.y, transform.localScale.z);
            else transform.localScale = new Vector3(transform.localScale.x, transform.localScale.y , -transform.localScale.z );

            //Assign & Enlarge the current Button.
            CurrentButton = IncludedBtns[0];
            CurrentButton.gameObject.transform.GetChild(0).transform.localScale *= 1.2f;
            CurrentButton.gameObject.transform.localScale = Vector_Enlarged;
            CurrentButton.SetFrameColor(ViveSR_Experience_Demo.instance.BrightFrameColor);

            //Push & scale down non-current buttons
            AdjustUITransform();

            //Display of included buttons
            RenderButtons(true);
        }

        public void SetRotatorTransform()
        {
            if (ViveSR_Experience_Demo.instance == null)
                return;

            ViveSR_Experience_Rotator rotator = ViveSR_Experience_Demo.instance.Rotator;
            switch (ViveSR_Experience.instance.CurrentDevice)
            {
                case DeviceType.VIVE_PRO:
                    rotator.transform.localPosition = new Vector3(0, -0.085f, 0.03f);
                    break;
                case DeviceType.VIVE_COSMOS:
                    rotator.transform.localPosition = new Vector3(0, -0.06f, 0.03f);
                    break;
                default:
                    goto case DeviceType.VIVE_PRO;
            }
        }

        public void HandleTouchpad_RotatorControl(ButtonStage buttonStage, Vector2 axis)
        {
            if (!isRotateOn) return;
 
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
                            case TouchpadDirection.Mid: { Debug.Log("[ViveSR Experience] ActOnButton"); ActOnButton(); } break; //Mid: Excute the choosen Button.
                            case TouchpadDirection.Right: isRotateDown = true; break;
                            case TouchpadDirection.Left: isRotateDown = true; break;
                        } break;

                    case ButtonStage.Press:
                        if (isRotateDown)
                        {
                            switch (touchpadDirection)
                            {
                                case TouchpadDirection.Right: StartCoroutine(Rotate(true)); break;  
                                case TouchpadDirection.Left: StartCoroutine(Rotate(false)); break;
                            }
                        } break;
                }

                switch (buttonStage)
                {
                    case ButtonStage.PressUp: isRotateDown = false; break;
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
                            case TouchpadDirection.Mid:
                                ActOnButton();
                                break; //Mid: Excute the choosen Button.
                            case TouchpadDirection.Right:
                                isRotateDown = true;
                                StartCoroutine(Rotate(true));
                                break; 
                            case TouchpadDirection.Left:       
                                isRotateDown = true;
                                StartCoroutine(Rotate(false));
                                break;
                        }
                        break;
                }

                if (touchpadDirection_prev != touchpadDirection)
                {
                    switch (touchpadDirection)
                    {
                        case TouchpadDirection.Right:
                            isRotateDown = true;
                            StartCoroutine(Rotate(true));
                            break;
                        case TouchpadDirection.Left:
                            isRotateDown = true;
                            StartCoroutine(Rotate(false)); break;
                    }
                } 
            }
            #endregion
        }

        void Enlarge(ViveSR_Experience_IButton button, System.Action done = null)
        {
            ViveSR_Experience.instance.SoundManager.PlayAtAttachPoint(AudioClipIndex.Slide);
            ColorFade(CurrentButton, false);

            StartCoroutine(_Enlarge(true, button, done));

        }   
        void Shirnk(ViveSR_Experience_IButton button, System.Action done = null)
        {
            ColorFade(CurrentButton, true);

            StartCoroutine(_Enlarge(false, button, done));
        }
        IEnumerator _Enlarge(bool on, ViveSR_Experience_IButton button, System.Action done)
        {
            if (on) button.isEnlarging = true;
            else button.isShrinking = true;

            bool a = true;
            bool b = true;

            //on ? enlarge : shrink
            while (a && b)
            {
                button.transform.localScale += (on ? 1 : -1) * new Vector3(buttonEnlargingSpeed * Time.deltaTime, buttonEnlargingSpeed * Time.deltaTime, buttonEnlargingSpeed * Time.deltaTime);
               
                a = on ? button.transform.localScale.x < EnlargedSize : button.transform.localScale.x > RegularSize;
                b = on ? button.isEnlarging : button.isShrinking;

                yield return new WaitForEndOfFrame();
            }

            button.transform.localScale = on ? Vector_Enlarged : Vector_RegularSize;
            button.frame.transform.localScale = Vector3.one * (on ? 1.13f : 0.9f);

            if (on) button.isEnlarging = false;
            else button.isShrinking = false;

            if (done != null) done();
        }

        void ColorFade(ViveSR_Experience_IButton button, bool isFading)
        {
            StartCoroutine(_ColorFade(button, isFading));
        }
        IEnumerator _ColorFade(ViveSR_Experience_IButton button, bool isFading)
        {
            Color newColor;

            bool a = true, b = true;

            Color iconColor = button.renderer.material.color;

            while (a && b)
            {
                iconColor = button.renderer.material.color;

                newColor = new Color(iconColor.r, iconColor.g, iconColor.b,
                   button.renderer.material.color.a + 2f * Time.deltaTime * (isFading ? -1 : 1));

                button.SetIconColor(newColor);
                a = isFading ? (iconColor.a > 0.3f) : (iconColor.a < 0.95f);
                b = isFading ? button.isShrinking : button.isEnlarging;

                yield return new WaitForEndOfFrame();
            }

            Color disableColor = ViveSR_Experience_Demo.instance.DisableColor;
            Color originalColor = ViveSR_Experience_Demo.instance.OriginalEmissionColor;
            Color brightFrameColor = ViveSR_Experience_Demo.instance.BrightFrameColor;
     
            button.SetIconColor((isFading || CurrentButton.disabled) ? disableColor : originalColor);
            button.SetFrameColor(isFading ? disableColor : brightFrameColor);
        }

        ViveSR_Experience_IButton GetButtons(bool isTouchPositive, int num)
        {
            int tempNum = (int)CurrentButton.ButtonType;
            tempNum += isTouchPositive ? num : -num;
            if (tempNum < 0) tempNum = IncludedBtns.Count + tempNum;
            else if (tempNum > IncludedBtns.Count - 1) tempNum = tempNum % IncludedBtns.Count;
            return ViveSR_Experience_Demo.instance.ButtonScripts[(MenuButton)tempNum];
        }     

        void AdjustUITransform()
        {
            for (int i = 0; i < offsetButtons.Count; i++)
            {
                Vector3 vec = offsetButtons[i].gameObject.transform.localEulerAngles;
                offsetButtons[i].gameObject.transform.localEulerAngles = new Vector3(vec.x, oldOffsetButtonDegrees[i], vec.z);
            }

            offsetButtons.Clear();
            oldOffsetButtonDegrees.Clear();

            List<ViveSR_Experience_IButton> Btns_Before = new List<ViveSR_Experience_IButton>();
            List<ViveSR_Experience_IButton> Btns_After = new List<ViveSR_Experience_IButton>();

            //get 3 buttons before & after current
            for (int i = 1; i < IncludedBtns.Count/2; i++) Btns_Before.Add(GetButtons(true, i));
            for (int i = 1; i < IncludedBtns.Count/2; i++) Btns_After.Add(GetButtons(false, i));

            OffsetButton(!isTouchPositive, Btns_Before.ToArray());
            OffsetButton(isTouchPositive, Btns_After.ToArray());
        }  

        void OffsetButton(bool isPositive, params ViveSR_Experience_IButton[] buttons)
        {
            float accelerator = 1f;
            int count = 0;
            foreach (ViveSR_Experience_IButton button in buttons)
            {
                offsetButtons.Add(button);

                Vector3 vec = button.gameObject.transform.localEulerAngles;

                int degree = 16;

                oldOffsetButtonDegrees.Add((int)vec.y);
                if (count == 0) accelerator = 1;
                else if (count == 1) accelerator = 0.63f;
                else if (count == 2) accelerator = 0.3f;
                float targetY = vec.y + degree * (isPositive ? 1 : -1) * accelerator;
                button.gameObject.transform.localEulerAngles = new Vector3(vec.x, targetY, vec.z);

                count += 1;
            }
        }

        IEnumerator Rotate(bool isTouchPositive)
        {                    
            SetRotator(false);

            /*---The Old Button---*/                
            CurrentButton.isEnlarging = false;
            Shirnk(CurrentButton);  

            if (CurrentButton.isOn && CurrentButton.disableWhenRotatedAway)
                CurrentButton.ActOnRotator(false);

            ViveSR_Experience_IButton oldBtn = CurrentButton;
            /*--------------------*/

            /*---The New Button---*/
            CurrentButton = GetButtons(isTouchPositive, 1);

            //Enlarge
            CurrentButton.isShrinking = false;
            Enlarge(CurrentButton);

            //Set the target degree.
            targetY = localY + (360 / (int)MenuButton.MaxNum) * (isTouchPositive ? 1 : -1);

            CurrentButton.gameObject.transform.GetChild(0).transform.localScale *= 1.2f;

            while (isTouchPositive ? localY < targetY : localY > targetY)
            {
                localY += (isTouchPositive ? 1 : -1) * rotateSpeed * Time.deltaTime;
                transform.localEulerAngles = new Vector3(transform.localEulerAngles.x, localY, transform.localEulerAngles.z);

                yield return new WaitForEndOfFrame();
            }

            targetY = localY = (360 / IncludedBtns.Count) * CurrentButton.rotatorIdx;
            transform.localEulerAngles = new Vector3(transform.localEulerAngles.x, targetY, transform.localEulerAngles.z);

            oldBtn.gameObject.transform.GetChild(0).transform.localScale = Vector3.one * 0.03f;

            AdjustUITransform();
            SetRotator(true);

            /*--------------------*/
        }

        void ActOnButton()
        {

            //Toggle on and off. Some Buttons do not allow toggling off.
            if ((CurrentButton.isOn && CurrentButton.allowToggle) || !CurrentButton.isOn)
            {
                if (!CurrentButton.disabled)
                {
                    CurrentButton.isOn = !CurrentButton.isOn;
                    CurrentButton.ActOnRotator(CurrentButton.isOn);
                }
            }
        }

        public void RenderButtons(bool isOn)
        {
            for (int i = 0; i < IncludedBtns.Count; i++)
            {
                IncludedBtns[i].renderer.enabled = isOn;
                IncludedBtns[i].frame.enabled = isOn;
            }       
            SetRotator(isOn);
        }
    }
}
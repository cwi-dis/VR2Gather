using UnityEngine;

namespace Vive.Plugin.SR.Experience
{
    public class ViveSR_Experience_IButton: MonoBehaviour
    {
        public MenuButton ButtonType { get; protected set; }
        public bool isOn = false;
        public bool isAMDCompatible;
        public bool disableWhenRotatedAway;
        public bool disabled;
        public bool allowToggle;
        public new Renderer renderer;
        public Renderer frame;  

        public ViveSR_Experience_ISubMenu SubMenu = null;

        public bool isShrinking, isEnlarging;

        //-1 means not in rotator included list
        public int rotatorIdx = -1;

        [SerializeField] protected Texture oriTex, subTex;

        public void Init_Awake()
        {
            frame.material = new Material(frame.material);

            AwakeToDo();

            if (ViveSR_Experience.instance.IsAMD && !isAMDCompatible)
                EnableButton(false);
        }

        protected virtual void AwakeToDo() { }

        public void Init_Start()
        {
            StartToDo();
        }

        protected virtual void StartToDo() { }

        protected void Update()
        {
            UpdateToDo();
        }
        protected virtual void UpdateToDo() { }

        public void Action(bool isOn)
        {
            if (!disabled)
            {
                this.isOn = isOn;
                if(SubMenu != null)
                {
                    SubMenu.enabled = isOn;
                    SubMenu.ToggleSubMenu(isOn);
                    renderer.material.mainTexture = isOn ? subTex : oriTex;
                }
                else
                {
                    ViveSR_Experience.instance.SoundManager.PlayAtAttachPoint(isOn ? AudioClipIndex.Select_On : AudioClipIndex.Select_Off);
                }
                SetButtonEmissionColor(isOn ? ColorType.Bright : ColorType.Original);
                ActionToDo();
            }
        }
        public virtual void ActionToDo() { }

        public virtual void ActOnRotator(bool isOn) 
        {
            Action(isOn);
        }

        public virtual void ForceExcuteButton(bool on)
        {
            Action(on);
        }

        public void EnableButton(bool on)
        {
            disabled = !on;

            SetButtonEmissionColor(on ? ColorType.Original : ColorType.Disable);
        }

        public void SetButtonEmissionColor(ColorType colorType)
        {
          //  Debug.Log("setColor");

            Color color = Color.clear;

            if (colorType == ColorType.Bright) color = ViveSR_Experience_Demo.instance.BrightColor;
            else if (colorType == ColorType.Original) color = ViveSR_Experience_Demo.instance.OriginalEmissionColor;
            else if (colorType == ColorType.Disable) color = ViveSR_Experience_Demo.instance.DisableColor;
            else if (colorType == ColorType.Attention) color = ViveSR_Experience_Demo.instance.BrightFrameColor;

            renderer.material.SetColor("_Color", color);
        }

        public void SetIconColor(Color color)
        {
            renderer.material.SetColor("_Color", color);
        }
         
        public void SetFrameColor(Color color)
        {
            frame.material.SetColor("_Color", color);
        }
    }
}







using UnityEngine;

namespace Vive.Plugin.SR.Experience
{
    public abstract class ViveSR_Experience_ISubBtn : MonoBehaviour
    {
        [HideInInspector] public ViveSR_Experience_ISubMenu SubMenu;
        protected int ThisButtonTypeNum;

        public bool isOn;
        public bool disabled;
        public bool isToggleAllowed;
        public new Renderer renderer;

        public bool isShrinking, isEnlarging;

        public void Init_Awake()
        {
            SubMenu = transform.parent.parent.GetComponent<ViveSR_Experience_ISubMenu>();
            AwakeToDo();
        }

        public void Init_Start()
        {
            StartToDo();
        }

        private void Update()
        {
            UpdateToDo();
        }

        protected virtual void AwakeToDo() { }
        protected virtual void StartToDo() { }
        protected virtual void UpdateToDo() { }

        public virtual void Execute()
        {
            if ((isOn && isToggleAllowed) || !isOn) isOn = !isOn;
            SetSubButtonColor(isOn ? ColorType.Bright: ColorType.Original);
            ExecuteToDo();
        }
        public virtual void ExecuteToDo() {}

        public void EnableButton(bool on)
        {  
            SubMenu.subBtnScripts[ThisButtonTypeNum].disabled = !on;
            SetSubButtonColor(on? ColorType.Original: ColorType.Disable);
        }

        public void ForceExcute(bool on)
        {
            isOn = on;

            ColorType _colorType;
            if (disabled) _colorType = ColorType.Disable;
            else _colorType = isOn ? ColorType.Bright : ColorType.Original;

            SetSubButtonColor(_colorType);
            ExecuteToDo();
        }

        public void SetSubButtonColor(ColorType colorType)
        {
            Color color = Color.clear;

            if (colorType == ColorType.Bright) color = ViveSR_Experience_Demo.instance.BrightColor;
            else if (colorType == ColorType.Original) color = ViveSR_Experience_Demo.instance.OriginalEmissionColor;
            else if (colorType == ColorType.Disable) color = ViveSR_Experience_Demo.instance.DisableColor;
            else if (colorType == ColorType.Attention) color = ViveSR_Experience_Demo.instance.BrightFrameColor;

            renderer.material.SetColor("_Color", color);
        }
    }
}
using UnityEngine;
using System.Collections.Generic;

namespace Vive.Plugin.SR.Experience
{
    public class ViveSR_Experience_Button_Effects : ViveSR_Experience_IButton
    {                                                 
        [SerializeField] Collider playerHeadCollider;
        [SerializeField] List<MeshRenderer> EffectBallRenderers;

        protected override void AwakeToDo()
        {
            ButtonType = MenuButton.Effects;
            playerHeadCollider = ViveSR_Experience.instance.PlayerHeadCollision.GetComponent<Collider>();

            MeshRenderer[] effectBallRenderers = GetComponentsInChildren<MeshRenderer>();
            foreach (MeshRenderer renderer in effectBallRenderers)
            {
                if (!renderer.name.Contains("_dup"))
                    EffectBallRenderers.Add(renderer);
            }
        }

        public override void ActionToDo()
        {
            playerHeadCollider.enabled = isOn;

            if (isOn)
            {
                ViveSR_Experience_ControllerDelegate.triggerDelegate += HandleTrigger_Effects;
            }
            else
            {
                ViveSR_Experience_ControllerDelegate.triggerDelegate -= HandleTrigger_Effects;
                ViveSR_Experience_Demo.instance.EffectsScript.ToggleEffects(false);
            }
        }

        void HandleTrigger_Effects(ButtonStage buttonStage, Vector2 axis)
        {
            switch (buttonStage)
            {
                case ButtonStage.PressDown:
                    ViveSR_Experience.instance.SoundManager.PlayAtAttachPoint(AudioClipIndex.EffectBall);
                    ViveSR_Experience_Demo.instance.Rotator.RenderButtons(false);
                    ViveSR_Experience_Demo.instance.EffectsScript.GenerateEffectBall();

                    for (int i = 0; i < EffectBallRenderers.Count; i++)
                        EffectBallRenderers[i].enabled = true;

                    break;
                case ButtonStage.PressUp:
                    ViveSR_Experience_Demo.instance.Rotator.RenderButtons(true);
                    ViveSR_Experience_Demo.instance.EffectsScript.HideEffectBall();
                    break;
            }
        }
    }
}
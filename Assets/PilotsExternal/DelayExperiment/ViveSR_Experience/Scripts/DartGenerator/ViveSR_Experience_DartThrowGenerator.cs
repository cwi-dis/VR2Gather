using Valve.VR.InteractionSystem;
using UnityEngine;

namespace Vive.Plugin.SR.Experience
{
    public class ViveSR_Experience_DartThrowGenerator : ViveSR_Experience_IDartGenerator
    {
        public override void TriggerPress()
        {
            base.TriggerPress();
            GenerateDart();
            InstantiatedDarts.Add(currentGameObj);
            isHolding = true;
        }
       
        protected override void TriggerHold()
        {
            //currentGameObj.transform.position = ViveSR_Experience.instance.AttachPoint.transform.position;
        }

        public override void TriggerRelease()
        {
            base.TriggerRelease();
            ViveSR_Experience.instance.targetHand.DetachObject(currentGameObj);
            currentGameObj.transform.SetParent(null);

            ViveSR_Experience_ParticleFade fadeScript = currentGameObj.GetComponent<ViveSR_Experience_ParticleFade>();
            if (fadeScript && fadeScript.FadeWhenDetached)
                fadeScript.StopParticleLooping();

            isHolding = false;
        }
        protected override void GenerateDart()
        {
            currentGameObj = Instantiate(dart_prefabs[currentDartPrefeb], ViveSR_Experience.instance.AttachPoint.transform);
            if (currentGameObj.name.Contains("viveDeer"))
            {      
                currentGameObj.transform.localEulerAngles = new Vector3(0, 180, 0);
                currentGameObj.GetComponent<Renderer>().material = deerMgr.deerMaterials[Random.Range(0, deerMgr.deerMaterials.Count - 1)];
                int scale = Random.Range(0, deerMgr.deerScale.Count);
                currentGameObj.transform.localScale = new Vector3(deerMgr.deerScale[scale], deerMgr.deerScale[scale], deerMgr.deerScale[scale]);
            }
            currentGameObj.GetComponent<ViveSR_Experience_Dart>().dartGeneratorMgr = dartGeneratorMgr;
            //attach obj without trigger(SteamVR standard) so Velocity Estimator can work right.

            Throwable throwable = currentGameObj.GetComponent<Throwable>();

            if (ViveSR_Experience.instance.CurrentDevice == DeviceType.VIVE_PRO)
                throwable.scaleReleaseVelocity = 1.2f;
            else if (ViveSR_Experience.instance.CurrentDevice == DeviceType.VIVE_COSMOS)
                throwable.scaleReleaseVelocity = 1.8f;

            ViveSR_Experience.instance.targetHand.AttachObject(currentGameObj, GrabTypes.Pinch, throwable.attachmentFlags, ViveSR_Experience.instance.targetHand.objectAttachmentPoint);
        }
    }
}
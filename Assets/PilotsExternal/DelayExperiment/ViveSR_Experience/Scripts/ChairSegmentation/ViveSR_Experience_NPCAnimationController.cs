using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Vive.Plugin.SR.Experience
{
    [RequireComponent(typeof(Animator))]
    public class ViveSR_Experience_NPCAnimationController : MonoBehaviour
    {
        Animator anim;
        [SerializeField] RuntimeAnimatorController runtimeAnimatorController;
        [SerializeField] List<ViveSR_Experience_NPCIKControl> IKControls;
        [SerializeField] Transform hipConstraint; //hip control
        [SerializeField] Transform hip; //link hip to hipConstraint
        public ViveSR_Experience_NPCAnimationRef NPCRef; //for overall rotation including IK constraints.

        float sittingSpeed = 1f;

        //reset chair
        Vector3 hipOriginalPos;
        Vector3 hipPos_Temp;

        public bool isActing { get; private set; }

        public ViveSR_Experience_ActionSequence ActionSequence;

        ViveSR_Experience_Chair chair;

        void Awake()
        {
            anim = GetComponent<Animator>();
            anim.runtimeAnimatorController = runtimeAnimatorController;

            hipOriginalPos = hipConstraint.transform.localPosition;
            foreach (ViveSR_Experience_NPCIKControl ikcontrol in IKControls)
            {
                ikcontrol.OriginalIKPos = ikcontrol.IKObj.transform.localPosition;
            }
        }

        IEnumerator CheckFrameStatus(System.Action done)
        {
            while (ViveSR.FrameworkStatus != FrameworkStatus.WORKING)
            {
                yield return new WaitForEndOfFrame();
            }

            done();
        }

        public void StartAnimationSequence_ChairFound(ViveSR_Experience_Chair chair)//go sit on chair
        {
            this.chair = chair;

            ResetCharacter();

            Vector3 FloorPosition = new Vector3(chair.transform.position.x, 0f, chair.transform.position.z) + chair.transform.forward * 0.5f;
            FloorPosition = new Vector3(FloorPosition.x, 0f, FloorPosition.z); // clear height 

            ActionSequence = ViveSR_Experience_ActionSequence.CreateActionSequence(gameObject);

            //  if (Vector3.Distance(FloorPosition, NPCRef.transform.position) > 0.1f)
            //      ActionSequence.AddAction(() => Turn((FloorPosition - NPCRef.transform.position) / Vector3.Distance(FloorPosition, NPCRef.transform.position), ActionSequence.ActionFinished));
            ActionSequence.AddAction(() => Walk(FloorPosition, ActionSequence.ActionFinished));
            ActionSequence.AddAction(() => Turn(chair.transform.forward, ActionSequence.ActionFinished));
            ActionSequence.AddAction(() => Sit(chair.transform.position, ActionSequence.ActionFinished));

            ActionSequence.StartSequence();
        }

        public void StartAnimationSequence_ChairNotFound(ViveSR_Experience_Chair facingChair)//turn and look at player
        {
            ActionSequence = ViveSR_Experience_ActionSequence.CreateActionSequence(gameObject);

            ActionSequence.AddAction(() => Walk(new Vector3(facingChair.transform.position.x, 0, facingChair.transform.position.z) + facingChair.transform.forward * 2, ActionSequence.ActionFinished));
            ActionSequence.AddAction(() => Turn(-facingChair.transform.forward, ActionSequence.ActionFinished));

            ActionSequence.StartSequence();
        }
          
        public void Walk(Vector3 targetPosition, System.Action done = null)
        {
            StartCoroutine(_Walk(targetPosition, done));
        }          
        IEnumerator _Walk(Vector3 targetPosition, System.Action done)
        {
            isActing = true;
            anim.SetBool("isWalking", true);

            while (Vector3.Distance(targetPosition, NPCRef.transform.position) > 0.4f)
            {
                ViveSR_Experience.instance.SoundManager.PlayAtAttachPoint(AudioClipIndex.FairyWalk);
                NPCRef.NavMeshAgent.destination = targetPosition;
                //NPCRef.transform.position = Vector3.MoveTowards(NPCRef.transform.position, targetPosition, walkspeed * Time.deltaTime);
                yield return new WaitForEndOfFrame();
            }
            NPCRef.transform.position = targetPosition; //match position

            anim.SetBool("isWalking", false);
            isActing = false;
            if (done != null) done();
        }
         

        void Turn (Vector3 dir, System.Action done = null)
        {
            StartCoroutine(_Turn(dir, done));
        }
        IEnumerator _Turn(Vector3 dir, System.Action done = null)
        {
            isActing = true;

            Debug.DrawRay(NPCRef.transform.position, dir, Color.green, 5f);

            //find the angle between the chair and the character.
            float angle = Vector3.Angle(dir, NPCRef.transform.forward);
                                                
            Vector3 cross = Vector3.Cross(dir, -NPCRef.transform.forward);
            if (cross.y < 0) angle = 360 - angle;

            if (angle > 10)
            {              
                if (angle >= 180 && angle < 360) //left
                {
                    anim.SetTrigger("isTurningLeft");
                }
                else if (angle < 180 && angle >= 0) //right
                {
                    anim.SetTrigger("isTurningRight");
                }

                while (angle > 10)
                {
                    NPCRef.transform.rotation = Quaternion.LookRotation(Vector3.RotateTowards(NPCRef.transform.forward, dir, 5f * Time.deltaTime, 0.0f));
                    angle = Vector3.Angle(dir, NPCRef.transform.forward);

                    yield return new WaitForEndOfFrame();
                }
            }

            NPCRef.transform.forward = dir;
            hipPos_Temp = hipConstraint.position;
            anim.ResetTrigger("isTurningRight");
            anim.ResetTrigger("isTurningLeft");
            anim.SetBool("isWalking", false);

            isActing = false;
            if (done != null) done(); 
        }

        void Sit(Vector3 targetHipPosition, System.Action done = null)
        {
            StartCoroutine(_Sit(targetHipPosition, done));

        }
        IEnumerator _Sit(Vector3 targetHipPosition, System.Action done = null)
        {
            isActing = true;
            SetArmIK();
            hip.transform.position = hipConstraint.position;
            anim.SetBool("isSitting", true);

            //lock limbs. Feet first so the feet don't float in a weird way. Arms can be locked while sitting.
            IKControls[2].IsActive = true;
            IKControls[3].IsActive = true;

            while (IKControls[2].IKWeight < 0.95f)
            {
                IKControls[2].IKWeight += 5f * Time.deltaTime;
                IKControls[3].IKWeight += 5f * Time.deltaTime;
                yield return new WaitForEndOfFrame();
            }

            IKControls[2].IKWeight = 1f;
            IKControls[3].IKWeight = 1f;


            yield return new WaitForSeconds(1f);

            foreach (ViveSR_Experience_NPCIKControl ikcontrol in IKControls)
            {
                if (ikcontrol.IKObj_thigh == null) continue;

                MoveHandIK(ikcontrol.IKObj, ikcontrol.IKObj_thigh);
            }

            //---Move hip----//
            if (chair.transform.position.y > 8f) //for unsittable
            {
                targetHipPosition = new Vector3(targetHipPosition.x, hipConstraint.transform.position.y, targetHipPosition.z);
            }

            while (Vector3.Distance(targetHipPosition, hipConstraint.transform.position) > 0.01)
            {
                hipConstraint.transform.position = Vector3.MoveTowards(hipConstraint.transform.position, targetHipPosition, sittingSpeed * Time.deltaTime);
                hip.transform.position = hipConstraint.position;
                yield return new WaitForEndOfFrame();
            }

            ViveSR_Experience.instance.SoundManager.PlayAtAttachPoint(AudioClipIndex.FairySit);

            hipConstraint.transform.position = targetHipPosition;
            hip.transform.position = hipConstraint.position;
            //--------------//


            yield return new WaitForSeconds(2f);
            isActing = false;
            if (done != null) done();

        }

        void MoveHandIK(GameObject ikObj, GameObject targetObj)
        {
            StartCoroutine(_MoveHandIK(ikObj, targetObj));
        }

        IEnumerator _MoveHandIK(GameObject ikObj, GameObject targetObj)
        {
            while (Vector3.Distance(ikObj.transform.position, targetObj.transform.position) > 0.01)
            {
                ikObj.transform.position = Vector3.MoveTowards(ikObj.transform.position, targetObj.transform.position, 0.3f * Time.deltaTime);
                yield return new WaitForEndOfFrame();
            }

            while (Vector3.Angle(ikObj.transform.forward, targetObj.transform.forward) > 0.01f)
            {
                ikObj.transform.rotation = Quaternion.RotateTowards(ikObj.transform.rotation, targetObj.transform.rotation, 100f * Time.deltaTime);
                yield return new WaitForEndOfFrame();
            }

            ikObj.transform.position = targetObj.transform.position;
            ikObj.transform.forward = targetObj.transform.forward;
        }

        public void Stand(System.Action done = null)
        {
            StartCoroutine(_Stand(done));
        }
        IEnumerator _Stand(System.Action done)
        {
            isActing = true;

            anim.SetBool("isSitting", false);
            SetArmIK_Stand();

            while (Vector3.Distance(hipPos_Temp, hipConstraint.transform.position) > 0.01)
            {
                hipConstraint.transform.position = Vector3.MoveTowards(hipConstraint.transform.position, hipPos_Temp, sittingSpeed * Time.deltaTime);
                hip.transform.position = hipConstraint.position;
                yield return new WaitForEndOfFrame();
            }

            hipConstraint.transform.position = hipPos_Temp;

            while (IKControls[2].IKWeight > 0f)
            {
                IKControls[2].IKWeight -= 5f * Time.deltaTime;
                IKControls[3].IKWeight -= 5f * Time.deltaTime;
                yield return new WaitForEndOfFrame();
            }

            IKControls[2].IKWeight = 0f;
            IKControls[3].IKWeight = 0f;

            yield return new WaitForSeconds(0.1f);
            isActing = false;
            if (done != null) done();
        }   

        void SetIK(bool active)
        {
            foreach (ViveSR_Experience_NPCIKControl ikcontrol in IKControls)
                ikcontrol.IsActive = active;
        }

        void SetArmIK()
        {
            StartCoroutine(_SetArmIK());
        }
        IEnumerator _SetArmIK()
        {
            IKControls[0].IsActive = true;
            IKControls[1].IsActive = true;
            while (IKControls[0].IKWeight < 0.95f)
            {
                IKControls[0].IKWeight += 0.5f * Time.deltaTime;
                IKControls[1].IKWeight += 0.5f * Time.deltaTime;
                yield return new WaitForEndOfFrame();
            }
            IKControls[0].IKWeight = 1f;
            IKControls[1].IKWeight = 1f;
        }

        void SetArmIK_Stand()
        {
            StartCoroutine(_SetArmIK_Stand());
        }
        IEnumerator _SetArmIK_Stand()
        {
            IKControls[0].IsActive = true;
            IKControls[1].IsActive = true;
            while (IKControls[0].IKWeight > 0f)
            {
                
                IKControls[0].IKWeight -= 1.5f * Time.deltaTime;
                IKControls[1].IKWeight -= 1.5f * Time.deltaTime;
                yield return new WaitForEndOfFrame();
            }
            IKControls[0].IKWeight = 0f;
            IKControls[1].IKWeight = 0f;
        }

        void OnAnimatorIK()
        {
            foreach (ViveSR_Experience_NPCIKControl ikcontrol in IKControls)
            {
                if (ikcontrol.IsActive)
                {
                    anim.SetIKPositionWeight(ikcontrol.targetPart, ikcontrol.IKWeight);
                    anim.SetIKRotationWeight(ikcontrol.targetPart, ikcontrol.IKWeight);
                    anim.SetIKPosition(ikcontrol.targetPart, ikcontrol.IKObj.transform.position);
                    anim.SetIKRotation(ikcontrol.targetPart, ikcontrol.IKObj.transform.rotation);

                    if (ikcontrol.IKHintObj == null) continue;
                    anim.SetIKHintPositionWeight(ikcontrol.targetHint, ikcontrol.IKWeight);
                    anim.SetIKHintPosition(ikcontrol.targetHint, ikcontrol.IKHintObj.transform.position);
                }
            }
        }

        void ResetCharacter()
        {
            if(ActionSequence!=null) ActionSequence.StopSequence();

            anim.SetBool("isWalking", false);
            anim.SetBool("isSitting", false);
            anim.ResetTrigger("isTurningRight");
            anim.ResetTrigger("isTurningLeft");
            StopAllCoroutines();
            SetIK(false);
            IKControls[0].IKWeight = 0f;
            IKControls[1].IKWeight = 0f;
            IKControls[2].IKWeight = 0f;
            IKControls[3].IKWeight = 0f;
            hipConstraint.transform.localPosition = hipOriginalPos;
            hip.transform.position = hipConstraint.transform.position;
            foreach (ViveSR_Experience_NPCIKControl ikcontrol in IKControls)
            {
                ikcontrol.IKObj.transform.localPosition = ikcontrol.OriginalIKPos;
            }
        }

        private void OnDestroy()
        {
            if(ActionSequence!=null) ActionSequence.StopSequence();
        }
    }
}
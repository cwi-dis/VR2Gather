using UnityEngine;
namespace Vive.Plugin.SR.Experience
{
    [RequireComponent(typeof(LineRenderer))]
    public class ViveSR_Experience_DartRaycastGenerator : ViveSR_Experience_IDartGenerator
    {
        RaycastHit hitInfo;
        LineRenderer lineRenderer;

        protected override void AwakeToDo()
        {
            lineRenderer = GetComponent<LineRenderer>();                                    
        }

        protected override void OnDisableToDo()
        {
            lineRenderer.enabled = false;
        }

        public override void TriggerPress()
        {
            base.TriggerPress();
            lineRenderer.enabled = true;
            InstantiatedDarts.Add(currentGameObj);

            isHolding = true;
        }

        protected override void TriggerHold()
        {
            Transform renderPoint;
            if (PlayerHandUILaserPointer.LaserPointer != null)
            {
                renderPoint = PlayerHandUILaserPointer.LaserPointer.transform;
            }
            else if (ViveSR_Experience.instance.AttachPoint.transform.Find("RaycastStartPoint") != null)
            {
                renderPoint = ViveSR_Experience.instance.AttachPoint.transform.Find("RaycastStartPoint").transform;
            }
            else
            {
                Debug.Log("RaycastStartPoint is not found.");
                return;
            }

            Vector3 fwd = renderPoint.forward;
            Physics.Raycast(renderPoint.position, fwd, out hitInfo);
            lineRenderer.SetPosition(0, renderPoint.position);
            if (hitInfo.rigidbody != null)
            {               
                if(currentGameObj == null) GenerateDart();
                else currentGameObj.SetActive(true);
                lineRenderer.endColor = Color.green;
                currentGameObj.transform.position = hitInfo.point;
                currentGameObj.transform.up = hitInfo.normal;
                lineRenderer.SetPosition(1, hitInfo.point);
            }
            else
            {
                if(currentGameObj != null) currentGameObj.SetActive(false);
                lineRenderer.endColor = Color.red;
                lineRenderer.SetPosition(1, fwd * 0.5f + renderPoint.position);
            }
        }

        public override void TriggerRelease()
        {
            base.TriggerRelease();
            lineRenderer.endColor = Color.white;
            lineRenderer.enabled = false;

            if (currentGameObj != null)
            {
                if (hitInfo.rigidbody == null) Destroy(currentGameObj);
                else
                {
                    ViveSR_Experience.instance.targetHand.DetachObject(currentGameObj);
                    currentGameObj.transform.parent = null;
                    InstantiatedDarts.Add(currentGameObj);
                }

                currentGameObj = null;
            }

            isHolding = false;
        }

        protected override void GenerateDart()
        {
            currentGameObj = Instantiate(dart_prefabs[currentDartPrefeb]);
            currentGameObj.transform.eulerAngles = Vector3.zero;
            currentGameObj.GetComponent<ViveSR_Experience_Dart>().dartGeneratorMgr = dartGeneratorMgr;

            if (currentGameObj.name.Contains("viveDeer"))
            {
                currentGameObj.GetComponent<Renderer>().material = deerMgr.deerMaterials[Random.Range(0, deerMgr.deerMaterials.Count - 1)];
                int scale = Random.Range(0, deerMgr.deerScale.Count);
                currentGameObj.transform.localScale = new Vector3(deerMgr.deerScale[scale], deerMgr.deerScale[scale], deerMgr.deerScale[scale]);
            }
        }
    }
}
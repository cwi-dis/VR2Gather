using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Vive.Plugin.SR.Experience
{
    public class ViveSR_Experience_TileSpawner : MonoBehaviour
    {
        public enum RaycastMode
        {
            ValidHit,
            ValidHit_Horizontal,
            InvalidHit,
            NoHit
        }

        public GameObject RaycastStartPoint;
        [SerializeField] ViveSR_Experience_TileMgr tileMgr;
        [SerializeField] LineRenderer lineRenderer;
        [SerializeField] GameObject Prefab;
        [Range(.0f, 1.0f)] public float targetScale = 1.0f;
        [Range(.0f, 1.0f)] public float timeInterval = 0.1f;
        [Range(0, 20)] public int growFrequence = 10;

        float tileLength = .0f;
        float tileWidth = .0f;
        float tileHeight = .0f;
        float tilePlaceHeight = .003f;

        RaycastMode raycastMode = RaycastMode.ValidHit;    
        private List<Vector3> spawnLocations = new List<Vector3>();

        [SerializeField] GameObject floatingTile;
        GameObject redCube;
        ViveSR_RigidReconstructionCollider hitCldInfo = null;
        ViveSR_RigidReconstructionColliderManager cldPool = null;
        RaycastHit hit;

        bool isTriggerDown;
        bool isGeneratingTiles;

        Color lightRed, lightGreen;

        private void Awake()
        {
            redCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Destroy(redCube.GetComponent<BoxCollider>());
            redCube.transform.localScale = Vector3.one * 0.07f;
            redCube.SetActive(false);
            Material mat = redCube.GetComponent<Renderer>().material = new Material(Shader.Find("Unlit/Color"));
            mat.color = Color.red;

            lightRed = new Color(1f, 0.5f, 0.5f, 1f);
            lightGreen = new Color(0.5f, 1f, 0.5f, 1f);
        }

        private void OnEnable()
        {
            floatingTile.SetActive(true);

            // reset floating
            ResetFloatingTile();
        }

        private void OnDisable()
        {
            floatingTile.SetActive(false);
        }

        private void Update()
        {
            if (isTriggerDown)
            {
                UpdateRaycastMode();
                UpdateRaycastLine();
            }
            else
            {
                floatingTile.transform.position = ViveSR_Experience.instance.AttachPoint.transform.position;
            }
        }

        void UpdateRaycastMode()
        {
            Vector3 forward = RaycastStartPoint.transform.forward;
            Vector3 startPos = RaycastStartPoint.transform.position;

            Physics.Raycast(startPos, forward, out hit);
            if (hit.collider)
            {
                ViveSR_RigidReconstructionCollider cldInfo = hit.collider.gameObject.GetComponent<ViveSR_RigidReconstructionCollider>();
                if (CheckHorizontalValidHit(hit, cldInfo))
                {
                    raycastMode = RaycastMode.ValidHit_Horizontal;
                    hitCldInfo = cldInfo;
                }
                else if (CheckValidHit(hit, cldInfo))
                {
                    raycastMode = RaycastMode.ValidHit;
                    hitCldInfo = cldInfo;
                }
                else
                {
                    raycastMode = RaycastMode.InvalidHit;
                }
            }
            else
            {
                raycastMode = RaycastMode.NoHit;
            }
        }

        void UpdateRaycastLine()
        {      
            lineRenderer.SetPosition(0, RaycastStartPoint.transform.position);
            Vector3 targetPos;

            floatingTile.SetActive(raycastMode == RaycastMode.ValidHit_Horizontal);
            redCube.SetActive(raycastMode == RaycastMode.ValidHit);

            if (raycastMode == RaycastMode.NoHit)
            {
                lineRenderer.startColor = lightRed;
                lineRenderer.endColor = Color.red;
                targetPos = RaycastStartPoint.transform.position + RaycastStartPoint.transform.forward * 10.0f;
            }
            else if (raycastMode == RaycastMode.InvalidHit)
            {
                lineRenderer.startColor = lightRed;
                lineRenderer.endColor = Color.red;
                targetPos = hit.point;
            }
            else if(raycastMode == RaycastMode.ValidHit_Horizontal)
            {
                lineRenderer.startColor = lightGreen;
                lineRenderer.endColor = Color.green;
                targetPos = hit.point;
                floatingTile.transform.position = targetPos;
            }
            else //validHit
            {
                lineRenderer.startColor = lightGreen;
                lineRenderer.endColor = Color.green;
                targetPos = hit.point;
                redCube.transform.position = targetPos;
            }
            lineRenderer.SetPosition(1, targetPos);
        }

        public void TriggerPressDown()
        {
            isTriggerDown = true;
            lineRenderer.enabled = true;
        }

        public void TriggerPressUp()
        {
            isTriggerDown = false;
            lineRenderer.enabled = false;
            floatingTile.SetActive(true);
            if (raycastMode == RaycastMode.ValidHit_Horizontal)
            {
                ClearTiles();
                SpawnTiles();
            }
            else if (raycastMode == RaycastMode.ValidHit)
            {
                ClearTiles();
                ShowCubesOnCollider();
                redCube.SetActive(false);
            }
        }

        public void SetCldPool(ViveSR_RigidReconstructionColliderManager cld_pool)
        {
            cldPool = cld_pool;
        }

        public void ResetFloatingTile()
        {
            floatingTile.transform.eulerAngles = Vector3.zero;
        }

        public void RotateFloatingTile(float degree)
        {
            floatingTile.transform.Rotate(Vector3.up * degree, Space.Self);
        }

        public void ClearTiles()
        {
            isGeneratingTiles = false;
            tileMgr.RemoveAllTiles();
            if (cldPool) cldPool.ClearPlacerList();
        }

        bool CheckHorizontalValidHit(RaycastHit hitInfo, ViveSR_RigidReconstructionCollider cldInfo)
        {
            if (hitInfo.collider != null && cldInfo != null &&
                (cldInfo.Orientation == PlaneOrientation.HORIZONTAL))
            {
                return true;
            }
            return false;
        }

        bool CheckValidHit(RaycastHit hitInfo, ViveSR_RigidReconstructionCollider cldInfo)
        {
            if (hitInfo.collider != null && cldInfo != null &&
                (/*cldInfo.Orientation == PlaneOrientation.HORIZONTAL || */cldInfo.Orientation == PlaneOrientation.VERTICAL || cldInfo.Orientation == PlaneOrientation.OBLIQUE))
            {
                return true;
            }

            return false;
        }

        void ShowCubesOnCollider()
        {
            ViveSR_RigidReconstructionCollider[] info = { hitCldInfo };
            if(cldPool) cldPool.DrawAllExtractedPlacerLocations(info);
        }

        void ShowCollider(ViveSR_RigidReconstructionCollider cldInfo)
        {
            MeshRenderer rnd = cldInfo.GetComponent<MeshRenderer>();
            Material wireframe = new Material(Shader.Find("ViveSR/Wireframe"));
            wireframe.SetFloat("_ZTest", 0);
            wireframe.SetFloat("_Thickness", 0);
            rnd.sharedMaterial = wireframe;
            rnd.enabled = true;
        }

        void SpawnTiles()
        {
            // render on a new plane
            StartCoroutine(RenderTilesWithRightAxis(floatingTile.transform.right));
        }

        IEnumerator RenderTilesWithRightAxis(Vector3 right)
        {
            isGeneratingTiles = true;

            tileWidth = Prefab.transform.localScale.x;
            tileHeight = Prefab.transform.localScale.y;
            tileLength = Prefab.transform.localScale.z;

            Quaternion outRot;
            hitCldInfo.GetColliderUsableLocationsWithRightAxis(tileWidth, tileLength, tilePlaceHeight, spawnLocations, out outRot, ref right);
            List<GameObject> renderingTiles = new List<GameObject>();

            for (int i = 0; i < spawnLocations.Count; i++)
            {
                GameObject tile = Instantiate(Prefab, spawnLocations[i], outRot);
                tileMgr.AddTile(tile);
                renderingTiles.Add(tile);
            }

            for (int j = 0; j <= growFrequence && isGeneratingTiles; j++)
            {
                for (int i = 0; i < renderingTiles.Count; i++)
                {
                    renderingTiles[i].transform.localScale = new Vector3(tileWidth * targetScale / growFrequence * j, tileHeight * targetScale / growFrequence * j, tileLength * targetScale / growFrequence * j);
                }
                yield return new WaitForSeconds(timeInterval / growFrequence);
            }

            isGeneratingTiles = false;
        }
    }
}
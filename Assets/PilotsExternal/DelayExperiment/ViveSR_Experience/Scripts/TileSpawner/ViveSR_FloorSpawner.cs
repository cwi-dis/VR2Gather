using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Vive.Plugin.SR.Experience
{
    public class ViveSR_FloorSpawner : MonoBehaviour
    {

        [SerializeField] GameObject AttachPoint;
        [SerializeField] ViveSR_Experience_TileMgr tileMgr;
        [SerializeField] GameObject Prefab;
        [SerializeField] GameObject PrefabWithAxis;
        GameObject floatingTile = null;
        [Range(.0f, 1.0f)] public float targetScale = 1.0f;
        [Range(.0f, 1.0f)] public float timeInterval = 0.1f;
        [Range(0, 20)] public int growFrequence = 10;

        float targetLengthX = .0f;
        float targetLengthY = .0f;
        float targetLengthZ = .0f;

        //ViveSR_StaticColliderPool cldPool = null;
        ViveSR_RigidReconstructionCollider floor = null;
        Vector3 right4FloatingTile;
        Vector3 up4FloatingTile;
        Vector3 forward4FloatingTile;
        IEnumerator coroutine = null;

        // Update is called once per frame
        void Update()
        {
            // update floating tile pos
            floatingTile.transform.position = AttachPoint.transform.position;
        }

        public void OnEnable()
        {
            // show floating tile
            if (!floatingTile)
                floatingTile = Instantiate(PrefabWithAxis);
        }

        public void OnDisable()
        {
            // hide floating tile
            if (floatingTile)
                Destroy(floatingTile);
        }

        public void TriggerPressDown()
        {
            // draw tiles on the floor
            ClearTiles();
            coroutine = RenderTilesWithRightAxis(floor, floatingTile.transform.right);
        }

        public void TriggerPressUp()
        {
            StartCoroutine(coroutine);
        }

        public void ResetTile(ViveSR_RigidReconstructionColliderManager cld_pool)
        {
            // Reset collider pool
            //cldPool = cld_pool;

            //// Reset tile rotation
            //floor = cldPool.GetFloorCollider(ColliderShapeType.MESH_SHAPE);
            //ViveSR_StaticColliderInfo bb_cldInfo = floor.GetCorrespondingColliderOfType(ColliderShapeType.BOUND_RECT_SHAPE);
            //if (bb_cldInfo == null) return;

            //right4FloatingTile = bb_cldInfo.RectRightAxis;
            //up4FloatingTile = bb_cldInfo.GroupNormal;
            //forward4FloatingTile = Vector3.Cross(right4FloatingTile, up4FloatingTile);
            //floatingTile.transform.LookAt(forward4FloatingTile, up4FloatingTile);
        }

        public void ClearTiles()
        {
            if (coroutine != null) StopCoroutine(coroutine);
            tileMgr.RemoveAllTiles();
            //if (cldPool != null) cldPool.HideAllCollider();
        }

        public void RotateTile(float degree)
        {
            floatingTile.transform.Rotate(Vector3.up * degree, Space.Self);
        }

        IEnumerator RenderTilesWithRightAxis(ViveSR_RigidReconstructionCollider cldInfo, Vector3 right)
        {
            //Vector3 up, forward;
            targetLengthX = Prefab.transform.localScale.x;
            targetLengthY = Prefab.transform.localScale.y;
            targetLengthZ = Prefab.transform.localScale.z;

            List<Vector3> locations = new List<Vector3>();
            Quaternion outRot;
            cldInfo.GetColliderUsableLocationsWithRightAxis(targetLengthX, targetLengthZ, targetLengthY, locations, out outRot, ref right);
            List<GameObject> renderingTiles = new List<GameObject>();

            for (int i = 0; i < locations.Count; i++)
            {
                GameObject tile = Instantiate(Prefab, locations[i], outRot);
                tileMgr.AddTile(tile);
                renderingTiles.Add(tile);
            }

            for (int j = 0; j <= growFrequence; j++)
            {
                for (int i = 0; i < renderingTiles.Count; i++)
                {
                    renderingTiles[i].transform.localScale = new Vector3(targetLengthX * targetScale / growFrequence * j, targetLengthY * targetScale / growFrequence * j, targetLengthZ * targetScale / growFrequence * j);
                }
                yield return new WaitForSeconds(timeInterval / growFrequence);
            }
        }
    }
}
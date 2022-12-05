using UnityEngine;
using System.IO;
using System.Collections;
using System.Collections.Generic;

namespace Vive.Plugin.SR.Experience
{   
    [RequireComponent(typeof(ViveSR_Experience_Recons3DAssetMultiLoader))]
    public class ViveSR_Experience_SceneUnderstanding : MonoBehaviour
    {
        ViveSR_Experience_Recons3DAssetMultiLoader SemanticMeshLoader = null;
        private bool _semanticMeshIsLoading = false;
        private bool _semanticMeshIsLoaded = false;
        private bool _semanticMeshIsExporting = false;

        string semanticObj_dir = "SceneUnderstanding/";
        string semanticObj_dirPath;

        public List<GameObject> semanticList = new List<GameObject>();
        public List<ViveSR_RigidReconstructionColliderManager> semanticCldPools = new List<ViveSR_RigidReconstructionColliderManager>();

        System.Action SemanticMeshReadyCallback;

        [SerializeField] GameObject HintLocatorPrefab;
        List<GameObject> HintLocators = new List<GameObject>();

        protected string reconsRootDir = Path.GetDirectoryName(System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData)) + "\\LocalLow\\HTC Corporation\\SR_Reconstruction_Output\\Recons3DAsset/";

        public bool SemanticMeshIsLoading
        {
            get { return _semanticMeshIsLoading; }
        }

        public bool SemanticMeshIsLoaded
        {
            get { return _semanticMeshIsLoaded; }
        }

        public bool SemanticMeshIsExporting
        {
            get { return _semanticMeshIsExporting; }
        }
        private void Awake()
        {
            semanticObj_dirPath = reconsRootDir + semanticObj_dir;
            SemanticMeshLoader = GetComponent<ViveSR_Experience_Recons3DAssetMultiLoader>();
        }

        public void ActivateSemanticMesh(bool active)
        {
            foreach (GameObject go in semanticList) go.SetActive(active);
        }

        public void LoadSemanticMesh(System.Action beforeLoad = null, System.Action done = null)
        {
            if (SemanticMeshIsLoading) return;

            if (SemanticMeshIsLoaded)
                UnloadSemanticMesh();

            _semanticMeshIsLoading = true;

            if (beforeLoad != null) beforeLoad();
            SemanticMeshReadyCallback = done;

            GameObject[] semanticObjs = SemanticMeshLoader.LoadSemanticColliderObjs(semanticObj_dirPath);
            foreach (GameObject go in semanticObjs)
            {
                go.SetActive(false);
                semanticList.Add(go);
            }
            StartCoroutine(waitForSemanticMeshLoad());
        }

        public void LoadSemanticMeshByType(SceneUnderstandingObjectType type, System.Action beforeLoad = null, System.Action done = null)
        {
            if (SemanticMeshIsLoading) return;

            if (SemanticMeshIsLoaded)
                UnloadSemanticMesh();

            _semanticMeshIsLoading = true;

            if (beforeLoad != null) beforeLoad();
            SemanticMeshReadyCallback = done;

            GameObject[] semanticObjs = SemanticMeshLoader.LoadSemanticColliderObjsByType(semanticObj_dirPath, type);
            foreach (GameObject go in semanticObjs)
            {
                go.SetActive(false);
                semanticList.Add(go);
            }
            StartCoroutine(waitForSemanticMeshLoad());
        }

        public void UnloadSemanticMesh(System.Action done = null)
        {
            if (!SemanticMeshIsLoaded) return;

            foreach (GameObject go in semanticList) Destroy(go);
            semanticList.Clear();
            semanticCldPools.Clear();
            _semanticMeshIsLoaded = false;
            if (done != null) done();
        }
        public bool CheckChairExist()
        {
            return GetSegmentationInfo(SceneUnderstandingObjectType.CHAIR).Count > 0;
        }

        IEnumerator waitForSemanticMeshLoad()
        {
            while (!SemanticMeshLoader.isAllColliderReady)
            {
                yield return new WaitForEndOfFrame();
            }
            SemanticMeshReady();
        }

        private void SemanticMeshReady()
        {
            if (SemanticMeshReadyCallback != null) SemanticMeshReadyCallback();
            _semanticMeshIsLoaded = true;
            _semanticMeshIsLoading = false;
        }

        public void ExportSemanticMesh(System.Action<int> UpdatePercentage = null, System.Action done = null)
        {
            _semanticMeshIsExporting = true;
            ViveSR_SceneUnderstanding.StartExporting(ViveSR_SceneUnderstanding.DataDirectory);
            StartCoroutine(_ExportSemanticMesh(UpdatePercentage, done));
        }

        private IEnumerator _ExportSemanticMesh(System.Action<int> UpdatePercentage = null, System.Action done = null)
        {
            int percentage = 0;
            while (percentage < 100)
            {
                percentage = ViveSR_SceneUnderstanding.GetSceneUnderstandingProgress();
                if (UpdatePercentage != null) UpdatePercentage(percentage);
                yield return new WaitForEndOfFrame();
            }

            _semanticMeshIsExporting = false;
            ViveSR_RigidReconstruction.StopScanning();
            if (done != null) done();
        }

        public bool CheckSemanticMeshDirExist()
        {
            bool ret = true;
            DirectoryInfo dir = new DirectoryInfo(semanticObj_dirPath);
            if (!dir.Exists)
            {
                Debug.Log(semanticObj_dirPath + " does not exist.");
                ret = false;
            }
            else if (dir.GetFiles("*.xml").Length == 0)
            {
                Debug.Log("There is no scene object in " + semanticObj_dirPath);
                ret = false;
            }
            return ret;
        }


        public void WaitForSemanticCldPool(GameObject go, System.Action done = null)
        {
            StartCoroutine(_CoroutineWaitForSemanticCldPool(go, done));
        }

        IEnumerator _CoroutineWaitForSemanticCldPool(GameObject go, System.Action done = null)
        {
            ViveSR_RigidReconstructionColliderManager pool = go.GetComponent<ViveSR_RigidReconstructionColliderManager>();
            while (pool == null)
            {
                pool = go.GetComponent<ViveSR_RigidReconstructionColliderManager>();
                yield return new WaitForEndOfFrame();
            }
            done();
        }

        public GameObject[] GetSemanticObjects(SceneUnderstandingObjectType type)
        {
            List<GameObject> objList = new List<GameObject>();

            if (type != SceneUnderstandingObjectType.NONE)
            {
                foreach (GameObject go in semanticList)
                {
                    ViveSR_RigidReconstructionColliderManager pool = go.GetComponent<ViveSR_RigidReconstructionColliderManager>();
                    if (pool == null)
                    {
                        Debug.Log("[SemanticSegmentation] Semantic object is not loaded completely.");
                        break;
                    }
                    if (pool.GetSemanticType() == type)
                        objList.Add(go);
                }
            }
            return objList.ToArray();
        }

        public void ShowSemanticColliderByType(SceneUnderstandingObjectType type)
        {
            if (type == SceneUnderstandingObjectType.NONE) return;

            foreach (GameObject go in semanticList)
            {
                WaitForSemanticCldPool(go, () => {
                    ViveSR_RigidReconstructionColliderManager pool = go.GetComponent<ViveSR_RigidReconstructionColliderManager>();
                    if (pool.GetSemanticType() == type)
                        pool.ShowAllColliderWithPropsAndCondition(new uint[] { (uint)ColliderShapeType.MESH_SHAPE });
                });
            }
        }

        public void ShowAllSemanticCollider()
        {
            _SetSemanticColliderVisible(true);
        }

        public void HideAllSemanticCollider()
        {
            _SetSemanticColliderVisible(false);
        }

        private void _SetSemanticColliderVisible(bool isVisible)
        {
            foreach (GameObject go in semanticList)
            {
                if (go == null) continue;

                WaitForSemanticCldPool(go, () => {
                    ViveSR_RigidReconstructionColliderManager pool = go.GetComponent<ViveSR_RigidReconstructionColliderManager>();

                    if (isVisible)
                    {
                        pool.ShowAllColliderWithPropsAndCondition(new uint[] { (uint)ColliderShapeType.MESH_SHAPE });
                    }
                    else
                    {
                        pool.HideAllColliderRenderers();
                    }
                });
            }
        }

        public void SetChairSegmentationConfig(bool isOn)
        {
            ViveSR_SceneUnderstanding.SetCustomSceneUnderstandingConfig(SceneUnderstandingObjectType.CHAIR, 5, isOn);
        }


        public List<SceneUnderstandingDataReader.SceneUnderstandingObject> GetSegmentationInfo(SceneUnderstandingObjectType type)
        {
            SceneUnderstandingDataReader SceneUnderstandingDataReader = new SceneUnderstandingDataReader(semanticObj_dirPath);

            return new List<SceneUnderstandingDataReader.SceneUnderstandingObject>(SceneUnderstandingDataReader.GetElements((int)type));
        }

        public void TestSegmentationResult(System.Action<int> UpdatePercentage = null, System.Action done = null)
        {
            StartCoroutine(_TestSegmentationResult(UpdatePercentage, done));
        }
        public IEnumerator _TestSegmentationResult(System.Action<int> UpdatePercentage = null, System.Action done = null)
        {
            int percentage = 0;

            ViveSR_SceneUnderstanding.StartExporting(semanticObj_dir);

            while (percentage < 100)
            {
                percentage = ViveSR_SceneUnderstanding.GetSceneUnderstandingProgress();
                UpdatePercentage(percentage);

                yield return new WaitForEndOfFrame();
            }

            if (done != null) done();
        }

        private void OnDestroy()
        {
            SetSegmentation(false);
        }

        public void SetSegmentation(bool isOn)
        {
            ViveSR_SceneUnderstanding.EnableSceneUnderstanding(isOn, true);
        }

        public void GenerateHintLocators(List<SceneUnderstandingDataReader.SceneUnderstandingObject> SceneUnderstandingObject)
        {
            ClearHintLocators();
            for (int i = 0; i < SceneUnderstandingObject.Count; i++)
            {
                HintLocators.Add(Instantiate(HintLocatorPrefab));
                HintLocators[i].transform.position = SceneUnderstandingObject[i].position[0];
                HintLocators[i].transform.forward = SceneUnderstandingObject[i].forward;
            }
        }

        public void ClearHintLocators()
        {
            foreach (GameObject hintLocator in HintLocators) Destroy(hintLocator);
            HintLocators.Clear();
        }

    }
}
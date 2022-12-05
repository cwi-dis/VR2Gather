using System.Collections;
using UnityEngine;

namespace Vive.Plugin.SR.Experience
{
    public class ViveSR_Experience_CloudOrbit : MonoBehaviour
    {
        IEnumerator Coroutine;

        ViveSR_Experience_CloudGenerator cloudGenerator;
        int TypeNum;
        float startingAngle;
        float distance;
        float speed;
        float scale;
        float meshAngle;
        float height;
        [SerializeField] GameObject orbitingObj;
        [SerializeField] MeshFilter meshFilter;

        private void Awake()
        {
            cloudGenerator = ViveSR_Experience_CloudGenerator.instance;
            height = Random.Range(-100f, 100f);
            distance = Random.Range(((height > 20 || height < -20) ? 10f : 200f), 400f);

            if ((height > 10 || height < -10) && distance < 200f)
                speed = 1f;
            else speed = 30000 / Mathf.Pow(distance, 2);

            if (distance < 200f) scale = Random.Range(1f, 3f);
            else scale = Random.Range(5f, 10f);

            meshAngle = Random.Range(0f, 359f);
            startingAngle = Random.Range(0f, 359f);
            Coroutine = Animate();
            meshFilter.mesh = cloudGenerator.cloudMeshes[Random.Range(0, cloudGenerator.cloudMeshes.Count)];
            orbitingObj.transform.localPosition += new Vector3(0f, height, distance);
            orbitingObj.transform.localScale = new Vector3(scale, scale, scale);
            orbitingObj.transform.localEulerAngles += new Vector3(0f, meshAngle, 0f);
        }

        private void OnEnable()
        {
            StartCoroutine(Coroutine);
        }

        private void OnDisable()
        {
            StopCoroutine(Coroutine);
        }

        IEnumerator Animate()
        {
            transform.localEulerAngles += new Vector3(0f, startingAngle, 0f);
            while (true)
            {
                transform.localEulerAngles += new Vector3(0f, speed * Time.deltaTime, 0f);
                yield return new WaitForEndOfFrame();
            }
        }
    }
}

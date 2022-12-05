using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace Music2Dance1980
{
    public class DiscoFloorCreator : MonoBehaviour
    {                       
        public List<DiscoFloorColor> DiscoFloorColors;

        [SerializeField] GameObject FloorPiecePrefab;
        [SerializeField] Vector2 QuadCount;
        [SerializeField] List<Texture> FloorPatternTextures;
        [SerializeField] Material FloorMaterialPrefab;

        [SerializeField] bool isAnimated;
        [SerializeField] float AnimationInterval = 3f;
        float lastTimeColorChanged;

        [SerializeField]
        List<DiscoFloor> DiscoFloors;

        Vector3 targetScale;

        // Use this for initialization
        void Start()
        {
            targetScale = transform.localScale;
            transform.localScale = Vector3.one;
            for (int x = 0; x < QuadCount.x; ++x)
            {
                for (int y = 0; y < QuadCount.y; ++y)
                {
                    DiscoFloor discoFloor = Instantiate(FloorPiecePrefab, transform).GetComponent<DiscoFloor>();
                    discoFloor.gameObject.transform.localPosition = new Vector3(x - (QuadCount.x - 1) * 0.5f, 0, y - (QuadCount.y - 1) * 0.5f);
                    discoFloor.CreateMaterial(FloorMaterialPrefab, FloorPatternTextures[UnityEngine.Random.Range(0, FloorPatternTextures.Count)], DiscoFloorColors);
                    DiscoFloors.Add(discoFloor);
                }
            }

            Destroy(FloorPiecePrefab);

            transform.localScale = targetScale;
        }

        // Update is called once per frame
        void Update()
        {
            if (!isAnimated) return;

            if (Time.timeSinceLevelLoad - lastTimeColorChanged > AnimationInterval)
            {
                lastTimeColorChanged = Time.timeSinceLevelLoad;

                foreach (DiscoFloorColor discoFloorColor in DiscoFloorColors)
                {
                    discoFloorColor.IsEmissionOn = UnityEngine.Random.Range(0, 2) == 0;
                }

                foreach (DiscoFloor discoFloor in DiscoFloors)
                {

                    discoFloor.ChangeTexture(FloorPatternTextures[UnityEngine.Random.Range(0, FloorPatternTextures.Count)]);

                    discoFloor.ChangeEmission(DiscoFloorColors);
                }
            }
        }
    }

    [Serializable]
    public class DiscoFloorColor
    {
        public bool IsEmissionOn;
        public Color Color;
    }                            
}
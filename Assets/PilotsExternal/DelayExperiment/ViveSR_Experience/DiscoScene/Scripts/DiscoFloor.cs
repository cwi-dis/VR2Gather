using System.Collections;
using System.Collections.Generic;
using UnityEngine;   

namespace Music2Dance1980
{
    public class DiscoFloor : MonoBehaviour
    {         
        [SerializeField] Renderer rnd;
        List<Color> _colors = new List<Color>();
        List<float> _isEmissionOn = new List<float>();

        public void CreateMaterial(Material material, Texture texture, List<DiscoFloorColor> discoFloorColors)
        {
            rnd.material = new Material(material);

            rnd.material.mainTexture = texture;

            int colorCount = (discoFloorColors.Count <= 5) ? discoFloorColors.Count : 5;

            foreach (DiscoFloorColor c in discoFloorColors)
            {
                _colors.Add(c.Color);
                _isEmissionOn.Add(c.IsEmissionOn ? 1f: 0f);
            }
             
            rnd.material.SetInt("_ColorCount", colorCount);
            rnd.material.SetColorArray("_Colors", _colors);
            rnd.material.SetFloatArray("_IsEmissionOn", _isEmissionOn);
        }

        public void ChangeTexture(Texture texture)
        {
            rnd.material.mainTexture = texture;
        }
        public void ChangeEmission(List<DiscoFloorColor> discoFloorColors)
        {
            _isEmissionOn.Clear();
            
            int a = Random.Range(0, discoFloorColors.Count);
            int b = Random.Range(0, discoFloorColors.Count);
            
            for (int i = 0; i < discoFloorColors.Count; ++i)
                _isEmissionOn.Add((i == a || i == b) ? 1f : 0f);

            rnd.material.SetFloatArray("_IsEmissionOn", _isEmissionOn);
        }
    }
}
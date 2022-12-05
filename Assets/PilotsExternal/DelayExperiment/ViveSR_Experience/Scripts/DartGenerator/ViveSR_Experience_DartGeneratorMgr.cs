using System.Collections.Generic;
using UnityEngine;

namespace Vive.Plugin.SR.Experience
{
    public enum DartPlacementMode
    {
        Throwable = 0,
        Raycast,  
        MaxNum   
    }

    public class ViveSR_Experience_DartGeneratorMgr : MonoBehaviour
    {
        [SerializeField] bool AllowSwitchingTool = true;
        [SerializeField] bool AutoEnable = true;

        public DartPlacementMode dartPlacementMode;
        [SerializeField] List<ViveSR_Experience_IDartGenerator> _dartGenerators;
        public Dictionary<DartPlacementMode, ViveSR_Experience_IDartGenerator> DartGenerators = new Dictionary<DartPlacementMode, ViveSR_Experience_IDartGenerator>();

        [HideInInspector] public readonly float coolDownTime = 0.2f;
        [HideInInspector] public float tempTime;

        private void Awake()
        {
            _dartGenerators.AddRange(GetComponents<ViveSR_Experience_IDartGenerator>());

            for (int i = 0; i < _dartGenerators.Count; i++)
            {
                if(_dartGenerators[i].GetType() == typeof(ViveSR_Experience_DartThrowGenerator)) DartGenerators[DartPlacementMode.Throwable] = _dartGenerators[i];
                else if (_dartGenerators[i].GetType() == typeof(ViveSR_Experience_DartRaycastGenerator)) DartGenerators[DartPlacementMode.Raycast] = _dartGenerators[i];
            }

            if (AutoEnable) DartGenerators[dartPlacementMode].enabled = true;
        }

        public void SwitchPlacementMode()
        {
            if(AllowSwitchingTool)
            {
                ViveSR_Experience_IDartGenerator oldDartGenerator = DartGenerators[dartPlacementMode];
                oldDartGenerator.TriggerRelease();

                GameObject lastObj = oldDartGenerator.InstantiatedDarts[oldDartGenerator.InstantiatedDarts.Count - 1];
                Destroy(lastObj);

                oldDartGenerator.enabled = false;

                //switch to the other DartGenerator
                dartPlacementMode = (DartPlacementMode)(((int)dartPlacementMode + 1) % (int)DartPlacementMode.MaxNum);

                ViveSR_Experience_IDartGenerator newDartGenerator = DartGenerators[dartPlacementMode];
                newDartGenerator.enabled = true;
                newDartGenerator.TriggerPress();
            }
        }

        public void DestroyObjs() 
        {
            foreach (ViveSR_Experience_IDartGenerator dartGenerator in _dartGenerators)
            {
                dartGenerator.DestroyObjs();
            }
        }     
    }
}
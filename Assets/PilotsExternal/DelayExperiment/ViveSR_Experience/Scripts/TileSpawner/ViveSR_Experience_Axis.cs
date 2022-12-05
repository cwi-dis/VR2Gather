using UnityEngine;

namespace Vive.Plugin.SR.Experience
{
    public class ViveSR_Experience_Axis : MonoBehaviour {

        enum AxisType {
            Right,
            Up,
            Forward
        };
        
        [SerializeField] AxisType axis;

        float axisLength = 0.05f;
        float axisDistance = 0.00f;

        LineRenderer line = null;
        Transform parent;

        // Use this for initialization
        void Start()
        {
            parent = gameObject.transform.parent.transform;

            line = gameObject.GetComponent<LineRenderer>();
            line.material = new Material(Shader.Find("Particles/Additive"));
            SetColor();
            
        }

        // Update is called once per frame
        void Update()
        {
            Vector3 pos = parent.position + Vector3.up * (parent.localScale.y + axisDistance);

            line.SetPosition(0, pos);
            line.SetPosition(1, pos + GetOrientation() * axisLength);
        }

        void SetColor()
        {
            line.startColor = Color.white;

            if (axis == AxisType.Right)
                line.endColor = Color.red;
            else if(axis == AxisType.Up)
                line.endColor = Color.green;
            else
                line.endColor = Color.blue;
        }

        Vector3 GetOrientation()
        {
            if (axis == AxisType.Right)
                return parent.right;
            else if (axis == AxisType.Up)
                return parent.up;
            else
                return parent.forward;
        }
    }
}
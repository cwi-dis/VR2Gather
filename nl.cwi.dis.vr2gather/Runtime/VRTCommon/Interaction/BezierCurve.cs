using UnityEngine;

namespace VRT.Pilots.Common
{
    /// <summary>
    /// Draws a bezier curve from a starting point transform to an end point transform.
    /// </summary>
    public class BezierCurve : MonoBehaviour
    {
        /// <summary>
        /// If the view scale changes more than this amount, then the line width will be updated causing the line to be rebuilt.
        /// </summary>
        const float k_ViewerScaleChangeThreshold = 0.1f;

        public enum UpdateType
        {
            UpdateAndBeforeRender,
            Update,
            BeforeRender,
        }

#pragma warning disable 649
        [SerializeField, Tooltip("The time within the frame that the curve will be updated.")]
        UpdateType m_UpdateTrackingType = UpdateType.Update;

        [SerializeField, Tooltip("The transform that determines the position and handle of the start point.")]
        Transform m_StartPoint;

        [SerializeField, Tooltip("The transform that determines the position and handle of the end point.")]
        Transform m_EndPoint;

        [SerializeField, Tooltip("Controls the scale factor of the curve's start bezier handle.")]
        float m_CurveFactorStart = 1.0f;

        [SerializeField, Tooltip("Controls the scale factor of the curve's end bezier handle.")]
        float m_CurveFactorEnd = 1.0f;

        [SerializeField, Tooltip("Controls the number of segments used to draw the curve.")]
        int m_SegmentCount = 50;

        [SerializeField, Tooltip("When enabled, the line color gradient will be animated so that an opaque part travels along the line.")]
        bool m_Animate;

        [SerializeField, Tooltip("If animated, this controls the speed of the animation.")]
        float m_AnimSpeed = 0.25f;

        [SerializeField, Tooltip("If animated, this color will be the main opaque color of the gradient.")]
        Color m_GradientKeyColor = new Color(0.1254902f, 0.5882353f, 0.9529412f);

        [SerializeField, Tooltip("The line renderer that will draw the curve. If not set it will find a line renderer on this GameObject.")]
        LineRenderer m_LineRenderer;
#pragma warning restore 649

        Vector3[] m_ControlPoints = new Vector3[4];
        float m_Time;
        float m_LineWidth;
        float m_LastViewerScale;

        Vector3 m_LastStartPosition;
        Vector3 m_LastEndPosition;

        void Awake()
        {
            if (m_LineRenderer == null)
                m_LineRenderer = GetComponent<LineRenderer>();

            m_LineWidth = m_LineRenderer.startWidth;
        }

        void OnEnable()
        {
            DrawCurve();
            Application.onBeforeRender += OnBeforeRender;
        }

        void OnDisable()
        {
            Application.onBeforeRender -= OnBeforeRender;
        }

        void OnBeforeRender()
        {
            if (m_UpdateTrackingType == UpdateType.BeforeRender || m_UpdateTrackingType == UpdateType.UpdateAndBeforeRender)
                DrawCurve();
        }

        void Update()
        {
            if (m_UpdateTrackingType == UpdateType.Update || m_UpdateTrackingType == UpdateType.UpdateAndBeforeRender)
                DrawCurve();

            if (m_Animate)
                AnimateCurve();
        }

        [ContextMenu("Draw")]
        public void DrawCurve()
        {
            var startPointPosition = m_StartPoint.position;
            var endPointPosition = m_EndPoint.position;

            if (startPointPosition == m_LastStartPosition &&
                endPointPosition == m_LastEndPosition)
                return;

            var dist = Vector3.Distance(startPointPosition, endPointPosition);

            m_ControlPoints[0] = startPointPosition;
            m_ControlPoints[1] = startPointPosition + (m_StartPoint.right * (dist * m_CurveFactorStart));
            m_ControlPoints[2] = endPointPosition - (m_EndPoint.right * (dist * m_CurveFactorEnd));
            m_ControlPoints[3] = endPointPosition;

            int segmentCount;
            const float smallestCurveLength = 0.0125f;
            if (Vector3.Distance(startPointPosition, endPointPosition) < (smallestCurveLength * m_LastViewerScale))
                segmentCount = 2;
            else
                segmentCount = m_SegmentCount;

            m_LineRenderer.positionCount = segmentCount + 1;
            m_LineRenderer.SetPosition(0, m_ControlPoints[0]);
            for (var i = 1; i <= segmentCount; i++)
            {
                var t = i / (float)segmentCount;
                var pixel = CalculateCubicBezierPoint(t, m_ControlPoints[0], m_ControlPoints[1], m_ControlPoints[2], m_ControlPoints[3]);
                m_LineRenderer.SetPosition(i, pixel);
            }

            m_LastStartPosition = startPointPosition;
            m_LastEndPosition = endPointPosition;
        }

        static Vector3 CalculateCubicBezierPoint(float t, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3)
        {
            var u = 1 - t;
            var tt = t * t;
            var uu = u * u;
            var uuu = uu * u;
            var ttt = tt * t;

            var p = uuu * p0;
            p += 3 * uu * t * p1;
            p += 3 * u * tt * p2;
            p += ttt * p3;

            return p;
        }

        void AnimateCurve()
        {
            var newGrad = new Gradient();

            var colorKeys = new GradientColorKey[1];
            var alphaKeys = new GradientAlphaKey[2];

            colorKeys[0] = new GradientColorKey(m_GradientKeyColor, 0f);
            alphaKeys[0] = new GradientAlphaKey(.25f, m_Time);
            alphaKeys[1] = new GradientAlphaKey(1f, 1f);

            newGrad.SetKeys(colorKeys, alphaKeys);
            newGrad.mode = GradientMode.Blend;

            m_LineRenderer.colorGradient = newGrad;
            m_Time += (Time.unscaledDeltaTime * m_AnimSpeed);

            if (m_Time >= 1f)
                m_Time = 0f;
        }
    }
}

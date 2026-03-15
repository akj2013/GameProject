using UnityEngine;

namespace WoodLand3D.StarShip
{
    [RequireComponent(typeof(LineRenderer))]
    public class StarShipRouteLine : MonoBehaviour
    {
        [Header("?? ?? ?????")]
        public Transform startPoint;   // ????
        public Transform endPoint;     // ?? (??? ???? ??��)

        [Header("?? ???")]
        [Tooltip("????? ??? ???? ????? (??? ????)")]
        public float curveHeight = 2.0f;

        [Tooltip("?��?? ??? ??? ????")]
        public float sideOffset = 0.5f;

        [Tooltip("???? ??? ??? ?????? (???? ????? ?? ?��???)")]
        [Range(2, 64)]
        public int segmentCount = 24;

        [Header("???? ?��?")]
        public float lineWidth = 0.15f;

        [Header("???? ?????")]
        public float scrollSpeed = 1.5f;

        [Header("??? ???")]
        public float pulseSpeed = 2f;
        public float pulseMin = 0.6f;
        public float pulseMax = 1.2f;

        private LineRenderer _lr;
        private Material _matInstance;

        private void Awake()
        {
            _lr = GetComponent<LineRenderer>();
            _lr.widthMultiplier = lineWidth;
            _lr.numCapVertices = 8;
            _lr.numCornerVertices = 4;
            _lr.textureMode = LineTextureMode.Tile;
            _lr.useWorldSpace = true;

            _matInstance = _lr.material; // ?��????
        }

        private void OnDestroy()
        {
            if (_matInstance != null)
                Destroy(_matInstance);
        }

        /// <summary>
        /// ?? ?? t(0~1) ????? ???? ????? ???. ??? ??????/VFX???? ?? ?????? ????? ???.
        /// </summary>
        public Vector3 EvaluatePoint(float t)
        {
            if (startPoint == null || endPoint == null)
                return transform.position;

            t = Mathf.Clamp01(t);
            GetBezierControlPoints(out Vector3 p0, out Vector3 p1, out Vector3 p2, out Vector3 p3);
            return BezierPoint(t, p0, p1, p2, p3);
        }

        /// <summary>
        /// ?? ?? t(0~1) ????????? ???? ????(?????). ???? ????????? ?????????? ????.
        /// </summary>
        public Vector3 EvaluateTangent(float t)
        {
            if (startPoint == null || endPoint == null)
                return transform.forward;

            t = Mathf.Clamp01(t);
            GetBezierControlPoints(out Vector3 p0, out Vector3 p1, out Vector3 p2, out Vector3 p3);

            // Cubic Bezier ?????: B'(t) = 3(1-t)^2(p1-p0) + 6(1-t)t(p2-p1) + 3t^2(p3-p2)
            float u = 1f - t;
            Vector3 tangent =
                3f * u * u * (p1 - p0) +
                6f * u * t * (p2 - p1) +
                3f * t * t * (p3 - p2);

            if (tangent.sqrMagnitude >= 0.0001f)
                return tangent.normalized;

            // ?????? ???? 0?? ???(???? ??) fallback
            Vector3 fallback = (endPoint.position - startPoint.position);
            if (fallback.sqrMagnitude >= 0.0001f)
                return fallback.normalized;
            return transform.forward;
        }

        /// <summary>
        /// ???? ????(startPoint, endPoint, curveHeight, sideOffset)???? ?????? ????? ????? 4?? ???.
        /// </summary>
        private void GetBezierControlPoints(out Vector3 p0, out Vector3 p1, out Vector3 p2, out Vector3 p3)
        {
            p0 = startPoint.position;
            p3 = endPoint.position;

            Vector3 diff = p3 - p0;
            Vector3 dir = diff.sqrMagnitude > 0.0001f ? diff.normalized : Vector3.forward;

            Vector3 side = Vector3.Cross(dir, Vector3.up);
            if (side.sqrMagnitude < 0.0001f)
                side = Vector3.right;
            side.Normalize();

            p1 = Vector3.Lerp(p0, p3, 0.33f)
                 + Vector3.up * curveHeight
                 + side * sideOffset;

            p2 = Vector3.Lerp(p0, p3, 0.66f)
                 + Vector3.up * (curveHeight * 0.6f)
                 - side * sideOffset * 0.5f;
        }

        /// <summary>
        /// Cubic Bezier B(t) = (1-t)^3 p0 + 3(1-t)^2 t p1 + 3(1-t) t^2 p2 + t^3 p3
        /// </summary>
        private static Vector3 BezierPoint(float t, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3)
        {
            float u = 1f - t;
            float uu = u * u;
            float tt = t * t;
            return uu * u * p0 + 3f * uu * t * p1 + 3f * u * tt * p2 + tt * t * p3;
        }

        private void Update()
        {
            if (startPoint == null || endPoint == null || _lr == null)
                return;

            if (segmentCount < 2) segmentCount = 2;

            _lr.widthMultiplier = lineWidth;

            // ���׸�Ʈ ������ŭ EvaluatePoint�� ���ø�
            int count = segmentCount + 1;
            _lr.positionCount = count;

            for (int i = 0; i < count; i++)
            {
                float t = i / (float)segmentCount;
                _lr.SetPosition(i, EvaluatePoint(t));
            }

            // ??????? ????? & ???
            if (_matInstance == null)
                return;

            Vector2 offset = _matInstance.mainTextureOffset;
            offset.x -= Time.deltaTime * scrollSpeed;
            _matInstance.mainTextureOffset = offset;

            float s = (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f;
            float intensity = Mathf.Lerp(pulseMin, pulseMax, s);
            Color c = new Color(intensity, intensity, intensity * 0.9f, 1f);
            _lr.startColor = c;
            _lr.endColor = c;
        }
    }
}

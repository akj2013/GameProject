using System.Collections.Generic;
using UnityEngine;

namespace WoodLand3D.StarShip
{
    /// <summary>
    /// StarShipRouteLine 이 그리는 곡선을 따라 여러 개의 파티클 Emitter를 배치해 주는 스크립트.
    /// - targetBeam 의 startPoint / endPoint / curveHeight / sideOffset 값을 그대로 사용한다.
    /// - emitters 리스트에 등록된 Transform 들을 곡선 위 0~1 구간의 t 범위(startT~endT)에 균등 분포시킨다.
    /// - 각 Emitter 의 forward 방향도 곡선의 접선 방향을 향하도록 맞춰 파티클이 빔을 따라 흐르는 느낌을 준다.
    /// </summary>
    public class BeamSparkAlongCurve : MonoBehaviour
    {
        [Header("기준 빔 (필수)")]
        public StarShipRouteLine targetBeam;

        [Header("이 곡선을 따라 움직일 Emitter들")]
        public List<Transform> emitters = new List<Transform>();

        [Header("곡선에서 사용할 t 범위 (0~1)")]
        [Range(0f, 1f)] public float startT = 0.1f;
        [Range(0f, 1f)] public float endT = 0.9f;

        /// <summary>
        /// 접선 계산 시 사용할 작은 델타 t. targetBeam.segmentCount 가 크면 자동으로 더 촘촘하게 잡힌다.
        /// </summary>
        private float TangentDeltaT
        {
            get
            {
                // segmentCount 가 0 이거나 너무 작을 때를 대비해 최소 분모를 8 정도로 잡는다.
                int segCount = (targetBeam != null && targetBeam.segmentCount > 0)
                    ? targetBeam.segmentCount
                    : 8;
                return 1f / (segCount * 2f);
            }
        }

        private void LateUpdate()
        {
            if (targetBeam == null ||
                targetBeam.startPoint == null ||
                targetBeam.endPoint == null)
                return;

            if (emitters == null || emitters.Count == 0)
                return;

            // StarShipRouteLine 이 사용하는 것과 동일한 베지어 컨트롤 포인트를 계산한다.
            Vector3 p0 = targetBeam.startPoint.position;
            Vector3 p3 = targetBeam.endPoint.position;

            Vector3 dir = (p3 - p0).sqrMagnitude > 0.0001f
                ? (p3 - p0).normalized
                : Vector3.forward;

            // dir 이 위쪽과 거의 평행할 때를 대비해 side 벡터를 안전하게 계산
            Vector3 side = Vector3.Cross(dir, Vector3.up);
            if (side.sqrMagnitude < 0.0001f)
                side = Vector3.right;
            side.Normalize();

            Vector3 p1 = Vector3.Lerp(p0, p3, 0.33f)
                        + Vector3.up * targetBeam.curveHeight
                        + side * targetBeam.sideOffset;

            Vector3 p2 = Vector3.Lerp(p0, p3, 0.66f)
                        + Vector3.up * (targetBeam.curveHeight * 0.6f)
                        - side * targetBeam.sideOffset * 0.5f;

            int count = emitters.Count;
            if (count == 0)
                return;

            float clampedStartT = Mathf.Clamp01(startT);
            float clampedEndT = Mathf.Clamp01(endT);
            if (clampedEndT < clampedStartT)
            {
                float tmp = clampedStartT;
                clampedStartT = clampedEndT;
                clampedEndT = tmp;
            }

            float deltaTForTangent = TangentDeltaT;

            for (int i = 0; i < count; i++)
            {
                Transform emitter = emitters[i];
                if (emitter == null)
                    continue;

                // emitters 가 1개일 때도 중앙에 배치되도록 보정
                float lerp = (count == 1) ? 0.5f : i / (float)(count - 1);
                float t = Mathf.Lerp(clampedStartT, clampedEndT, lerp);

                Vector3 pos = GetBezierPoint(t, p0, p1, p2, p3);
                emitter.position = pos;

                // 접선 방향으로 forward 를 맞춘다.
                float t2 = (t + deltaTForTangent <= 1f)
                    ? t + deltaTForTangent
                    : t - deltaTForTangent;
                t2 = Mathf.Clamp01(t2);
                Vector3 pos2 = GetBezierPoint(t2, p0, p1, p2, p3);
                Vector3 tangent = (pos2 - pos).normalized;

                if (tangent.sqrMagnitude > 0.0001f)
                {
                    emitter.rotation = Quaternion.LookRotation(tangent, Vector3.up);
                }
            }
        }

        private static Vector3 GetBezierPoint(float t, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3)
        {
            float u = 1f - t;
            float uu = u * u;
            float tt = t * t;

            // Cubic Bezier: B(t) = (1-t)^3 p0 + 3(1-t)^2 t p1 + 3(1-t) t^2 p2 + t^3 p3
            return
                uu * u * p0 +
                3f * uu * t * p1 +
                3f * u * tt * p2 +
                tt * t * p3;
        }
    }
}


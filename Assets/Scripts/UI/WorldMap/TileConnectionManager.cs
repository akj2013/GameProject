using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace WoodLand3D.WorldMap
{
    /// <summary>
    /// 두 타일 슬롯 사이를 하얀 곡선 실선으로 연결해 주는 매니저.
    /// - World Space Canvas 상의 RectTransform 위치를 이용해 LineRenderer 들을 갱신한다.
    /// - 한 번 만들어진 연결선은 명시적으로 지우지 않는 한 유지된다.
    /// - 새로운 연결이 생성될 때마다 "떠다니는 통나무(Floating_Woods)" 연출과 +3 팝업을 재생한다.
    /// </summary>
    public class TileConnectionManager : MonoBehaviour
    {
        /// <summary>전역에서 접근하기 위한 싱글턴 인스턴스</summary>
        public static TileConnectionManager Instance { get; private set; }

        [Header("라인 공통 설정")]
        [Tooltip("라인에 사용할 머티리얼 (없으면 Sprites/Default 로 생성)")]
        [SerializeField] private Material lineMaterial;

        [Tooltip("라인 두께")]
        [SerializeField] private float lineWidth = 50f;

        [Tooltip("기본 곡선 높이 (두 타일의 중간에서 얼마나 위로 휘어지는지)")]
        [SerializeField] private float baseCurveHeight = 40f;

        [Tooltip("부유 애니메이션 진폭")]
        [SerializeField] private float floatAmplitude = 10f;

        [Tooltip("부유 애니메이션 속도")]
        [SerializeField] private float floatSpeed = 1.2f;

        [Header("연결 연출 (Floating_Woods)")]
        [Tooltip("연결선을 따라 이동할 통나무 스프라이트 (Floating_Woods)")]
        [SerializeField] private Sprite floatingWoodsSprite;

        [Tooltip("통나무 UI의 기본 크기 (width, height)")]
        [SerializeField] private Vector2 woodsSize = new Vector2(96f, 64f);

        [Tooltip("통나무 스케일 변화 범위 (작음 → 큼)")]
        [SerializeField] private Vector2 woodsScaleRange = new Vector2(0.8f, 1.25f);

        [Tooltip("통나무가 A → B 로 이동하는 데 걸리는 시간(초)")]
        [SerializeField] private float woodsTravelDuration = 1.5f;

        [Tooltip("통나무 이동 경로의 추가 곡선 높이 (라인보다 얼마나 더 위로 떠다니는지)")]
        [SerializeField] private float woodsExtraCurveHeight = 20f;

        [Tooltip("도착 시 B 위에 표시할 +값 (예: 3 → \"+3\")")]
        [SerializeField] private int gainAmountOnArrive = 3;

        [Tooltip("획득 텍스트가 위로 떠오르며 사라지는 높이")]
        [SerializeField] private float gainTextRiseHeight = 60f;

        [Tooltip("획득 텍스트가 유지되는 시간(초)")]
        [SerializeField] private float gainTextDuration = 1.2f;

        [Tooltip("한 연결선에서 Floating_Woods 연출이 반복되는 간격(초)")]
        [SerializeField] private float woodsRepeatInterval = 4f;

        [Header("통나무 주변 반짝이 (Spark01 등)")]
        [Tooltip("통나무 이동 중에 함께 나올 반짝이 스프라이트")]
        [SerializeField] private Sprite sparkleSprite;

        [Tooltip("반짝이 UI 크기 (width, height)")]
        [SerializeField] private Vector2 sparkleSize = new Vector2(32f, 32f);

        [Tooltip("반짝이 생성 간격(초)")]
        [SerializeField] private float sparkleSpawnInterval = 0.15f;

        /// <summary>
        /// 내부에서 관리하는 하나의 연결선 정보.
        /// </summary>
        private class Connection
        {
            public TileSlotUI from;
            public TileSlotUI to;
            public LineRenderer line;
            public float phaseOffset;
            public float nextFxTime;
        }

        private readonly List<Connection> _connections = new List<Connection>(16);

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (lineMaterial == null)
            {
                var shader = Shader.Find("Sprites/Default");
                if (shader != null)
                    lineMaterial = new Material(shader) { name = "TileConnectionLineMat" };
            }
        }

        /// <summary>
        /// 두 슬롯 사이에 새로운 연결선을 추가한다.
        /// - 같은 쌍이 이미 추가되어 있다면 중복 추가하지 않는다.
        /// </summary>
        public void ShowConnection(TileSlotUI from, TileSlotUI to)
        {
            if (from == null || to == null || from == to)
                return;

            // 이미 같은 쌍이 연결되어 있다면 패스 (순서 무관)
            foreach (var c in _connections)
            {
                if ((c.from == from && c.to == to) || (c.from == to && c.to == from))
                    return;
            }

            var go = new GameObject($"Connection_{from.SlotIndex}_{to.SlotIndex}");
            go.transform.SetParent(transform, false);

            var lr = go.AddComponent<LineRenderer>();
            // LineRenderer 공통 스타일을 여기에서 통일해서 적용한다.
            lr.useWorldSpace = true;
            lr.positionCount = 3;
            lr.widthMultiplier = lineWidth;
            // Width 曲線: 시작이 두껍고 끝으로 갈수록 가늘어지도록 설정
            lr.widthCurve = new AnimationCurve(
                new Keyframe(0f, 1f),     // t=0, 가장 두꺼움
                new Keyframe(0.5f, 0.5f), // 중간에서 절반 정도
                new Keyframe(1f, 0.2f)    // 끝에서 가늘게
            );
            lr.startColor = Color.white;
            lr.endColor = Color.white;
            lr.alignment = LineAlignment.View;
            lr.textureMode = LineTextureMode.Stretch;
            lr.numCornerVertices = 4;
            lr.numCapVertices = 4;
            if (lineMaterial != null)
                lr.material = lineMaterial;

            var conn = new Connection
            {
                from = from,
                to = to,
                line = lr,
                phaseOffset = Random.Range(0f, Mathf.PI * 2f),
                // 처음 한 번은 바로 재생하고, 이후에는 woodsRepeatInterval 마다 반복
                nextFxTime = Time.time + woodsRepeatInterval
            };

            _connections.Add(conn);

            // 즉시 한 번 위치 갱신
            UpdateConnection(conn, 0f);

            // 연결이 새로 만들어질 때 통나무 + +3 연출을 한 번 재생
            if (floatingWoodsSprite != null)
                StartCoroutine(PlayFloatingWoodsAndGainText(from, to));
        }

        /// <summary>
        /// (필요하면) 모든 연결선을 지우고 싶을 때 호출.
        /// 현재 로직에서는 사용하지 않지만, 나중을 위해 남겨둔다.
        /// </summary>
        public void ClearAllConnections()
        {
            foreach (var c in _connections)
            {
                if (c.line != null)
                    Destroy(c.line.gameObject);
            }
            _connections.Clear();
        }

        private void Update()
        {
            if (_connections.Count == 0)
                return;

            float time = Time.time * floatSpeed;

            // 역방향 루프로, 슬롯이 파괴된 연결은 정리
            for (int i = _connections.Count - 1; i >= 0; i--)
            {
                var c = _connections[i];
                if (c.from == null || c.to == null || c.line == null)
                {
                    if (c.line != null)
                        Destroy(c.line.gameObject);
                    _connections.RemoveAt(i);
                    continue;
                }

                UpdateConnection(c, time);

                // 일정 시간마다 이 연결선에 대해 Floating_Woods 연출을 반복 재생
                if (floatingWoodsSprite != null && Time.time >= c.nextFxTime)
                {
                    StartCoroutine(PlayFloatingWoodsAndGainText(c.from, c.to));
                    c.nextFxTime = Time.time + woodsRepeatInterval;
                }
            }
        }

        /// <summary>
        /// 하나의 연결선 위치를 갱신한다.
        /// </summary>
        private void UpdateConnection(Connection c, float time)
        {
            var fromRt = c.from.GetComponent<RectTransform>();
            var toRt = c.to.GetComponent<RectTransform>();
            if (fromRt == null || toRt == null)
                return;

            Vector3 p0 = fromRt.position;
            Vector3 p2 = toRt.position;
            Vector3 mid = (p0 + p2) * 0.5f;

            // 위쪽으로 휘어지면서 살짝 부유하는 곡선
            float floatOffset = Mathf.Sin(time + c.phaseOffset) * floatAmplitude;
            Vector3 up = Vector3.up * (baseCurveHeight + floatOffset);
            Vector3 p1 = mid + up;

            c.line.positionCount = 3;
            c.line.SetPosition(0, p0);
            c.line.SetPosition(1, p1);
            c.line.SetPosition(2, p2);
        }

        /// <summary>
        /// A → B 연결선이 생겼을 때,
        /// Floating_Woods 스프라이트가 곡선을 따라 천천히 이동하고,
        /// 도착 시 B 위에 +3 텍스트가 떠오르며 사라지는 연출을 재생한다.
        /// </summary>
        private IEnumerator PlayFloatingWoodsAndGainText(TileSlotUI from, TileSlotUI to)
        {
            if (from == null || to == null || floatingWoodsSprite == null)
                yield break;

            var fromRt = from.GetComponent<RectTransform>();
            var toRt = to.GetComponent<RectTransform>();
            if (fromRt == null || toRt == null)
                yield break;

            // 통나무 이미지 생성
            var woodsGo = new GameObject("FloatingWoodsFx");
            woodsGo.transform.SetParent(transform, false);
            var woodsRt = woodsGo.AddComponent<RectTransform>();
            woodsRt.sizeDelta = woodsSize;
            woodsRt.localScale = Vector3.one * woodsScaleRange.x;

            var woodsImg = woodsGo.AddComponent<UnityEngine.UI.Image>();
            woodsImg.sprite = floatingWoodsSprite;
            woodsImg.color = Color.white;
            woodsImg.raycastTarget = false;

            // 통나무 이동 중에 반짝이를 주기적으로 생성하기 위한 타이머
            float nextSparkleTime = 0f;

            float t = 0f;
            while (t < woodsTravelDuration)
            {
                t += Time.deltaTime;
                float u = Mathf.Clamp01(t / woodsTravelDuration);

                // A→B 곡선을 따라 이동 (라인보다 약간 더 위로)
                Vector3 p0 = fromRt.position;
                Vector3 p2 = toRt.position;
                Vector3 mid = (p0 + p2) * 0.5f;
                float extraFloat = Mathf.Sin((Time.time + u) * floatSpeed) * floatAmplitude;
                Vector3 up = Vector3.up * (baseCurveHeight + woodsExtraCurveHeight + extraFloat);
                Vector3 p1 = mid + up;

                // 단순 3점 베지어 보간
                Vector3 a = Vector3.Lerp(p0, p1, u);
                Vector3 b = Vector3.Lerp(p1, p2, u);
                Vector3 pos = Vector3.Lerp(a, b, u);

                woodsRt.position = pos;

                // 정규분포 느낌의 스케일 곡선 (작음 → 중간에 가장 큼 → 다시 작아짐)
                // 여기서는 간단히 sin(pi * u)를 사용해 0→1→0 곡선을 만든 뒤,
                // woodsScaleRange 로 보간한다.
                float bell = Mathf.Sin(Mathf.PI * u); // 0~1~0
                float scale = Mathf.Lerp(woodsScaleRange.x, woodsScaleRange.y, bell);
                woodsRt.localScale = new Vector3(scale, scale, 1f);

                // 통나무 주변 반짝이 스폰
                if (sparkleSprite != null && t >= nextSparkleTime)
                {
                    SpawnSparkleAt(pos);
                    nextSparkleTime = t + sparkleSpawnInterval;
                }

                yield return null;
            }

            // 도착 후 통나무는 비활성화/삭제
            Destroy(woodsGo);

            // B 위에서 +3 텍스트 연출
            yield return StartCoroutine(PlayGainTextAt(toRt));
        }

        /// <summary>
        /// 주어진 월드 위치에 작은 반짝이 스프라이트를 잠깐 보여주고 사라지게 만든다.
        /// Floating_Woods 가 지나가면서 남기는 잔상 느낌.
        /// </summary>
        private void SpawnSparkleAt(Vector3 worldPos)
        {
            if (sparkleSprite == null)
                return;

            var go = new GameObject("SparkleFx");
            go.transform.SetParent(transform, false);
            var rt = go.AddComponent<RectTransform>();
            rt.position = worldPos;
            rt.sizeDelta = sparkleSize;

            var img = go.AddComponent<UnityEngine.UI.Image>();
            img.sprite = sparkleSprite;
            img.color = Color.white;
            img.raycastTarget = false;

            // 짧게 페이드 아웃시키는 코루틴 실행
            StartCoroutine(FadeAndDestroySparkle(img, 0.4f));
        }

        private IEnumerator FadeAndDestroySparkle(UnityEngine.UI.Image img, float duration)
        {
            if (img == null)
                yield break;

            float t = 0f;
            var rt = img.rectTransform;
            Vector3 startScale = rt.localScale;

            while (t < duration)
            {
                t += Time.deltaTime;
                float u = Mathf.Clamp01(t / duration);

                // 살짝 커졌다가 사라지는 느낌
                float scale = Mathf.Lerp(1f, 1.3f, u);
                rt.localScale = startScale * scale;

                var c = img.color;
                c.a = 1f - u;
                img.color = c;

                yield return null;
            }

            if (img != null)
                Destroy(img.gameObject);
        }

        /// <summary>
        /// 지정된 RectTransform 위에서 "+N" 텍스트가 떠오르며 사라지는 연출.
        /// </summary>
        private IEnumerator PlayGainTextAt(RectTransform target)
        {
            if (target == null)
                yield break;

            var go = new GameObject("GainTextFx");
            go.transform.SetParent(transform, false);
            var rt = go.AddComponent<RectTransform>();
            rt.position = target.position;
            rt.sizeDelta = new Vector2(80f, 32f);

            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = $"+{gainAmountOnArrive}";
            tmp.fontSize = 26f;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            tmp.raycastTarget = false;

            float elapsed = 0f;
            Vector3 startPos = rt.position;
            while (elapsed < gainTextDuration)
            {
                elapsed += Time.deltaTime;
                float u = Mathf.Clamp01(elapsed / gainTextDuration);

                // 위로 떠오르면서 서서히 사라짐
                rt.position = startPos + Vector3.up * (gainTextRiseHeight * u);
                var c = tmp.color;
                c.a = 1f - u;
                tmp.color = c;

                yield return null;
            }

            Destroy(go);
        }
    }
}



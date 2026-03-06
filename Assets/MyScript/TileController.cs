using System.Collections;
using UnityEngine;

namespace WoodLand3D.Tiles
{
    using WoodLand3D.Core.Resources;

    /// <summary>
    /// 단일 타일의 잠금/해제 시각, 트리거 감지, 하이라이트를 제어한다.
    /// 그리드 좌표와 언락 상태를 가지며, 해제 시 리소스 스포너를 호출한다.
    /// </summary>
    public class TileController : MonoBehaviour
    {
        [Header("시각 요소")]
        [SerializeField, Tooltip("잠긴 타일 위 구름 오브젝트")]
        private GameObject cloudVisual;
        [SerializeField, Tooltip("해제된 타일의 루트 오브젝트")]
        private GameObject unlockedRoot;

        [Header("트리거")]
        [SerializeField, Tooltip("플레이어 진입 감지용 콜라이더")]
        private Collider triggerCollider;

        [Header("하이라이트")]
        [SerializeField, Tooltip("스케일로 하이라이트할 대상")]
        private Transform highlightTarget;
        [SerializeField, Tooltip("하이라이트 시 적용할 스케일 배율")]
        private float highlightScaleMultiplier = 1.05f;

        [Header("자원")]
        [SerializeField, Tooltip("이 타일의 자원 스포너")]
        private TileResourceSpawner resourceSpawner;

        public Vector2Int GridPos { get; private set; }
        public bool IsUnlocked { get; private set; }

        private Vector3 _originalHighlightScale;
        private bool _highlightInitialized;

        /// <summary>
        /// 타일의 그리드 좌표와 초기 잠금 상태를 설정한다. 생성 직후 한 번만 호출.
        /// </summary>
        public void Initialize(Vector2Int pos, bool initiallyUnlocked)
        {
            GridPos = pos;
            SetUnlocked(initiallyUnlocked, playFx: false, force: true);

            var t = transform;
            t.position = new Vector3(t.position.x, 0f, t.position.z);

            if (highlightTarget == null)
                highlightTarget = transform;

            _originalHighlightScale = highlightTarget.localScale;
            _highlightInitialized = true;

            if (triggerCollider != null)
                triggerCollider.isTrigger = true;

            if (IsUnlocked && resourceSpawner != null)
                resourceSpawner.OnTileUnlocked();
        }

        /// <summary>
        /// 타일의 잠금/해제 상태를 설정하고 시각을 갱신한다.
        /// </summary>
        public void SetUnlocked(bool unlocked, bool playFx, bool force = false)
        {
            if (!force && IsUnlocked == unlocked)
                return;

            IsUnlocked = unlocked;

            if (unlocked)
            {
                if (playFx)
                    StartCoroutine(PlayUnlockFx());
                if (resourceSpawner != null)
                    resourceSpawner.OnTileUnlocked();
            }

            if (cloudVisual != null)
                cloudVisual.SetActive(!unlocked);
            if (unlockedRoot != null)
                unlockedRoot.SetActive(unlocked);
        }

        /// <summary>
        /// 구름이 커졌다가 사라지는 언락 연출 코루틴.
        /// </summary>
        public IEnumerator PlayUnlockFx()
        {
            if (cloudVisual == null)
                yield break;

            var cloudTransform = cloudVisual.transform;
            var originalScale = cloudTransform.localScale;
            var targetScale = originalScale * 1.3f;
            float duration = 0.6f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                cloudTransform.localScale = Vector3.Lerp(originalScale, targetScale, t);
                yield return null;
            }

            cloudVisual.SetActive(false);
            cloudTransform.localScale = originalScale;
        }

        /// <summary>
        /// 잠긴 타일일 때 하이라이트(스케일 확대)를 표시한다.
        /// </summary>
        public void ShowHighlight()
        {
            if (!_highlightInitialized || IsUnlocked || highlightTarget == null)
                return;
            highlightTarget.localScale = _originalHighlightScale * highlightScaleMultiplier;
        }

        /// <summary>
        /// 하이라이트를 제거한다.
        /// </summary>
        public void HideHighlight()
        {
            if (!_highlightInitialized || highlightTarget == null)
                return;
            highlightTarget.localScale = _originalHighlightScale;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player"))
                return;
            if (IsUnlocked)
                return;

            if (TileUnlockSystem.Instance != null)
                TileUnlockSystem.Instance.HandleTileTriggerEnter(this);
            ShowHighlight();
        }

        private void OnTriggerExit(Collider other)
        {
            if (!other.CompareTag("Player"))
                return;
            if (IsUnlocked)
                return;

            if (TileUnlockSystem.Instance != null)
                TileUnlockSystem.Instance.HandleTileTriggerExit(this);
            HideHighlight();
        }
    }
}

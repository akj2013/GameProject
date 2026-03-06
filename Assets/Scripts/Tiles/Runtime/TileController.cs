using System.Collections;
using UnityEngine;
using WoodLand3D.Resources.Spawn;
using WoodLand3D.Tiles.Unlock;

namespace WoodLand3D.Tiles.Runtime
{
    /// <summary>
    /// 단일 타일의 잠금/해제 시각, 트리거 감지, 하이라이트를 제어한다.
    /// </summary>
    public class TileController : MonoBehaviour
    {
        [Header("시각 요소")]
        [SerializeField] private GameObject cloudVisual;
        [SerializeField] private GameObject unlockedRoot;
        [Header("트리거")]
        [SerializeField] private Collider triggerCollider;
        [Header("하이라이트")]
        [SerializeField] private Transform highlightTarget;
        [SerializeField] private float highlightScaleMultiplier = 1.05f;
        [Header("자원")]
        [SerializeField] private TileResourceSpawner resourceSpawner;

        public Vector2Int GridPos { get; private set; }
        public bool IsUnlocked { get; private set; }

        private Vector3 _originalHighlightScale;
        private bool _highlightInitialized;

        public void Initialize(Vector2Int pos, bool initiallyUnlocked)
        {
            GridPos = pos;
            SetUnlocked(initiallyUnlocked, playFx: false, force: true);
            var t = transform;
            t.position = new Vector3(t.position.x, 0f, t.position.z);
            if (highlightTarget == null) highlightTarget = transform;
            _originalHighlightScale = highlightTarget.localScale;
            _highlightInitialized = true;
            if (triggerCollider != null) triggerCollider.isTrigger = true;
            if (IsUnlocked && resourceSpawner != null) resourceSpawner.OnTileUnlocked();
        }

        public void SetUnlocked(bool unlocked, bool playFx, bool force = false)
        {
            if (!force && IsUnlocked == unlocked) return;
            IsUnlocked = unlocked;
            if (unlocked)
            {
                if (playFx) StartCoroutine(PlayUnlockFx());
                if (resourceSpawner != null) resourceSpawner.OnTileUnlocked();
            }
            if (cloudVisual != null) cloudVisual.SetActive(!unlocked);
            if (unlockedRoot != null) unlockedRoot.SetActive(unlocked);
        }

        public IEnumerator PlayUnlockFx()
        {
            if (cloudVisual == null) yield break;
            var cloudTransform = cloudVisual.transform;
            var originalScale = cloudTransform.localScale;
            var targetScale = originalScale * 1.3f;
            float duration = 0.6f, elapsed = 0f;
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

        public void ShowHighlight()
        {
            if (!_highlightInitialized || IsUnlocked || highlightTarget == null) return;
            highlightTarget.localScale = _originalHighlightScale * highlightScaleMultiplier;
        }

        public void HideHighlight()
        {
            if (!_highlightInitialized || highlightTarget == null) return;
            highlightTarget.localScale = _originalHighlightScale;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player") || IsUnlocked) return;
            if (TileUnlockSystem.Instance != null) TileUnlockSystem.Instance.HandleTileTriggerEnter(this);
            ShowHighlight();
        }

        private void OnTriggerExit(Collider other)
        {
            if (!other.CompareTag("Player") || IsUnlocked) return;
            if (TileUnlockSystem.Instance != null) TileUnlockSystem.Instance.HandleTileTriggerExit(this);
            HideHighlight();
        }
    }
}

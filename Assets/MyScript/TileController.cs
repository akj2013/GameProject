using System.Collections;
using UnityEngine;

namespace WoodLand3D.Tiles
{
    using WoodLand3D.Core.Resources;

    /// <summary>
    /// Controls a single tile instance (lock/unlock visuals, trigger detection).
    /// </summary>
    public class TileController : MonoBehaviour
    {
        [Header("Visuals")]
        [SerializeField] private GameObject cloudVisual;
        [SerializeField] private GameObject unlockedRoot;

        [Header("Trigger")]
        [SerializeField] private Collider triggerCollider;

        [Header("Highlight")]
        [SerializeField] private Transform highlightTarget;
        [SerializeField] private float highlightScaleMultiplier = 1.05f;

        [Header("Resources")]
        [SerializeField] private TileResourceSpawner resourceSpawner;

        public Vector2Int GridPos { get; private set; }
        public bool IsUnlocked { get; private set; }

        private Vector3 _originalHighlightScale;
        private bool _highlightInitialized;

        /// <summary>
        /// Initialize tile logical position and initial state.
        /// Must be called once right after instantiation.
        /// </summary>
        public void Initialize(Vector2Int pos, bool initiallyUnlocked)
        {
            GridPos = pos;
            // Force-apply visual state on initialization so locked tiles also configure correctly.
            SetUnlocked(initiallyUnlocked, playFx: false, force: true);

            // Ensure tile root always stays on the flat plane.
            var t = transform;
            t.position = new Vector3(t.position.x, 0f, t.position.z);

            if (highlightTarget == null)
                highlightTarget = transform;

            _originalHighlightScale = highlightTarget.localScale;
            _highlightInitialized = true;

            if (triggerCollider != null)
            {
                triggerCollider.isTrigger = true;
            }

            // If this tile starts unlocked from save/load, ensure resources spawn.
            if (IsUnlocked && resourceSpawner != null)
            {
                resourceSpawner.OnTileUnlocked();
            }
        }

        public void SetUnlocked(bool unlocked, bool playFx, bool force = false)
        {
            if (!force && IsUnlocked == unlocked)
                return;

            // Always update visuals to match target state.
            IsUnlocked = unlocked;

            if (unlocked)
            {
                if (playFx)
                {
                    StartCoroutine(PlayUnlockFx());
                }

                // Spawn resources when tile becomes unlocked.
                if (resourceSpawner != null)
                {
                    resourceSpawner.OnTileUnlocked();
                }
            }
            
            if (cloudVisual != null)
                cloudVisual.SetActive(!unlocked);

            if (unlockedRoot != null)
                unlockedRoot.SetActive(unlocked);
        }

        /// <summary>
        /// Simple visual feedback for unlocking.
        /// Scales the cloud up slightly, then disables it.
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

        public void ShowHighlight()
        {
            if (!_highlightInitialized || IsUnlocked || highlightTarget == null)
                return;

            highlightTarget.localScale = _originalHighlightScale * highlightScaleMultiplier;
        }

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
            {
                TileUnlockSystem.Instance.HandleTileTriggerEnter(this);
            }

            ShowHighlight();
        }

        private void OnTriggerExit(Collider other)
        {
            if (!other.CompareTag("Player"))
                return;

            if (IsUnlocked)
                return;

            if (TileUnlockSystem.Instance != null)
            {
                TileUnlockSystem.Instance.HandleTileTriggerExit(this);
            }

            HideHighlight();
        }
    }
}


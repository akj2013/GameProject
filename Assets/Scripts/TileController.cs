using System.Collections;
using UnityEngine;

namespace WoodLand3D.Tiles
{
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

        public Vector2Int GridPos { get; private set; }
        public bool IsUnlocked { get; private set; }

        /// <summary>
        /// Initialize tile logical position and initial state.
        /// Must be called once right after instantiation.
        /// </summary>
        public void Initialize(Vector2Int pos, bool initiallyUnlocked)
        {
            GridPos = pos;
            SetUnlocked(initiallyUnlocked, playFx: false);

            // Ensure tile root always stays on the flat plane.
            var t = transform;
            t.position = new Vector3(t.position.x, 0f, t.position.z);

            if (triggerCollider != null)
            {
                triggerCollider.isTrigger = true;
            }
        }

        public void SetUnlocked(bool unlocked, bool playFx)
        {
            if (IsUnlocked == unlocked)
                return;

            IsUnlocked = unlocked;

            if (unlocked)
            {
                if (playFx)
                {
                    StartCoroutine(PlayCloudFadeOut());
                }
                else if (cloudVisual != null)
                {
                    cloudVisual.SetActive(false);
                }

                if (unlockedRoot != null)
                    unlockedRoot.SetActive(true);
            }
            else
            {
                if (cloudVisual != null)
                    cloudVisual.SetActive(true);

                if (unlockedRoot != null)
                    unlockedRoot.SetActive(false);
            }
        }

        /// <summary>
        /// Simple visual feedback for unlocking.
        /// Tries to scale the cloud up slightly before disabling it.
        /// </summary>
        public IEnumerator PlayCloudFadeOut()
        {
            if (cloudVisual == null)
                yield break;

            var cloudTransform = cloudVisual.transform;
            var originalScale = cloudTransform.localScale;
            var targetScale = originalScale * 1.2f;

            float duration = 0.3f;
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
        }
    }
}


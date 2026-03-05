using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace WoodLand3D.UI
{
    /// <summary>
    /// Simple unlock panel UI for tiles.
    /// </summary>
    public class UnlockPanelUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Canvas rootCanvas;
        [SerializeField] private TMP_Text tilePosText;
        [SerializeField] private TMP_Text messageText;
        [SerializeField] private Button unlockButton;
        [SerializeField] private Button closeButton;

        private Action _onUnlock;

        private void Awake()
        {
            if (unlockButton != null)
                unlockButton.onClick.AddListener(OnUnlockClicked);

            if (closeButton != null)
                closeButton.onClick.AddListener(Hide);

            // 시작 시에는 패널을 숨기되, GameObject 자체는 활성 상태로 둔다.
            // (Canvas.enabled 만 끄고, 필요 시 Show()에서 다시 켠다)
            if (rootCanvas != null)
            {
                rootCanvas.enabled = false;
            }
        }

        public void Show(
            Tiles.TileController tile,
            int cost,
            bool canUnlock,
            string message,
            Action onUnlock)
        {
            _onUnlock = canUnlock ? onUnlock : null;

            if (tilePosText != null)
                tilePosText.text = $"Tile ({tile.GridPos.x}, {tile.GridPos.y})";

            if (messageText != null)
                messageText.text = message;

            if (unlockButton != null)
            {
                unlockButton.interactable = canUnlock;
                unlockButton.gameObject.SetActive(canUnlock);
            }

            if (rootCanvas != null)
            {
                // GameObject가 비활성화된 상태여도 안전하게 다시 켜준다.
                if (!rootCanvas.gameObject.activeSelf)
                    rootCanvas.gameObject.SetActive(true);
                rootCanvas.enabled = true;
            }
            else
                gameObject.SetActive(true);
        }

        public void Hide()
        {
            _onUnlock = null;

            if (rootCanvas != null)
            {
                rootCanvas.enabled = false;
            }
            else
                gameObject.SetActive(false);
        }

        private void OnUnlockClicked()
        {
            _onUnlock?.Invoke();
        }
    }
}


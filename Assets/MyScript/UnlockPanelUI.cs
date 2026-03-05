using System;
using UnityEngine;
using UnityEngine.UI;

namespace WoodLand3D.UI
{
    /// <summary>
    /// Simple unlock panel UI for tiles.
    /// </summary>
    public class UnlockPanelUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Canvas rootCanvas;
        [SerializeField] private Text tilePosText;
        [SerializeField] private Text messageText;
        [SerializeField] private Button unlockButton;
        [SerializeField] private Button closeButton;

        private Action _onUnlock;

        private void Awake()
        {
            if (unlockButton != null)
                unlockButton.onClick.AddListener(OnUnlockClicked);

            if (closeButton != null)
                closeButton.onClick.AddListener(Hide);

            Hide();
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
                rootCanvas.enabled = true;
            else
                gameObject.SetActive(true);
        }

        public void Hide()
        {
            _onUnlock = null;

            if (rootCanvas != null)
                rootCanvas.enabled = false;
            else
                gameObject.SetActive(false);
        }

        private void OnUnlockClicked()
        {
            _onUnlock?.Invoke();
        }
    }
}


using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using WoodLand3D.Tiles.Runtime;

namespace WoodLand3D.UI.Panels
{
    /// <summary>
    /// 타일 언락 비용과 메시지를 보여주는 간단한 패널 UI.
    /// TileUnlockSystem이 표시/숨김을 제어하며, 언락 버튼 클릭 시 콜백을 호출한다.
    /// </summary>
    public class UnlockPanelUI : MonoBehaviour
    {
        [Header("UI 참조")]
        [SerializeField, Tooltip("패널의 루트 캔버스")]
        private Canvas rootCanvas;
        [SerializeField, Tooltip("타일 좌표 표시 텍스트")]
        private TMP_Text tilePosText;
        [SerializeField, Tooltip("비용/메시지 표시 텍스트")]
        private TMP_Text messageText;
        [SerializeField, Tooltip("언락 실행 버튼")]
        private Button unlockButton;
        [SerializeField, Tooltip("닫기 버튼")]
        private Button closeButton;

        private Action _onUnlock;

        private void Awake()
        {
            if (unlockButton != null)
                unlockButton.onClick.AddListener(OnUnlockClicked);
            if (closeButton != null)
                closeButton.onClick.AddListener(Hide);

            if (rootCanvas != null)
                rootCanvas.enabled = false;
        }

        /// <summary>
        /// 패널을 표시하고 비용·가능 여부·메시지를 설정한다. 언락 가능 시 버튼 클릭 시 onUnlock이 호출된다.
        /// </summary>
        public void Show(
            TileController tile,
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
                if (!rootCanvas.gameObject.activeSelf)
                    rootCanvas.gameObject.SetActive(true);
                rootCanvas.enabled = true;
            }
            else
                gameObject.SetActive(true);
        }

        /// <summary>
        /// 패널을 숨긴다.
        /// </summary>
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

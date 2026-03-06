using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 스테이지 아이콘 패널 컨트롤러
/// </summary>
public class StageIconPanelController : MonoBehaviour
{
    [Tooltip("스테이지 아이콘 버튼")]
    public Button btnStageIcon;
    [Tooltip("스테이지 패널")]
    public GameObject stagePanel;

    void Start()
    {
        if (btnStageIcon != null && stagePanel != null)
        {
            btnStageIcon.onClick.AddListener(() => {
                stagePanel.SetActive(!stagePanel.activeSelf);
            });
        }
    }
}

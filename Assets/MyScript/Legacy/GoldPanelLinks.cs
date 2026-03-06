using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// GoldPanel에 붙여두면 GameManager, FloatingCoinFx가 참조를 사용합니다.
/// 에디터 메뉴(Tools > Gold Panel 연결)로 씬에 등록하세요.
/// </summary>
public class GoldPanelLinks : MonoBehaviour
{
    [Tooltip("골드 숫자 텍스트")]
    public TextMeshProUGUI goldText;
    [Tooltip("금화 아이콘 RectTransform")]
    public RectTransform coinIconRect;

    [Header("플로팅 골드")]
    [Tooltip("통나무 판매 시 날아가는 금화 이미지 (비어있으면 FloatingCoinFx 기본값 사용)")]
    public Sprite floatingCoinSprite;
    [Tooltip("플로팅 골드 크기 배율 (1=기본, 0.5=절반)")]
    [Range(0.1f, 2f)]
    public float floatingCoinScale = 1f;

    public static GoldPanelLinks Instance { get; private set; }

    void Awake()
    {
        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}

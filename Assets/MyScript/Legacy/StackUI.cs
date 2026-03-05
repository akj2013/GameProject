using UnityEngine;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// PowerPanel - 캐릭터 공격력 표시 (원래 스택 수치였으나 공격력으로 변경)
/// </summary>
public class StackUI : MonoBehaviour
{
    public Slider slider;
    public TextMeshProUGUI stackText;

    int _lastAttackDamage = -1;

    void Update()
    {
        int attackDamage = 1;
        if (PlayerStats.Instance != null)
            attackDamage = PlayerStats.Instance.attackDamage;

        if (attackDamage == _lastAttackDamage) return;
        _lastAttackDamage = attackDamage;

        if (slider != null)
        {
            slider.maxValue = Mathf.Max(1, attackDamage);
            slider.value = attackDamage;
        }
        if (stackText != null)
            stackText.text = attackDamage.ToString();
    }
}

using UnityEngine;
using WoodLand3D.Core.Events;
using WoodLand3D.Resources.Types;

namespace WoodLand3D.UI.WorldUI
{
    /// <summary>
    /// 자원 획득 시 월드 스페이스 플로팅 텍스트를 스폰하는 매니저.
    /// ResourceGained 이벤트를 구독하며, 프리팹이 할당되지 않으면 경고만 남기고 동작은 유지한다.
    /// </summary>
    public class FloatingTextWorld : MonoBehaviour
    {
        [Header("프리팹(선택)")]
        [SerializeField, Tooltip("FloatingTextItem + TMP_Text가 붙은 프리팹. 비어 있으면 플로팅 텍스트 미표시")]
        private GameObject floatingTextPrefab;

        [Header("오프셋")]
        [SerializeField, Tooltip("자원 위치에서 스폰할 때 더할 오프셋")]
        private Vector3 spawnOffset = new Vector3(0f, 1f, 0f);

        private void OnEnable()
        {
            ResourceGainedEvents.OnResourceGained += OnResourceGained;
        }

        private void OnDisable()
        {
            ResourceGainedEvents.OnResourceGained -= OnResourceGained;
        }

        private void OnResourceGained(ResourceType type, int amount, Vector3 worldPosition, Transform playerTransform)
        {
            if (floatingTextPrefab == null)
            {
                Debug.LogWarning("FloatingTextWorld: floatingTextPrefab이 할당되지 않았습니다. 플로팅 텍스트를 건너뜁니다.");
                return;
            }

            string displayText = $"+{amount} {type}";
            Vector3 pos = worldPosition + spawnOffset;

            GameObject instance = Instantiate(floatingTextPrefab, pos, Quaternion.identity);
            var item = instance.GetComponent<FloatingTextItem>();
            if (item != null)
                item.Setup(displayText, pos);
            else
            {
                var tmp = instance.GetComponentInChildren<TMPro.TMP_Text>(true);
                if (tmp != null)
                    tmp.text = displayText;
                Destroy(instance, 1.2f);
            }
        }
    }
}

using UnityEngine;
using WoodLand3D.Resources.Types;

namespace WoodLand3D.Gameplay.Events
{
    /// <summary>
    /// 자원 획득 시 발생하는 이벤트 허브. HUD, 플로팅 텍스트, 픽업 VFX 등이 구독한다.
    /// </summary>
    public class ResourceGainedEvents : MonoBehaviour
    {
        public static ResourceGainedEvents Instance { get; private set; }

        public System.Action<ResourceType, int, Vector3> OnResourceGained;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>
        /// 자원 획득을 알린다. (타입, 수량, 월드 위치)
        /// </summary>
        public void Raise(ResourceType type, int amount, Vector3 worldPosition)
        {
            OnResourceGained?.Invoke(type, amount, worldPosition);
        }
    }
}

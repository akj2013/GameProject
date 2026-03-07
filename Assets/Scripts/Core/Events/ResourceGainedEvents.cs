using System;
using UnityEngine;
using WoodLand3D.Resources.Types;

namespace WoodLand3D.Core.Events
{
    /// <summary>
    /// 자원 획득 시 정적 이벤트 허브. HUD, 플로팅 텍스트, 픽업 VFX 등이 구독한다.
    /// </summary>
    public static class ResourceGainedEvents
    {
        /// <summary>
        /// 자원 획득 시 (타입, 수량, 월드 위치, 플레이어 Transform).
        /// </summary>
        public static event Action<ResourceType, int, Vector3, Transform> OnResourceGained;

        /// <summary>
        /// 자원 획득을 알린다.
        /// </summary>
        public static void Raise(ResourceType type, int amount, Vector3 worldPosition, Transform playerTransform)
        {
            OnResourceGained?.Invoke(type, amount, worldPosition, playerTransform);
        }
    }
}

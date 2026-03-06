using System;
using UnityEngine;

namespace WoodLand3D.Core.Resources
{
    /// <summary>
    /// 자원 획득 시 플로팅 텍스트·픽업 VFX·HUD가 구독하는 정적 이벤트 허브.
    /// 채집 보상 지급 시 Raise를 호출하면 구독자에게 전달된다.
    /// </summary>
    public static class ResourceGainedEvents
    {
        /// <summary>
        /// 플레이어가 자원을 획득했을 때 발생. (자원 타입, 획득량, 월드 위치, 플레이어 Transform)
        /// </summary>
        public static event Action<ResourceType, int, Vector3, Transform> OnResourceGained;

        /// <summary>
        /// 자원 획득을 알린다. 플레이어 Transform 없이 호출 시 null로 전달된다.
        /// </summary>
        public static void Raise(ResourceType type, int amount, Vector3 worldPosition)
        {
            Raise(type, amount, worldPosition, null);
        }

        /// <summary>
        /// 자원 획득을 알린다. 플레이어 Transform은 픽업 VFX 등에서 사용한다.
        /// </summary>
        public static void Raise(ResourceType type, int amount, Vector3 worldPosition, Transform playerTransform)
        {
            if (amount <= 0)
                return;
            OnResourceGained?.Invoke(type, amount, worldPosition, playerTransform);
        }
    }
}

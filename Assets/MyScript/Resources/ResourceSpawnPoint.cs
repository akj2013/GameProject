using UnityEngine;

namespace WoodLand3D.Core.Resources
{
    /// <summary>
    /// 타일 위에서 자원이 스폰될 위치를 표시하는 선택용 컴포넌트.
    /// ResourceSpawnPoint가 붙은 Transform을 스포너가 우선 사용한다.
    /// </summary>
    public class ResourceSpawnPoint : MonoBehaviour
    {
        [Header("스폰 설정")]
        [SerializeField, Tooltip("이 위치에서 스폰할 자원 타입")]
        private ResourceType type = ResourceType.Tree;
        [SerializeField, Tooltip("실제 스폰 위치(비어 있으면 이 오브젝트의 transform 사용)")]
        private Transform spawnTransform;

        public ResourceType Type => type;
        public Transform SpawnTransform => spawnTransform != null ? spawnTransform : transform;
    }
}

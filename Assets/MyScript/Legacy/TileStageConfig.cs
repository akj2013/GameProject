using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 8x8 타일 프리팹에 부착하여 스테이지 번호와 스폰 가능한 나무 프리팹을 관리합니다.
/// </summary>
public class TileStageConfig : MonoBehaviour
{
    [Header("스테이지 설정")]
    [Tooltip("해당 타일의 스테이지 번호 (1=마을, 2~16=벌목 구역)")]
    [SerializeField, Min(1)] int stageNumber = 1;

    [Header("나무 스폰 설정")]
    [Tooltip("이 타일 내에 스폰될 수 있는 나무 프리팹 목록. Element 0이 기본 스폰 프리팹. 리젠 시 각 TileGround_ 자식으로 한 개씩 생성됨")]
    [SerializeField] List<GameObject> spawnableTreePrefabs = new List<GameObject>();

    [Header("나무 스폰 Transform (TileGround_ 자식 기준)")]
    [Tooltip("TileGround_ 자식으로 생성되는 나무의 로컬 위치")]
    [SerializeField] Vector3 treeSpawnLocalPosition = Vector3.zero;
    [Tooltip("TileGround_ 자식으로 생성되는 나무의 로컬 회전 (오일러 각도)")]
    [SerializeField] Vector3 treeSpawnLocalRotationEuler = Vector3.zero;
    [Tooltip("TileGround_ 자식으로 생성되는 나무의 로컬 스케일")]
    [SerializeField] Vector3 treeSpawnLocalScale = new Vector3(1f, 5f, 1f);

    /// <summary>스테이지 번호 (1번부터)</summary>
    public int StageNumber => stageNumber;

    /// <summary>스폰 가능한 나무 프리팹 목록</summary>
    public IReadOnlyList<GameObject> SpawnableTreePrefabs => spawnableTreePrefabs;

    /// <summary>마을(1번) 여부</summary>
    public bool IsVillage => stageNumber == 1;

    /// <summary>스폰 가능한 나무 프리팹이 있는지</summary>
    public bool HasSpawnableTrees => spawnableTreePrefabs != null && spawnableTreePrefabs.Count > 0;

    /// <summary>스폰 가능한 나무 프리팹 목록 설정 (에디터/런타임)</summary>
    public void SetSpawnableTreePrefabs(List<GameObject> prefabs)
    {
        spawnableTreePrefabs = prefabs != null ? new List<GameObject>(prefabs) : new List<GameObject>();
    }

    void Start()
    {
        RefreshTileTreesFromElement0();
    }

    /// <summary>게임 최초 시작 시 호출. Element 0이 있으면 TileGround_별로 나무 리젠, 없으면 TileGround_에서 나무만 제거.</summary>
    public void RefreshTileTreesFromElement0()
    {
        RespawnTreesOnTileGrounds();
    }

    /// <summary>HOME 버튼·스테이지 패널 나무 변경 시 공통 리젠: 1) 기존 나무 제거 2) 나무 스폰 설정(Element 0)으로 TileGround_마다 생성. syncFromExistingTrees가 true일 때만 spawnable이 비어 있으면 기존 나무의 sourcePrefab으로 채움.</summary>
    /// <param name="syncFromExistingTrees">false면 스테이지 패널에서 슬롯을 비운 경우처럼, 기존 나무에서 spawnable을 채우지 않고 그대로 제거만 함</param>
    public void RespawnTreesOnTileGrounds(bool syncFromExistingTrees = true)
    {
        GameObject treePrefab = (spawnableTreePrefabs != null && spawnableTreePrefabs.Count > 0 && spawnableTreePrefabs[0] != null) ? spawnableTreePrefabs[0] : null;

        // spawnable이 비어 있을 때만: 기존 나무의 sourcePrefab으로 한 번 채우기 (HOME 리젠용). 스테이지 패널에서 슬롯 비울 때는 syncFromExistingTrees=false로 호출해 이 블록 스킵
        if (syncFromExistingTrees && treePrefab == null)
        {
            var existing = GetComponentsInChildren<TreeManager>(true);
            foreach (var tm in existing)
            {
                if (tm != null && tm.sourcePrefab != null)
                {
                    spawnableTreePrefabs = spawnableTreePrefabs ?? new List<GameObject>();
                    if (spawnableTreePrefabs.Count == 0)
                        spawnableTreePrefabs.Add(tm.sourcePrefab);
                    treePrefab = tm.sourcePrefab;
                    break;
                }
            }
        }

        // 1) 타일 내 기존 나무 전부 제거 (TileGround_ 여부와 관계없이)
        var existingTrees = GetComponentsInChildren<TreeManager>(true);
        foreach (var tm in existingTrees)
        {
            if (tm != null && tm.gameObject != null)
                Object.Destroy(tm.gameObject);
        }

        // 2) TileGround_마다 나무 프리팹이 있으면 한 개씩 생성
        var allTransforms = GetComponentsInChildren<Transform>(true);
        foreach (var t in allTransforms)
        {
            if (t == null || !t.gameObject.name.StartsWith("TileHex_0")) continue;

            if (treePrefab == null) continue;

            var tree = Object.Instantiate(treePrefab, t);
            tree.transform.localPosition = treeSpawnLocalPosition;
            tree.transform.localRotation = Quaternion.Euler(treeSpawnLocalRotationEuler);
            tree.transform.localScale = treeSpawnLocalScale;
            tree.SetActive(true);
            var tm = tree.GetComponent<TreeManager>();
            if (tm != null)
                tm.sourcePrefab = treePrefab;
        }
    }

    /// <summary>Element 0을 지정 프리팹으로 바꾸고, 타일 내 TileGround_별로 나무 리젠</summary>
    public void AssignTreeAndReplace(GameObject treePrefab)
    {
        if (treePrefab != null)
        {
            spawnableTreePrefabs = new List<GameObject> { treePrefab };
            RespawnTreesOnTileGrounds();
        }
        else
        {
            spawnableTreePrefabs = new List<GameObject>();
            RespawnTreesOnTileGrounds();
        }
    }

    /// <summary>타일 내 기존 64개 나무(위치/스케일 유지)를 지정 프리팹으로 교체. 새 인스턴스는 활성화됨. 나무가 없으면 타일의 원래 나무 위치에 새로 스폰.</summary>
    public void ReplaceTreesInTile(GameObject treePrefab)
    {
        if (treePrefab == null) return;

        // TreeManager만 찾기 (비활성 포함). 스테이지 패널 교체와 동일하게 활성/비활성 구분 없이 전부 교체
        var treeManagers = GetComponentsInChildren<TreeManager>(true);
        
        List<GameObject> treesToReplace = new List<GameObject>();
        HashSet<Transform> existingTreeTransforms = new HashSet<Transform>();
        
        foreach (var tm in treeManagers)
        {
            if (tm != null && tm.gameObject != null)
            {
                treesToReplace.Add(tm.gameObject);
                existingTreeTransforms.Add(tm.transform);
            }
        }

        // 기존 나무 교체
        foreach (var oldTree in treesToReplace)
        {
            if (oldTree == null) continue;
            
            var parent = oldTree.transform.parent;
            var pos = oldTree.transform.localPosition;
            var rot = oldTree.transform.localRotation;
            var scale = oldTree.transform.localScale;
            var idx = oldTree.transform.GetSiblingIndex();

            var newTree = Object.Instantiate(treePrefab, parent);
            newTree.transform.localPosition = pos;
            newTree.transform.localRotation = rot;
            newTree.transform.localScale = scale;
            newTree.transform.SetSiblingIndex(idx);

            Object.Destroy(oldTree);
        }

        // 나무가 없거나 부족한 경우: 타일의 자식 Transform 중 나무가 없는 위치에 새로 스폰
        // 타일 프리팹에 원래 64개의 나무가 배치되어 있다면, 그 위치를 기준으로 스폰
        if (treesToReplace.Count < 64)
        {
            // 타일의 직접 자식들 확인 (나무가 배치된 위치)
            List<Transform> spawnPoints = new List<Transform>();
            foreach (Transform child in transform)
            {
                if (child == null) continue;
                // 이미 나무가 있는 위치는 제외
                if (!existingTreeTransforms.Contains(child))
                {
                    // 이 위치에 TreeManager가 없으면 스폰 포인트로 사용
                    if (child.GetComponent<TreeManager>() == null)
                    {
                        spawnPoints.Add(child);
                    }
                }
            }

            // 스폰 포인트가 있으면 그 위치에 나무 생성
            foreach (var spawnPoint in spawnPoints)
            {
                if (spawnPoint == null) continue;
                
                var newTree = Object.Instantiate(treePrefab, spawnPoint);
                newTree.transform.localPosition = Vector3.zero;
                newTree.transform.localRotation = Quaternion.identity;
                newTree.transform.localScale = Vector3.one;
            }
        }
    }

    /// <summary>타일 내 모든 나무 비활성화 (오브젝트는 유지, 위치/스케일 그대로). 클리어 시 사용.</summary>
    public void DeactivateAllTrees()
    {
        var treeManagers = GetComponentsInChildren<TreeManager>(true);
        foreach (var tm in treeManagers)
            if (tm != null && tm.gameObject != null)
                tm.gameObject.SetActive(false);
    }

    /// <summary>타일 나무 전부 비활성화 + 스폰 목록 비우기 (슬롯에서 바깥에 놓을 때). 나무 오브젝트는 삭제하지 않음.</summary>
    public void ClearTileCompletely()
    {
        DeactivateAllTrees();
        SetSpawnableTreePrefabs(null);
    }

    /// <summary>스테이지 번호 설정</summary>
    public void SetStageNumber(int number)
    {
        stageNumber = Mathf.Max(1, number);
    }

    /// <summary>타일 내 모든 나무가 베어졌는지 확인</summary>
    public bool AreAllTreesCut()
    {
        var treeManagers = GetComponentsInChildren<TreeManager>(true);
        
        // TreeManager 확인 (공격 가능하면 아직 베어지지 않음)
        foreach (var tree in treeManagers)
        {
            if (tree != null && tree.IsAttackable)
                return false;
        }
        
        return true;
    }

    /// <summary>타일 내 활성화된 나무가 하나라도 있는지 (미니맵 색상: 있으면 기본색, 없으면 Cut 색)</summary>
    public bool HasAnyActiveTree()
    {
        var treeManagers = GetComponentsInChildren<TreeManager>(true);
        foreach (var tree in treeManagers)
        {
            if (tree != null && tree.gameObject.activeInHierarchy)
                return true;
        }
        return false;
    }

    /// <summary>타일의 월드 공간 Bounds 계산</summary>
    public Bounds GetWorldBounds()
    {
        var renderers = GetComponentsInChildren<Renderer>(true);
        if (renderers.Length > 0)
        {
            var bounds = renderers[0].bounds;
            foreach (var r in renderers)
                bounds.Encapsulate(r.bounds);
            return bounds;
        }
        var colliders = GetComponentsInChildren<Collider>(true);
        if (colliders.Length > 0)
        {
            var bounds = colliders[0].bounds;
            foreach (var c in colliders)
                bounds.Encapsulate(c.bounds);
            return bounds;
        }
        return new Bounds(transform.position, Vector3.one * 8f);
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        stageNumber = Mathf.Max(1, stageNumber);
    }
#endif
}

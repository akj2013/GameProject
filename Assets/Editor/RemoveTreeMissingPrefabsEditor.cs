using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;

/// <summary>
/// 씬에서 이름이 "Tree_"로 시작하고 Missing Prefab인 오브젝트를 모두 찾아 삭제하는 에디터 메뉴.
/// </summary>
public static class RemoveTreeMissingPrefabsEditor
{
    [MenuItem("Tools/WoodLand3D/씬에서 Tree_ Missing Prefab 전부 삭제")]
    public static void RemoveTreeMissingPrefabs()
    {
        var scene = EditorSceneManager.GetActiveScene();
        var roots = scene.GetRootGameObjects();
        var toDestroy = new HashSet<GameObject>();

        foreach (var root in roots)
        {
            var transforms = root.GetComponentsInChildren<Transform>(true);
            foreach (var t in transforms)
            {
                var go = t.gameObject;
                if (!go.name.StartsWith("Tree_"))
                    continue;
                if (PrefabUtility.GetPrefabInstanceStatus(go) != PrefabInstanceStatus.MissingAsset)
                    continue;

                var instanceRoot = PrefabUtility.GetOutermostPrefabInstanceRoot(go);
                if (instanceRoot != null)
                    toDestroy.Add(instanceRoot);
                else
                    toDestroy.Add(go);
            }
        }

        int count = 0;
        foreach (var go in toDestroy)
        {
            if (go == null) continue;
            Object.DestroyImmediate(go);
            count++;
        }

        if (count > 0)
        {
            EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log($"[WoodLand3D] 씬에서 Tree_ Missing Prefab 오브젝트 {count}개 삭제됨.");
        }
        else
        {
            Debug.Log("[WoodLand3D] 삭제할 Tree_ Missing Prefab 오브젝트가 없습니다.");
        }
    }
}

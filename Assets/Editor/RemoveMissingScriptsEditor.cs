using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// 씬·프리팹에 붙어 있는 Missing Script 컴포넌트를 한 번에 제거하는 에디터 메뉴.
/// (삭제된 셀존 스크립트 등으로 인한 "The referenced script is missing" 경고 해결)
/// </summary>
public static class RemoveMissingScriptsEditor
{
    [MenuItem("Tools/WoodLand3D/Missing Script 전부 제거 (현재 씬)")]
    public static void RemoveMissingScriptsInActiveScene()
    {
        var scene = EditorSceneManager.GetActiveScene();
        var roots = scene.GetRootGameObjects();
        int totalRemoved = 0;

        foreach (var root in roots)
        {
            var transforms = root.GetComponentsInChildren<Transform>(true);
            foreach (var t in transforms)
            {
                var go = t.gameObject;
                int count = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(go);
                if (count <= 0) continue;

                Undo.RegisterCompleteObjectUndo(go, "Remove missing scripts");
                int removed = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);
                totalRemoved += removed;
            }
        }

        if (totalRemoved > 0)
        {
            EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log($"[WoodLand3D] 현재 씬에서 Missing Script {totalRemoved}개 제거됨.");
        }
        else
        {
            Debug.Log("[WoodLand3D] 현재 씬에 Missing Script가 없습니다.");
        }
    }

    [MenuItem("Tools/WoodLand3D/Missing Script 전부 제거 (씬 + 프로젝트 프리팹)")]
    public static void RemoveMissingScriptsEverywhere()
    {
        int sceneRemoved = 0;
        var scene = EditorSceneManager.GetActiveScene();
        var roots = scene.GetRootGameObjects();
        foreach (var root in roots)
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
            {
                int n = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject);
                if (n > 0)
                {
                    Undo.RegisterCompleteObjectUndo(t.gameObject, "Remove missing scripts");
                    sceneRemoved += GameObjectUtility.RemoveMonoBehavioursWithMissingScript(t.gameObject);
                }
            }
        }
        if (sceneRemoved > 0)
            EditorSceneManager.MarkSceneDirty(scene);

        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" });
        int prefabRemoved = 0;
        foreach (string guid in prefabGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefabRoot = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefabRoot == null) continue;

            bool modified = false;
            foreach (var t in prefabRoot.GetComponentsInChildren<Transform>(true))
            {
                var go = t.gameObject;
                int count = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(go);
                if (count <= 0) continue;
                Undo.RegisterCompleteObjectUndo(go, "Remove missing scripts");
                prefabRemoved += GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);
                modified = true;
            }
            if (modified)
                EditorUtility.SetDirty(prefabRoot);
        }
        if (prefabRemoved > 0)
            AssetDatabase.SaveAssets();

        int total = sceneRemoved + prefabRemoved;
        if (total > 0)
            Debug.Log($"[WoodLand3D] Missing Script 제거 완료. 씬: {sceneRemoved}개, 프리팹: {prefabRemoved}개 (총 {total}개)");
        else
            Debug.Log("[WoodLand3D] 제거할 Missing Script가 없습니다.");
    }
}

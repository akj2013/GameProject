using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// 나뭇잎 색상 조정 샘플을 씬에 추가하는 에디터 메뉴.
/// </summary>
public static class TreeLeafColorSampleEditor
{
    const string Tree7Path = "Assets/EDGE/Trees_1/Prefabs/Tree7.prefab";
    const string Tree7AutumnPath = "Assets/EDGE/Trees_1/Prefabs/Tree7_autumn.prefab";

    [MenuItem("Tools/WoodLand3D/나뭇잎 색상 조정 샘플 추가 (Tree7)")]
    public static void AddTree7LeafColorSample()
    {
        AddSample(Tree7Path);
    }

    [MenuItem("Tools/WoodLand3D/나뭇잎 색상 조정 샘플 추가 (Tree7_autumn)")]
    public static void AddTree7AutumnLeafColorSample()
    {
        AddSample(Tree7AutumnPath);
    }

    static void AddSample(string prefabPath)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null)
        {
            Debug.LogError($"프리팹을 찾을 수 없습니다: {prefabPath}");
            return;
        }

        var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        instance.name = prefab.name + "_LeafColorSample";

        if (instance.GetComponent<TreeLeafColorTint>() == null)
            instance.AddComponent<TreeLeafColorTint>();

        // 기존 Tree(1), Tree(2) 근처에 배치 (없으면 원점)
        var tree1 = GameObject.Find("Tree(1)");
        var tree2 = GameObject.Find("Tree(2)");
        Vector3 pos = Vector3.zero;
        if (tree1 != null)
        {
            var t1Pos = tree1.transform.position;
            pos = t1Pos + new Vector3(3f, 0, 0);
        }
        else if (tree2 != null)
        {
            var t2Pos = tree2.transform.position;
            pos = t2Pos + new Vector3(3f, 0, 0);
        }
        instance.transform.position = pos;

        Selection.activeGameObject = instance;
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log($"나뭇잎 색상 조정 샘플 추가됨: {instance.name}. Inspector에서 Leaf Color Tint를 조정하세요.");
    }
}

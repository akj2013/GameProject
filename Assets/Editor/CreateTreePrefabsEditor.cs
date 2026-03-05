using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// 씬의 Tree_Autumn, Tree_Spring, Tree_Winter, Tree_Purple를 프리팹으로 저장하고,
/// Tile8x8 내 나무를 해당 프리팹으로 교체하는 에디터 메뉴.
/// </summary>
public static class CreateTreePrefabsEditor
{
    const string PrefabFolder = "Assets/MyPrefab";

    [MenuItem("Tools/WoodLand3D/Tree_Autumn, Tree_Spring, Tree_Winter, Tree_Purple 프리팹 생성")]
    public static void CreateTreePrefabs()
    {
        var autumn = GameObject.Find("Tree_Autumn");
        var spring = GameObject.Find("Tree_Spring");
        var winter = GameObject.Find("Tree_Winter");
        var purple = GameObject.Find("Tree_Purple");

        if (autumn == null)
        {
            Debug.LogError("씬에서 Tree_Autumn을 찾을 수 없습니다. SampleScene을 열고 다시 시도하세요.");
            return;
        }
        if (spring == null)
        {
            Debug.LogError("씬에서 Tree_Spring을 찾을 수 없습니다. SampleScene을 열고 다시 시도하세요.");
            return;
        }
        if (winter == null)
        {
            Debug.LogError("씬에서 Tree_Winter을 찾을 수 없습니다. Tree_Autumn을 복제한 뒤 이름을 Tree_Winter로 바꾸고, 자식 모델을 Tree7_winter로 교체하세요.");
            return;
        }
        if (purple == null)
            Debug.LogWarning("씬에서 Tree_Purple을 찾을 수 없습니다. Tree_Purple을 건너뜁니다.");

        if (!AssetDatabase.IsValidFolder("Assets/MyPrefab"))
            AssetDatabase.CreateFolder("Assets", "MyPrefab");

        string autumnPath = $"{PrefabFolder}/Tree_Autumn.prefab";
        string springPath = $"{PrefabFolder}/Tree_Spring.prefab";
        string winterPath = $"{PrefabFolder}/Tree_Winter.prefab";
        string purplePath = $"{PrefabFolder}/Tree_Purple.prefab";

        var autumnPrefab = PrefabUtility.SaveAsPrefabAsset(autumn, autumnPath);
        if (autumnPrefab != null)
            Debug.Log($"프리팹 생성됨: {autumnPath}");
        else
            Debug.LogError($"Tree_Autumn 프리팹 저장 실패: {autumnPath}");

        var springPrefab = PrefabUtility.SaveAsPrefabAsset(spring, springPath);
        if (springPrefab != null)
            Debug.Log($"프리팹 생성됨: {springPath}");
        else
            Debug.LogError($"Tree_Spring 프리팹 저장 실패: {springPath}");

        var winterPrefab = PrefabUtility.SaveAsPrefabAsset(winter, winterPath);
        if (winterPrefab != null)
            Debug.Log($"프리팹 생성됨: {winterPath}");
        else
            Debug.LogError($"Tree_Winter 프리팹 저장 실패: {winterPath}");

        GameObject purplePrefab = null;
        if (purple != null)
        {
            purplePrefab = PrefabUtility.SaveAsPrefabAsset(purple, purplePath);
            if (purplePrefab != null)
                Debug.Log($"프리팹 생성됨: {purplePath}");
            else
                Debug.LogError($"Tree_Purple 프리팹 저장 실패: {purplePath}");
        }

        var created = new System.Collections.Generic.List<Object>();
        if (autumnPrefab != null) created.Add(autumnPrefab);
        if (springPrefab != null) created.Add(springPrefab);
        if (winterPrefab != null) created.Add(winterPrefab);
        if (purplePrefab != null) created.Add(purplePrefab);
        if (created.Count > 0)
        {
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Selection.objects = created.ToArray();
        }
    }

    [MenuItem("Tools/WoodLand3D/Tile8x8_01 나무를 Tree_Autumn으로 교체")]
    public static void ReplaceTreesInTile8x8_01()
    {
        var tile = GameObject.Find("Tile8x8_01");
        if (tile == null)
        {
            Debug.LogError("씬에서 Tile8x8_01을 찾을 수 없습니다.");
            return;
        }

        var autumnPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabFolder}/Tree_Autumn.prefab");
        if (autumnPrefab == null)
        {
            Debug.LogError("Tree_Autumn.prefab을 찾을 수 없습니다. 먼저 'Tree_Autumn, Tree_Spring, Tree_Winter 프리팹 생성'을 실행하세요.");
            return;
        }

        var trees = tile.GetComponentsInChildren<TreeManager>(true);
        if (trees.Length == 0)
        {
            Debug.LogWarning("Tile8x8_01 내부에 나무(TreeManager)가 없습니다.");
            return;
        }

        int count = 0;
        foreach (var tm in trees)
        {
            var oldTree = tm.gameObject;
            var parent = oldTree.transform.parent;
            if (parent == null) continue;

            var newTree = (GameObject)PrefabUtility.InstantiatePrefab(autumnPrefab, parent);
            newTree.transform.localPosition = Vector3.zero;
            newTree.transform.localRotation = Quaternion.identity;
            newTree.transform.localScale = new Vector3(1f, 5f, 1f);

            Object.DestroyImmediate(oldTree);
            count++;
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log($"Tile8x8_01 내 {count}개 나무를 Tree_Autumn으로 교체했습니다. (Position: 0,0,0 / Scale: 1,5,1 / Rotation: 0,0,0)");
    }

    [MenuItem("Tools/WoodLand3D/Tile8x8_05 나무를 Tree_Winter으로 교체")]
    public static void ReplaceTreesInTile8x8_05()
    {
        var tile = GameObject.Find("Tile8x8_05");
        if (tile == null)
        {
            Debug.LogError("씬에서 Tile8x8_05을 찾을 수 없습니다.");
            return;
        }

        var winterPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabFolder}/Tree_Winter.prefab");
        if (winterPrefab == null)
        {
            Debug.LogError("Tree_Winter.prefab을 찾을 수 없습니다. 먼저 'Tree_Autumn, Tree_Spring, Tree_Winter 프리팹 생성'을 실행하세요.");
            return;
        }

        var trees = tile.GetComponentsInChildren<TreeManager>(true);
        if (trees.Length == 0)
        {
            Debug.LogWarning("Tile8x8_05 내부에 나무(TreeManager)가 없습니다.");
            return;
        }

        int count = 0;
        foreach (var tm in trees)
        {
            var oldTree = tm.gameObject;
            var parent = oldTree.transform.parent;
            if (parent == null) continue;

            var newTree = (GameObject)PrefabUtility.InstantiatePrefab(winterPrefab, parent);
            newTree.transform.localPosition = Vector3.zero;
            newTree.transform.localRotation = Quaternion.identity;
            newTree.transform.localScale = new Vector3(1f, 5f, 1f);

            Object.DestroyImmediate(oldTree);
            count++;
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log($"Tile8x8_05 내 {count}개 나무를 Tree_Winter으로 교체했습니다. (Position: 0,0,0 / Scale: 1,5,1 / Rotation: 0,0,0)");
    }

    [MenuItem("Tools/WoodLand3D/Tile8x8_07 나무를 Tree_Purple으로 교체")]
    public static void ReplaceTreesInTile8x8_07()
    {
        var tile = GameObject.Find("Tile8x8_07");
        if (tile == null)
        {
            Debug.LogError("씬에서 Tile8x8_07을 찾을 수 없습니다.");
            return;
        }

        var purplePrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabFolder}/Tree_Purple.prefab");
        if (purplePrefab == null)
        {
            Debug.LogError("Tree_Purple.prefab을 찾을 수 없습니다. 먼저 'Tree_Autumn, Tree_Spring, Tree_Winter, Tree_Purple 프리팹 생성'을 실행하세요.");
            return;
        }

        var trees = tile.GetComponentsInChildren<TreeManager>(true);
        if (trees.Length == 0)
        {
            Debug.LogWarning("Tile8x8_07 내부에 나무(TreeManager)가 없습니다.");
            return;
        }

        int count = 0;
        foreach (var tm in trees)
        {
            var oldTree = tm.gameObject;
            var parent = oldTree.transform.parent;
            if (parent == null) continue;

            var newTree = (GameObject)PrefabUtility.InstantiatePrefab(purplePrefab, parent);
            newTree.transform.localPosition = Vector3.zero;
            newTree.transform.localRotation = Quaternion.identity;
            newTree.transform.localScale = new Vector3(1f, 5f, 1f);

            Object.DestroyImmediate(oldTree);
            count++;
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log($"Tile8x8_07 내 {count}개 나무를 Tree_Purple으로 교체했습니다. (Position: 0,0,0 / Scale: 1,5,1 / Rotation: 0,0,0)");
    }

    [MenuItem("Tools/WoodLand3D/Tile8x8_06 Z축 최하단 TileGround → TileWater_Fence_z_south 교체")]
    public static void ReplaceTile8x8_06SouthEdgeWithWaterFence()
    {
        var tile = GameObject.Find("Tile8x8_06");
        if (tile == null)
        {
            Debug.LogError("씬에서 Tile8x8_06을 찾을 수 없습니다.");
            return;
        }

        var waterFencePrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabFolder}/TileWater_Fence_z_south.prefab");
        if (waterFencePrefab == null)
        {
            Debug.LogError("TileWater_Fence_z_south.prefab을 찾을 수 없습니다.");
            return;
        }

        var allTransforms = tile.GetComponentsInChildren<Transform>(true);
        var tileGrounds = new System.Collections.Generic.List<Transform>();
        foreach (var t in allTransforms)
        {
            if (t == tile.transform) continue;
            if (t.gameObject.name.Contains("TileGround"))
                tileGrounds.Add(t);
        }

        if (tileGrounds.Count == 0)
        {
            Debug.LogWarning("Tile8x8_06 내부에 TileGround를 찾을 수 없습니다.");
            return;
        }

        float minZ = float.MaxValue;
        foreach (var t in tileGrounds)
        {
            float z = t.position.z;
            if (z < minZ) minZ = z;
        }

        const float epsilon = 0.01f;
        int count = 0;
        foreach (var t in tileGrounds)
        {
            if (Mathf.Abs(t.position.z - minZ) > epsilon) continue;

            var parent = t.parent;
            var pos = t.localPosition;
            var rot = t.localRotation;
            var scale = t.localScale;

            var newObj = (GameObject)PrefabUtility.InstantiatePrefab(waterFencePrefab, parent);
            newObj.transform.localPosition = pos;
            newObj.transform.localRotation = rot;
            newObj.transform.localScale = scale;

            Object.DestroyImmediate(t.gameObject);
            count++;
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log($"Tile8x8_06 Z축 최하단 {count}개 TileGround를 TileWater_Fence_z_south로 교체했습니다.");
    }

    [MenuItem("Tools/WoodLand3D/Tile8x8_09 TileGround → X축 위쪽 Fence, 나머지 TileWater_01")]
    public static void ReplaceTile8x8_09WithWaterTiles()
    {
        var tile = GameObject.Find("Tile8x8_09");
        if (tile == null)
        {
            Debug.LogError("씬에서 Tile8x8_09을 찾을 수 없습니다.");
            return;
        }

        var fencePrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabFolder}/TileWater_Fence_x_west.prefab");
        var waterPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabFolder}/TileWater_01.prefab");
        if (fencePrefab == null)
        {
            Debug.LogError("TileWater_Fence_x_west.prefab을 찾을 수 없습니다.");
            return;
        }
        if (waterPrefab == null)
        {
            Debug.LogError("TileWater_01.prefab을 찾을 수 없습니다. (Assets/MyPrefab)");
            return;
        }

        var allTransforms = tile.GetComponentsInChildren<Transform>(true);
        var tileGrounds = new System.Collections.Generic.List<Transform>();
        foreach (var t in allTransforms)
        {
            if (t == tile.transform) continue;
            if (t.gameObject.name.Contains("TileGround"))
                tileGrounds.Add(t);
        }

        if (tileGrounds.Count == 0)
        {
            Debug.LogWarning("Tile8x8_09 내부에 TileGround를 찾을 수 없습니다.");
            return;
        }

        float minX = float.MaxValue;
        foreach (var t in tileGrounds)
        {
            float x = t.position.x;
            if (x < minX) minX = x;
        }

        const float epsilon = 0.01f;
        int fenceCount = 0;
        int waterCount = 0;
        foreach (var t in tileGrounds)
        {
            var parent = t.parent;
            var pos = t.localPosition;
            var rot = t.localRotation;
            var scale = t.localScale;

            GameObject prefabToUse;
            if (Mathf.Abs(t.position.x - minX) <= epsilon)
            {
                prefabToUse = fencePrefab;
                fenceCount++;
            }
            else
            {
                prefabToUse = waterPrefab;
                waterCount++;
            }

            var newObj = (GameObject)PrefabUtility.InstantiatePrefab(prefabToUse, parent);
            newObj.transform.localPosition = pos;
            newObj.transform.localRotation = rot;
            newObj.transform.localScale = scale;

            Object.DestroyImmediate(t.gameObject);
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log($"Tile8x8_09: X축 위쪽(west) {fenceCount}개 → TileWater_Fence_x_west, 나머지 {waterCount}개 → TileWater_01 교체 완료.");
    }

    [MenuItem("Tools/WoodLand3D/Tile8x8_09 (4) Fence → TileWater_01 교체")]
    public static void ReplaceTile8x8_09_4FencesWithTileWater01()
    {
        var tile = GameObject.Find("Tile8x8_09 (4)");
        if (tile == null)
        {
            Debug.LogError("씬에서 Tile8x8_09 (4)을 찾을 수 없습니다.");
            return;
        }

        var waterPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabFolder}/TileWater_01.prefab");
        if (waterPrefab == null)
        {
            Debug.LogError("TileWater_01.prefab을 찾을 수 없습니다.");
            return;
        }

        var allTransforms = tile.GetComponentsInChildren<Transform>(true);
        var fences = new System.Collections.Generic.List<Transform>();
        foreach (var t in allTransforms)
        {
            if (t == tile.transform) continue;
            var name = t.gameObject.name;
            if (name.Contains("TileWater_Fence_z_south") || name.Contains("TileWater_Fence_x_west"))
                fences.Add(t);
        }

        if (fences.Count == 0)
        {
            Debug.LogWarning("Tile8x8_09 (4) 내부에 TileWater_Fence_z_south 또는 TileWater_Fence_x_west를 찾을 수 없습니다.");
            return;
        }

        int count = 0;
        foreach (var t in fences)
        {
            var parent = t.parent;
            var pos = t.localPosition;
            var rot = t.localRotation;
            var scale = t.localScale;

            var newObj = (GameObject)PrefabUtility.InstantiatePrefab(waterPrefab, parent);
            newObj.transform.localPosition = pos;
            newObj.transform.localRotation = rot;
            newObj.transform.localScale = scale;

            Object.DestroyImmediate(t.gameObject);
            count++;
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log($"Tile8x8_09 (4) 내 {count}개 Fence를 TileWater_01로 교체했습니다.");
    }
}

using UnityEngine;
using UnityEditor;

/// <summary>
/// UpgradeTrigger를 UpgradeCarpet으로 교체하여 셀존처럼 카펫 형태로 만듭니다.
/// </summary>
public static class UpgradeCarpetSetupEditor
{
    const string PrefabFolder = "Assets/MyPrefab";

    [MenuItem("Tools/WoodLand3D/UpgradeTrigger → UpgradeCarpet으로 교체 (셀존 스타일)")]
    public static void ReplaceUpgradeTriggerWithCarpet()
    {
        var upgradeZone = GameObject.Find("UpgradeZone");
        if (upgradeZone == null)
        {
            Debug.LogError("씬에서 UpgradeZone을 찾을 수 없습니다.");
            return;
        }

        Transform triggerT = null;
        for (int i = 0; i < upgradeZone.transform.childCount; i++)
        {
            var c = upgradeZone.transform.GetChild(i);
            if (c.name.Contains("UpgradeTrigger"))
            {
                triggerT = c;
                break;
            }
        }
        if (triggerT == null)
        {
            Debug.LogError("UpgradeZone 내부에 UpgradeTrigger를 찾을 수 없습니다.");
            return;
        }

        var upgradeZoneScript = triggerT.GetComponent<UpgradeZone>();
        var boxCollider = triggerT.GetComponent<BoxCollider>();
        var upgradeUI = upgradeZoneScript != null ? upgradeZoneScript.upgradeUI : null;

        if (upgradeZoneScript == null || boxCollider == null)
        {
            Debug.LogError("UpgradeTrigger에 UpgradeZone 또는 BoxCollider가 없습니다.");
            return;
        }

        var pos = triggerT.position;
        var rot = triggerT.rotation;
        var scale = triggerT.localScale;
        var parent = triggerT.parent;

        var colliderCenter = boxCollider.center;
        var colliderSize = boxCollider.size;
        var isTrigger = boxCollider.isTrigger;

        PrefabUtility.UnpackPrefabInstance(triggerT.gameObject, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);

        var renderers = triggerT.GetComponentsInChildren<Renderer>(true);
        foreach (var r in renderers)
        {
            if (r.transform == triggerT)
            {
                Object.DestroyImmediate(r);
                var mf = triggerT.GetComponent<MeshFilter>();
                if (mf != null) Object.DestroyImmediate(mf);
            }
            else
            {
                Object.DestroyImmediate(r.gameObject);
            }
        }

        GameObject carpet;
        var carpetPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabFolder}/UpgradeCarpet.prefab");
        if (carpetPrefab != null)
        {
            carpet = (GameObject)PrefabUtility.InstantiatePrefab(carpetPrefab, triggerT);
        }
        else
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Textures/UpgradeCarpet.mat");
            if (mat == null)
            {
                Debug.LogError("UpgradeCarpet.mat 또는 UpgradeCarpet.prefab을 찾을 수 없습니다. 'UpgradeCarpet 프리팹 생성'을 먼저 실행하세요.");
                return;
            }
            carpet = CreateCarpetPlane(triggerT, mat);
        }
        carpet.name = "UpgradeCarpet";
        carpet.transform.localPosition = new Vector3(0, 0.01f, 0);
        carpet.transform.localRotation = Quaternion.Euler(-90, 0, 0);
        carpet.transform.localScale = new Vector3(4, 1, 3);
        carpet.transform.SetAsFirstSibling();

        if (boxCollider == null)
            boxCollider = triggerT.gameObject.AddComponent<BoxCollider>();
        boxCollider.center = colliderCenter;
        boxCollider.size = colliderSize;
        boxCollider.isTrigger = isTrigger;

        if (upgradeZoneScript.upgradeUI == null && upgradeUI != null)
            upgradeZoneScript.upgradeUI = upgradeUI;

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
        Debug.Log("UpgradeTrigger를 UpgradeCarpet(셀존 스타일)으로 교체했습니다.");
    }

    static GameObject CreateCarpetPlane(Transform parent, Material mat)
    {
        var go = new GameObject("UpgradeCarpet");
        go.transform.SetParent(parent, false);

        var mf = go.AddComponent<MeshFilter>();
        mf.sharedMesh = CreatePlaneMesh();

        var mr = go.AddComponent<MeshRenderer>();
        mr.sharedMaterial = mat;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
        mr.receiveShadows = true;

        return go;
    }

    static Mesh CreatePlaneMesh()
    {
        var mesh = new Mesh();
        mesh.name = "Plane";
        mesh.vertices = new Vector3[]
        {
            new Vector3(-0.5f, 0, -0.5f),
            new Vector3(0.5f, 0, -0.5f),
            new Vector3(-0.5f, 0, 0.5f),
            new Vector3(0.5f, 0, 0.5f)
        };
        mesh.uv = new Vector2[]
        {
            new Vector2(0, 0),
            new Vector2(1, 0),
            new Vector2(0, 1),
            new Vector2(1, 1)
        };
        mesh.triangles = new int[] { 0, 2, 1, 2, 3, 1 };
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    [MenuItem("Tools/WoodLand3D/UpgradeCarpet 프리팹 생성")]
    public static void CreateUpgradeCarpetPrefab()
    {
        var mat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Textures/UpgradeCarpet.mat");
        if (mat == null)
        {
            Debug.LogError("UpgradeCarpet.mat을 찾을 수 없습니다.");
            return;
        }

        var go = new GameObject("UpgradeCarpet");
        var mf = go.AddComponent<MeshFilter>();
        mf.sharedMesh = CreatePlaneMesh();
        var mr = go.AddComponent<MeshRenderer>();
        mr.sharedMaterial = mat;
        go.transform.localRotation = Quaternion.Euler(-90, 0, 0);
        go.transform.localScale = new Vector3(4, 1, 3);

        if (!System.IO.Directory.Exists(System.IO.Path.Combine(Application.dataPath, "MyPrefab")))
            AssetDatabase.CreateFolder("Assets", "MyPrefab");

        var path = $"{PrefabFolder}/UpgradeCarpet.prefab";
        PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
        AssetDatabase.Refresh();
        Debug.Log($"UpgradeCarpet 프리팹 생성됨: {path}");
    }
}

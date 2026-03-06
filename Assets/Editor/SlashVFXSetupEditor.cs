using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// GameManager에 Slash Fire VFX 프리팹을 자동 연결합니다.
/// </summary>
public static class SlashVFXSetupEditor
{
    const string SlashFireVFXPath = "Assets/Free Slash VFX/Prefabs/Slash Fire VFX.prefab";

    [MenuItem("Tools/WoodLand3D/Slash Fire VFX 도끼 이펙트 연결")]
    public static void ConnectSlashFireVFX()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(SlashFireVFXPath);
        if (prefab == null)
        {
            Debug.LogError($"Slash Fire VFX를 찾을 수 없습니다: {SlashFireVFXPath}");
            return;
        }

        var gm = Object.FindFirstObjectByType<GameManager>();
        if (gm == null)
        {
            Debug.LogWarning("씬에 GameManager가 없습니다.");
            return;
        }

        gm.axeHitVfxPrefab = prefab;
        EditorUtility.SetDirty(gm);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("Slash Fire VFX가 GameManager.axeHitVfxPrefab에 연결되었습니다. 씬을 저장하세요 (Ctrl+S)");
    }
}

using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 씬 파일의 스크립트 참조를 현재 스크립트 GUID로 자동 복구합니다.
/// </summary>
public class FixSceneScriptReferences : EditorWindow
{
    [MenuItem("Tools/WoodLand3D/씬 스크립트 참조 복구")]
    public static void ShowWindow()
    {
        GetWindow<FixSceneScriptReferences>("씬 스크립트 복구");
    }

    void OnGUI()
    {
        GUILayout.Label("씬의 스크립트 참조를 복구합니다", EditorStyles.boldLabel);
        GUILayout.Space(10);

        if (GUILayout.Button("현재 씬의 스크립트 참조 복구", GUILayout.Height(30)))
        {
            FixCurrentScene();
        }

        GUILayout.Space(10);

        if (GUILayout.Button("모든 씬의 스크립트 참조 복구", GUILayout.Height(30)))
        {
            FixAllScenes();
        }
    }

    static void FixCurrentScene()
    {
        var scene = EditorSceneManager.GetActiveScene();
        if (!scene.isLoaded)
        {
            EditorUtility.DisplayDialog("오류", "씬이 열려있지 않습니다.", "확인");
            return;
        }

        FixScene(scene);
        EditorUtility.DisplayDialog("완료", "현재 씬의 스크립트 참조를 복구했습니다.", "확인");
    }

    static void FixAllScenes()
    {
        string[] sceneGuids = AssetDatabase.FindAssets("t:Scene", new[] { "Assets/MyScenes" });
        int fixedCount = 0;

        foreach (string guid in sceneGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            if (FixScene(scene))
            {
                fixedCount++;
            }
        }

        EditorUtility.DisplayDialog("완료", $"{fixedCount}개의 씬에서 스크립트 참조를 복구했습니다.", "확인");
    }

    static bool FixScene(UnityEngine.SceneManagement.Scene scene)
    {
        bool hasChanges = false;
        var scriptGuidMap = BuildScriptGuidMap();

        // 씬의 모든 GameObject에서 MonoBehaviour 컴포넌트 찾기
        GameObject[] rootObjects = scene.GetRootGameObjects();
        foreach (GameObject root in rootObjects)
        {
            MonoBehaviour[] components = root.GetComponentsInChildren<MonoBehaviour>(true);
            foreach (MonoBehaviour mb in components)
            {
                if (mb == null) continue;

                System.Type scriptType = mb.GetType();
                string typeName = scriptType.Name;

                // 스크립트 GUID 맵에서 찾기
                if (scriptGuidMap.ContainsKey(typeName))
                {
                    string correctGuid = scriptGuidMap[typeName];
                    
                    // SerializedObject를 사용하여 GUID 업데이트
                    SerializedObject so = new SerializedObject(mb);
                    SerializedProperty scriptProp = so.FindProperty("m_Script");
                    
                    if (scriptProp != null)
                    {
                        string currentGuid = scriptProp.FindPropertyRelative("guid")?.stringValue;
                        
                        if (currentGuid != correctGuid)
                        {
                            // GUID 업데이트
                            var scriptAsset = AssetDatabase.LoadAssetAtPath<MonoScript>(
                                AssetDatabase.GUIDToAssetPath(correctGuid));
                            
                            if (scriptAsset != null)
                            {
                                scriptProp.objectReferenceValue = scriptAsset;
                                so.ApplyModifiedProperties();
                                hasChanges = true;
                                Debug.Log($"복구됨: {root.name}.{typeName} -> {correctGuid}");
                            }
                        }
                    }
                }
            }
        }

        if (hasChanges)
        {
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        return hasChanges;
    }

    /// <summary>
    /// 모든 스크립트 파일의 타입 이름과 GUID를 매핑합니다.
    /// </summary>
    static Dictionary<string, string> BuildScriptGuidMap()
    {
        var map = new Dictionary<string, string>();

        // MyScript 폴더의 모든 스크립트 찾기
        string[] scriptGuids = AssetDatabase.FindAssets("t:MonoScript", new[] { "Assets/MyScript" });
        
        foreach (string guid in scriptGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            MonoScript script = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
            
            if (script != null)
            {
                System.Type type = script.GetClass();
                if (type != null)
                {
                    map[type.Name] = guid;
                }
            }
        }

        // 백업 씬의 GUID를 현재 GUID로 매핑 (스크립트가 새로 생성된 경우)
        // 백업 씬 GUID -> 현재 GUID 매핑
        var backupGuidMap = new Dictionary<string, string>
        {
            // StageIconPanelController
            { "2c0d4f66e1b717245a210efd4ac39073", GetCurrentGuid("StageIconPanelController") },
            // WeaponSwitchPanelController
            { "49cb4fdb5805ba343a2dc77aad71d380", GetCurrentGuid("WeaponSwitchPanelController") },
            // ItemCollectorController
            { "10dd96df5b1ff36459830d195a8ea18d", GetCurrentGuid("ItemCollectorController") }
        };

        // 백업 GUID로 씬을 검색하고 현재 GUID로 교체
        foreach (var kvp in backupGuidMap)
        {
            if (!string.IsNullOrEmpty(kvp.Value))
            {
                // 씬 파일에서 백업 GUID를 찾아 현재 GUID로 교체
                ReplaceGuidInScene(EditorSceneManager.GetActiveScene(), kvp.Key, kvp.Value);
            }
        }

        return map;
    }

    static string GetCurrentGuid(string className)
    {
        string[] guids = AssetDatabase.FindAssets($"t:MonoScript {className}", new[] { "Assets/MyScript" });
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            MonoScript script = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
            if (script != null && script.GetClass() != null && script.GetClass().Name == className)
            {
                return guid;
            }
        }
        return null;
    }

    static void ReplaceGuidInScene(UnityEngine.SceneManagement.Scene scene, string oldGuid, string newGuid)
    {
        if (string.IsNullOrEmpty(newGuid)) return;

        string scenePath = scene.path;
        if (string.IsNullOrEmpty(scenePath)) return;

        string sceneContent = System.IO.File.ReadAllText(scenePath);
        if (sceneContent.Contains(oldGuid))
        {
            sceneContent = sceneContent.Replace($"guid: {oldGuid}", $"guid: {newGuid}");
            System.IO.File.WriteAllText(scenePath, sceneContent);
            AssetDatabase.Refresh();
            Debug.Log($"씬에서 GUID 교체: {oldGuid} -> {newGuid}");
        }
    }
}

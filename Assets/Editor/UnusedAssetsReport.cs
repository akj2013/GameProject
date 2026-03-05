using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;

/// <summary>
/// MyOriginalScene01에서 참조하지 않는 에셋 목록을 뽑습니다.
/// 다른 씬은 삭제할 예정이므로, 이 씬 기준으로 "삭제 후보"를 정리합니다.
/// </summary>
public static class UnusedAssetsReport
{
    const string ScenePath = "Assets/MyScenes/MyOriginalScene01.unity";
    const string ReportPath = "Assets/UnusedAssetsReport.txt";

    [MenuItem("Tools/WoodLand3D/Unused Assets Report (MyOriginalScene01 only)")]
    public static void GenerateReport()
    {
        if (!File.Exists(ScenePath))
        {
            EditorUtility.DisplayDialog("에러", $"씬을 찾을 수 없습니다: {ScenePath}", "확인");
            return;
        }

        EditorUtility.DisplayProgressBar("Unused Assets Report", "의존성 수집 중...", 0f);
        try
        {
            // 1) MyOriginalScene01이 직접·간접 참조하는 모든 에셋
            var used = new HashSet<string>(AssetDatabase.GetDependencies(ScenePath, true));
            used.Add(ScenePath);
            used.Add(ScenePath + ".meta");

            // 2) Assets/ 아래 모든 에셋
            var allGuids = AssetDatabase.FindAssets("", new[] { "Assets" });
            var allPaths = new HashSet<string>();
            foreach (var guid in allGuids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (string.IsNullOrEmpty(path) || path.StartsWith("Assets/") == false) continue;
                allPaths.Add(path);
                var meta = path + ".meta";
                if (!allPaths.Contains(meta))
                    allPaths.Add(meta);
            }

            // 3) 사용 안 하는 것 = 삭제 후보
            var unused = new List<string>();
            foreach (var p in allPaths)
            {
                if (used.Contains(p)) continue;
                if (p.EndsWith(".meta") && used.Contains(p.Replace(".meta", ""))) continue;
                unused.Add(p);
            }

            // 4) 폴더별로 그룹화
            var byFolder = new Dictionary<string, List<string>>();
            foreach (var path in unused.OrderBy(x => x))
            {
                var parts = path.Replace("\\", "/").Split('/');
                if (parts.Length < 2) continue;
                var folder = parts.Length >= 3 ? $"{parts[0]}/{parts[1]}" : $"{parts[0]}";
                if (!byFolder.ContainsKey(folder))
                    byFolder[folder] = new List<string>();
                byFolder[folder].Add(path);
            }

            // 5) 리포트 작성
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("========================================");
            sb.AppendLine("MyOriginalScene01 기준 미사용 에셋 리포트");
            sb.AppendLine("========================================");
            sb.AppendLine();
            sb.AppendLine($"[기준 씬] {ScenePath}");
            sb.AppendLine($"[사용 에셋 수] {used.Count}");
            sb.AppendLine($"[전체 에셋 수] {allPaths.Count}");
            sb.AppendLine($"[미사용 후보 수] {unused.Count}");
            sb.AppendLine();
            sb.AppendLine("※ Resources/ 에서 런타임 로드하는 에셋은 '미사용'으로 나올 수 있습니다.");
            sb.AppendLine("※ Editor 전용 스크립트도 '미사용'으로 나올 수 있으니 삭제 전 확인하세요.");
            sb.AppendLine();
            sb.AppendLine("========================================");
            sb.AppendLine("폴더별 미사용 에셋 (삭제 후보)");
            sb.AppendLine("========================================");

            foreach (var kv in byFolder.OrderBy(x => x.Key))
            {
                var folder = kv.Key;
                var list = kv.Value;
                sb.AppendLine();
                sb.AppendLine($"--- {folder} ({list.Count}개) ---");
                foreach (var p in list.Take(200))
                    sb.AppendLine("  " + p);
                if (list.Count > 200)
                    sb.AppendLine($"  ... 외 {list.Count - 200}개");
            }

            sb.AppendLine();
            sb.AppendLine("========================================");
            sb.AppendLine("삭제해도 되는 폴더 요약 (해당 폴더 전체가 미사용인 경우)");
            sb.AppendLine("========================================");
            var fullUnusedFolders = new List<string>();
            foreach (var kv in byFolder.OrderBy(x => x.Key))
            {
                var folder = kv.Key;
                var list = kv.Value;
                bool anyUsed = used.Any(u => u.Replace("\\", "/").StartsWith(folder + "/") || u == folder);
                if (!anyUsed && list.Count > 0)
                    fullUnusedFolders.Add(folder);
            }
            foreach (var f in fullUnusedFolders)
                sb.AppendLine("  " + f);

            var report = sb.ToString();
            var dir = Path.GetDirectoryName(ReportPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            File.WriteAllText(ReportPath, report, System.Text.Encoding.UTF8);
            AssetDatabase.Refresh();

            Debug.Log($"[UnusedAssetsReport] 저장됨: {ReportPath}\n미사용 후보 {unused.Count}개, 전체 미사용 폴더 {fullUnusedFolders.Count}개");
            EditorUtility.DisplayDialog("완료", $"리포트 저장됨: {ReportPath}\n\n미사용 후보: {unused.Count}개\n전체 삭제 가능 폴더: {fullUnusedFolders.Count}개", "확인");
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }
    }
}

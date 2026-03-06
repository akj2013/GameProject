using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// GoldCoin, GoldCoinPile, 업그레이드 아이콘 이미지에서 체커보드 패턴을 투명으로 교체합니다.
/// 이미지 편집기에서 투명 영역을 체커보드로 저장했을 때 사용.
/// </summary>
public static class FixCheckerboardTextures
{
    [MenuItem("Tools/WoodLand3D/금화 텍스처 - 체커보드 제거")]
    public static void RemoveCheckerboard()
    {
        RemoveCheckerboardFromFiles(new[] { "GoldCoin.png", "GoldCoinPile.png" });
    }

    [MenuItem("Tools/WoodLand3D/업그레이드 아이콘 - 체커보드 제거 (ShoeIcon 등)")]
    public static void RemoveCheckerboardFromUpgradeIcons()
    {
        RemoveCheckerboardFromFiles(new[] { "ShoeIcon.png", "AxeIcon.png", "StackIcon.png" });
    }

    [MenuItem("Tools/WoodLand3D/UpgradePanel_MoveSpeed - 체커보드 제거")]
    public static void RemoveCheckerboardFromMoveSpeedPanel()
    {
        RemoveCheckerboardFromFiles(new[] { "UpgradePanel_MoveSpeed.png" });
    }

    static void RemoveCheckerboardFromFiles(string[] fileNames)
    {
        var baseDir = Path.Combine(Application.dataPath, "Resources", "UI");

        foreach (var fileName in fileNames)
        {
            var fullPath = Path.Combine(baseDir, fileName);

            if (!File.Exists(fullPath))
            {
                Debug.LogWarning($"파일 없음: {fullPath}");
                continue;
            }

            var bytes = File.ReadAllBytes(fullPath);
            var tex = new Texture2D(2, 2);
            if (!tex.LoadImage(bytes))
            {
                Debug.LogWarning($"로드 실패: {fileName}");
                continue;
            }

            var pixels = tex.GetPixels32();
            int w = tex.width, h = tex.height;
            int replaced = 0;

            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                int i = y * w + x;
                var p = pixels[i];
                if (IsCheckerboardColor(pixels, x, y, w, h))
                {
                    pixels[i] = new Color32(p.r, p.g, p.b, 0);
                    replaced++;
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply();

            var png = tex.EncodeToPNG();
            Object.DestroyImmediate(tex);

            var fixedPath = Path.Combine(baseDir, Path.GetFileNameWithoutExtension(fileName) + "_fixed.png");
            var backupPath = fullPath + ".bak";
            File.WriteAllBytes(fixedPath, png);
            try
            {
                if (!File.Exists(backupPath)) File.Copy(fullPath, backupPath);
                File.Copy(fixedPath, fullPath, true);
                File.Delete(fixedPath);
                Debug.Log($"{fileName}: 체커보드 픽셀 {replaced}개를 투명으로 교체했습니다.");
            }
            catch (System.IO.IOException)
            {
                Debug.LogWarning($"파일이 사용 중입니다. '{Path.GetFileName(fixedPath)}'로 저장됨. Unity 종료 → 해당 파일을 '{fileName}'로 이름 변경 → Unity 재실행");
            }
        }

        AssetDatabase.Refresh();
        Debug.Log("텍스처 수정 완료. 씬을 다시 실행해 확인하세요.");
    }

    /// <summary>
    /// 회색/흰색(투명 배경용 체커보드) 픽셀인지 판단. 금화 색은 제외.
    /// </summary>
    static bool IsCheckerboardColor(Color32[] pixels, int x, int y, int w, int h)
    {
        var c = pixels[y * w + x];

        const int grayTolerance = 40;
        bool isGray = Mathf.Abs(c.r - c.g) <= grayTolerance &&
                      Mathf.Abs(c.g - c.b) <= grayTolerance &&
                      Mathf.Abs(c.r - c.b) <= grayTolerance;

        if (!isGray) return false;

        int v = (c.r + c.g + c.b) / 3;
        int sat = Mathf.Max(c.r, c.g, c.b) - Mathf.Min(c.r, c.g, c.b);
        if (sat > 50) return false;

        return v >= 40;
    }
}

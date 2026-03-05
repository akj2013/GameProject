using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// 업그레이드 패널용 플레이스홀더 아이콘 생성.
/// Resources/UI에 AxeIcon, ShoeIcon, StackIcon PNG 생성.
/// </summary>
public static class CreateUpgradeIcons
{
    [MenuItem("Tools/WoodLand3D/업그레이드 아이콘 플레이스홀더 생성")]
    public static void CreatePlaceholders()
    {
        var dir = Path.Combine(Application.dataPath, "Resources", "UI");
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

        CreateIcon(Path.Combine(dir, "AxeIcon.png"), new Color(0.8f, 0.5f, 0.2f));
        CreateIcon(Path.Combine(dir, "ShoeIcon.png"), new Color(0.4f, 0.25f, 0.15f));
        CreateIcon(Path.Combine(dir, "StackIcon.png"), new Color(0.85f, 0.6f, 0.2f));

        AssetDatabase.Refresh();

        foreach (var name in new[] { "AxeIcon", "ShoeIcon", "StackIcon" })
        {
            var importer = AssetImporter.GetAtPath("Assets/Resources/UI/" + name + ".png") as TextureImporter;
            if (importer != null && importer.textureType != TextureImporterType.Sprite)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.SaveAndReimport();
            }
        }
        Debug.Log("플레이스홀더 아이콘 생성 완료. Resources/UI/AxeIcon, ShoeIcon, StackIcon. 나중에 교체 가능.");
    }

    static void CreateIcon(string path, Color color)
    {
        if (File.Exists(path)) return;

        var tex = new Texture2D(32, 32);
        for (int y = 0; y < 32; y++)
        for (int x = 0; x < 32; x++)
        {
            float dx = (x - 16f) / 16f;
            float dy = (y - 16f) / 16f;
            float d = Mathf.Sqrt(dx * dx + dy * dy);
            float a = d < 0.9f ? 1f : (d < 1f ? 1f - (d - 0.9f) * 10f : 0f);
            tex.SetPixel(x, y, new Color(color.r, color.g, color.b, a));
        }
        tex.Apply();
        File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
    }
}

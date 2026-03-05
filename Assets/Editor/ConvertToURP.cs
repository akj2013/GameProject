using UnityEngine;
using UnityEditor;

/// <summary>
/// Built-in 머티리얼을 URP로 변환하는 에디터 스크립트.
/// 메뉴: Tools -> Convert Materials to URP
/// </summary>
public static class ConvertToURP
{
    const string URP_LIT_GUID = "933532a4fcc9baf4fa0491de14d08ed7";

    [MenuItem("Tools/Convert Materials to URP")]
    public static void ConvertAllMaterials()
    {
        var urpLit = AssetDatabase.LoadAssetAtPath<Shader>(
            AssetDatabase.GUIDToAssetPath(URP_LIT_GUID));
        if (urpLit == null)
        {
            urpLit = Shader.Find("Universal Render Pipeline/Lit");
            if (urpLit == null)
            {
                Debug.LogError("URP Lit 셰이더를 찾을 수 없습니다.");
                return;
            }
        }

        string[] matGuids = AssetDatabase.FindAssets("t:Material", new[] { "Assets" });
        int converted = 0;

        foreach (string guid in matGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null || mat.shader == null) continue;

            string shaderName = mat.shader.name;
            // 이미 URP 셰이더면 스킵
            if (shaderName.Contains("Universal Render Pipeline")) continue;

            // 분홍(에러) / Built-in / 기타 전부 URP Lit으로 변환
            ConvertMaterialToURP(mat, urpLit);
            EditorUtility.SetDirty(mat);
            converted++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"URP 변환 완료: {converted}개 머티리얼 변환됨");
    }

    static void ConvertMaterialToURP(Material mat, Shader urpLit)
    {
        Texture mainTex = null;
        if (mat.HasProperty("_MainTex")) mainTex = mat.GetTexture("_MainTex");
        if (mainTex == null && mat.HasProperty("_BaseMap")) mainTex = mat.GetTexture("_BaseMap");
        if (mainTex == null && mat.HasProperty("_BaseColorMap")) mainTex = mat.GetTexture("_BaseColorMap");

        Color color = Color.white;
        if (mat.HasProperty("_Color")) color = mat.GetColor("_Color");
        else if (mat.HasProperty("_BaseColor")) color = mat.GetColor("_BaseColor");

        mat.shader = urpLit;

        if (mat.HasProperty("_BaseMap") && mainTex != null)
            mat.SetTexture("_BaseMap", mainTex);
        if (mat.HasProperty("_BaseColor"))
            mat.SetColor("_BaseColor", color);
    }
}

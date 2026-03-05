using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// 현재 씬(MyOriginalScene01 등)에 있는 모든 렌더러의 머티리얼 사용량과 예상 DrawCall을 집계합니다.
/// </summary>
public static class SceneMaterialAndDrawCallReport
{
    [MenuItem("Tools/WoodLand3D/Scene Material and DrawCall Report")]
    public static void Report()
    {
        var scene = EditorSceneManager.GetActiveScene();
        if (!scene.isLoaded)
        {
            EditorUtility.DisplayDialog("씬 리포트", "씬이 로드되어 있지 않습니다.", "확인");
            return;
        }

        var roots = scene.GetRootGameObjects();
        var uniqueMaterials = new HashSet<int>();
        int meshRendererCount = 0;
        int skinnedMeshRendererCount = 0;
        int spriteRendererCount = 0;
        int otherRendererCount = 0;
        int totalMaterialSlots = 0;

        foreach (var root in roots)
        {
            var renderers = root.GetComponentsInChildren<Renderer>(true);
            foreach (var r in renderers)
            {
                if (r is MeshRenderer) meshRendererCount++;
                else if (r is SkinnedMeshRenderer) skinnedMeshRendererCount++;
                else if (r is SpriteRenderer) spriteRendererCount++;
                else otherRendererCount++;

                var mats = r.sharedMaterials;
                if (mats != null)
                {
                    for (int i = 0; i < mats.Length; i++)
                    {
                        if (mats[i] != null)
                        {
                            uniqueMaterials.Add(mats[i].GetInstanceID());
                            totalMaterialSlots++;
                        }
                    }
                }
            }
        }

        int totalRenderers = meshRendererCount + skinnedMeshRendererCount + spriteRendererCount + otherRendererCount;
        int uniqueMaterialCount = uniqueMaterials.Count;

        // DrawCall 추정:
        // - 배칭 없을 때: 각 렌더러·서브메시별로 1 DrawCall → 상한 ≈ totalMaterialSlots (또는 렌더러 수)
        // - 동일 머티리얼 배칭 시: 고유 머티리얼 종류당 최소 1 DrawCall. 스프라이트는 보통 하나의 머티리얼로 많이 배칭됨.
        int estimatedDrawCallUpper = totalMaterialSlots > 0 ? totalMaterialSlots : totalRenderers;
        int estimatedDrawCallByUniqueMaterials = uniqueMaterialCount;

        string report = $"[씬: {scene.name}]\n\n" +
            $"■ 렌더러 수\n" +
            $"  MeshRenderer: {meshRendererCount}\n" +
            $"  SkinnedMeshRenderer: {skinnedMeshRendererCount}\n" +
            $"  SpriteRenderer: {spriteRendererCount}\n" +
            $"  기타 Renderer: {otherRendererCount}\n" +
            $"  총 렌더러 수: {totalRenderers}\n\n" +
            $"■ 머티리얼\n" +
            $"  씬에서 사용 중인 고유 머티리얼 수: {uniqueMaterialCount}개\n" +
            $"  머티리얼 슬롯 합계(렌더러×재질): {totalMaterialSlots}\n\n" +
            $"■ 예상 DrawCall\n" +
            $"  배칭 시 (고유 머티리얼 기준): 약 {estimatedDrawCallByUniqueMaterials}회 이상\n" +
            $"  배칭 없을 때 상한: 약 {estimatedDrawCallUpper}회 이하\n" +
            $"  (스프라이트는 동일 머티리얼이면 배칭되어 실제 DrawCall은 더 적을 수 있음)";

        Debug.Log(report);
        EditorUtility.DisplayDialog("씬 머티리얼·DrawCall 리포트", report, "확인");
    }
}

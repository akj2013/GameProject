using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

/// <summary>
/// Hexa_Stage_Exp_01(WorldMap)의 간격을 기준으로 헥사 타일 격자를 자동 생성하는 에디터 도구.
/// 중앙부터 밖으로 확장되는 Level 기반 생성 방식.
/// 빈 오브젝트 구조: 부모 → 레벨별 빈 오브젝트 → 타일들
/// </summary>
public class HexTileGridGenerator : EditorWindow
{
    [Header("타일 설정")]
    GameObject tilePrefab;
    int maxLevel = 2;  // Level 0부터 Level maxLevel까지 생성
    
    [Header("간격 설정")]
    [Tooltip("HexGridManager 방식 사용 (면 인접 보장)")]
    float hexSize = 2.886f;      // Flat-Top 기준 한 변 길이
    
    [Tooltip("기존 Spacing 방식 사용 (비권장 - 꼭지점만 맞닿을 수 있음)")]
    bool useSpacingMethod = false;
    float xSpacing = 4.33f;      // 같은 행에서 옆으로 한 칸
    float zSpacing = 3.751f;     // 다음 행으로 한 칸
    float xOffset = 2.165f;      // 홀수 행의 X 오프셋 (4.33의 절반)
    
    [Header("생성 옵션")]
    bool createLevelParents = true;  // 레벨별 빈 오브젝트 생성 여부
    string parentName = "Hexa_Stage_Exp_01(WorldMap)";
    string levelNamePrefix = "Level_";
    
    Vector2 scrollPos;

    [MenuItem("Tools/WoodLand3D/헥사 타일 격자 생성기")]
    public static void ShowWindow()
    {
        GetWindow<HexTileGridGenerator>("헥사 타일 격자 생성기");
    }

    void OnGUI()
    {
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
        
        EditorGUILayout.LabelField("헥사 타일 격자 생성기", EditorStyles.boldLabel);
        EditorGUILayout.Space(10);
        
        // 타일 프리팹 선택
        EditorGUILayout.LabelField("타일 설정", EditorStyles.boldLabel);
        tilePrefab = (GameObject)EditorGUILayout.ObjectField(
            "타일 프리팹", 
            tilePrefab, 
            typeof(GameObject), 
            false
        );
        
        maxLevel = EditorGUILayout.IntField("최대 Level", maxLevel);
        maxLevel = Mathf.Max(0, maxLevel);
        
        EditorGUILayout.HelpBox(
            $"Level 0: 중앙 1개\n" +
            $"Level 1: 주위 6개\n" +
            $"Level 2: 그 다음 12개\n" +
            $"Level n: 6×n개\n" +
            $"총 타일 수: {GetTotalTileCount(maxLevel)}개",
            MessageType.Info
        );
        
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("간격 설정", EditorStyles.boldLabel);
        hexSize = EditorGUILayout.FloatField("Hex Size (circumradius)", hexSize);
        
        // 타일 프리팹이 선택되어 있으면 실제 크기 기반 권장값 계산
        if (tilePrefab != null)
        {
            Bounds bounds = GetPrefabBounds(tilePrefab);
            if (bounds.size.z > 0) // Z축 크기가 있으면
            {
                // Flat-top 헥사에서 높이(flat-to-flat) = hexSize * 2 * sqrt(3)
                // 따라서 hexSize = 높이 / (2 * sqrt(3))
                float recommendedHexSize = bounds.size.z / (2f * Mathf.Sqrt(3f));
                EditorGUILayout.HelpBox(
                    $"타일 실제 크기: X={bounds.size.x:F3}, Z={bounds.size.z:F3}\n" +
                    $"권장 Hex Size: {recommendedHexSize:F3} (타일 높이 기반)\n" +
                    $"현재 값: {hexSize:F3}",
                    MessageType.Info
                );
                
                if (GUILayout.Button($"권장값으로 설정 ({recommendedHexSize:F3})"))
                {
                    hexSize = recommendedHexSize;
                }
            }
            else
            {
                EditorGUILayout.HelpBox(
                    "Hex Size 방식 사용 시 타일들이 면으로 인접하여 빈공간이 생기지 않습니다.\n" +
                    $"현재 값: {hexSize:F3}",
                    MessageType.Info
                );
            }
        }
        else
        {
            EditorGUILayout.HelpBox(
                "Hex Size 방식 사용 시 타일들이 면으로 인접하여 빈공간이 생기지 않습니다.\n" +
                $"현재 값: {hexSize:F3} (타일 프리팹을 선택하면 권장값이 표시됩니다)",
                MessageType.Info
            );
        }
        
        EditorGUILayout.Space(5);
        useSpacingMethod = EditorGUILayout.Toggle("기존 Spacing 방식 사용 (비권장)", useSpacingMethod);
        if (useSpacingMethod)
        {
            EditorGUILayout.HelpBox("경고: Spacing 방식은 꼭지점만 맞닿을 수 있습니다.", MessageType.Warning);
            xSpacing = EditorGUILayout.FloatField("X 간격 (같은 행)", xSpacing);
            zSpacing = EditorGUILayout.FloatField("Z 간격 (다음 행)", zSpacing);
            xOffset = EditorGUILayout.FloatField("X 오프셋 (홀수 행)", xOffset);
        }
        
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("생성 옵션", EditorStyles.boldLabel);
        createLevelParents = EditorGUILayout.Toggle("레벨별 빈 오브젝트 생성", createLevelParents);
        parentName = EditorGUILayout.TextField("부모 오브젝트 이름", parentName);
        levelNamePrefix = EditorGUILayout.TextField("레벨 이름 접두사", levelNamePrefix);
        
        EditorGUILayout.Space(20);
        
        GUI.enabled = tilePrefab != null && maxLevel >= 0;
        if (GUILayout.Button("격자 생성", GUILayout.Height(30)))
        {
            GenerateGrid();
        }
        GUI.enabled = true;
        
        EditorGUILayout.Space(10);
        
        if (GUILayout.Button("선택된 오브젝트에서 간격 측정", GUILayout.Height(25)))
        {
            MeasureSpacing();
        }
        
        EditorGUILayout.EndScrollView();
    }

    void GenerateGrid()
    {
        if (tilePrefab == null)
        {
            EditorUtility.DisplayDialog("오류", "타일 프리팹을 선택해주세요.", "확인");
            return;
        }

        // 부모 오브젝트 찾기 또는 생성
        GameObject parentObj = GameObject.Find(parentName);
        if (parentObj == null)
        {
            parentObj = new GameObject(parentName);
            Undo.RegisterCreatedObjectUndo(parentObj, "Create Hex Grid Parent");
        }

        // 기존 타일들 제거 옵션
        if (parentObj.transform.childCount > 0)
        {
            if (!EditorUtility.DisplayDialog(
                "기존 오브젝트 제거",
                $"'{parentName}'에 이미 자식 오브젝트가 있습니다. 모두 제거하고 새로 생성하시겠습니까?",
                "제거하고 생성",
                "취소"))
            {
                return;
            }

            // 기존 자식들 제거
            while (parentObj.transform.childCount > 0)
            {
                Undo.DestroyObjectImmediate(parentObj.transform.GetChild(0).gameObject);
            }
        }

        // 레벨별로 타일 생성 (면 인접 방식 사용)
        HashSet<Vector3Int> allCreatedTiles = new HashSet<Vector3Int>(); // 전체 생성된 타일 (중복 방지용)
        HashSet<Vector3Int> previousLevelTiles = new HashSet<Vector3Int>(); // 이전 레벨의 타일들
        int totalTiles = 0;

        for (int level = 0; level <= maxLevel; level++)
        {
            GameObject levelParent = null;
            
            if (createLevelParents)
            {
                levelParent = new GameObject($"{levelNamePrefix}{level}");
                levelParent.transform.SetParent(parentObj.transform);
                levelParent.transform.localPosition = Vector3.zero;
                Undo.RegisterCreatedObjectUndo(levelParent, "Create Level Parent");
            }

            // 해당 레벨의 타일들 가져오기 (면 인접 방식 사용)
            List<Vector3Int> levelTiles = GetTilesForLevel(level, previousLevelTiles);
            
            // 디버그: 레벨별 타일 개수 확인
            Debug.Log($"Level {level}: {levelTiles.Count}개 타일 생성 예정");
            
            // Level 1일 때 상세 디버그
            if (level == 1)
            {
                Debug.Log($"Level 1 타일 목록:");
                foreach (var tile in levelTiles)
                {
                    Vector3 pos = CubeToWorldPosition(tile);
                    Debug.Log($"  {tile} -> 월드 위치: ({pos.x:F3}, {pos.z:F3})");
                }
            }
            
            // Level 1 이상일 때 면 인접 검증
            if (level > 0 && previousLevelTiles.Count > 0)
            {
                int notAdjacentCount = 0;
                foreach (var tile in levelTiles)
                {
                    bool isAdjacent = false;
                    int minDistance = int.MaxValue;
                    
                    foreach (var prevTile in previousLevelTiles)
                    {
                        int dist = CubeDistance(tile, prevTile);
                        if (dist < minDistance) minDistance = dist;
                        if (dist == 1) // 면으로 인접 (거리 1)
                        {
                            isAdjacent = true;
                            break;
                        }
                    }
                    
                    if (!isAdjacent)
                    {
                        notAdjacentCount++;
                        if (notAdjacentCount <= 5) // 처음 5개만 로그 출력
                        {
                            Vector3 tilePos = CubeToWorldPosition(tile);
                            Debug.LogWarning($"경고: Level {level} 타일 {tile} (월드 위치: {tilePos})이 이전 레벨과 면으로 인접하지 않습니다! (최소 거리: {minDistance})");
                        }
                    }
                }
                
                if (notAdjacentCount > 0)
                {
                    Debug.LogWarning($"Level {level}: 총 {notAdjacentCount}개 타일이 이전 레벨과 면으로 인접하지 않습니다.");
                }
            }
            
            foreach (Vector3Int cubeCoord in levelTiles)
            {
                if (allCreatedTiles.Contains(cubeCoord))
                {
                    Debug.LogWarning($"중복 타일 발견: {cubeCoord} (레벨 {level})");
                    continue;
                }
                
                allCreatedTiles.Add(cubeCoord);
                previousLevelTiles.Add(cubeCoord); // 다음 레벨을 위해 추가
                
                // 큐브 좌표를 월드 위치로 변환
                Vector3 worldPos = CubeToWorldPosition(cubeCoord);
                
                GameObject tile = PrefabUtility.InstantiatePrefab(tilePrefab) as GameObject;
                tile.name = $"{tilePrefab.name}_Level{level}_{cubeCoord.x}_{cubeCoord.y}_{cubeCoord.z}";
                
                if (levelParent != null)
                {
                    tile.transform.SetParent(levelParent.transform);
                }
                else
                {
                    tile.transform.SetParent(parentObj.transform);
                }
                
                tile.transform.localPosition = worldPos;
                tile.transform.localRotation = Quaternion.identity;
                tile.transform.localScale = Vector3.one;
                
                Undo.RegisterCreatedObjectUndo(tile, "Create Hex Tile");
                totalTiles++;
            }
        }

        // 씬 뷰 업데이트
        Selection.activeGameObject = parentObj;
        SceneView.FrameLastActiveSceneView();
        
        int expectedCount = GetTotalTileCount(maxLevel);
        Debug.Log($"헥사 타일 격자 생성 완료: Level 0~{maxLevel} = 총 {totalTiles}개 타일 (예상: {expectedCount}개)");
        EditorUtility.DisplayDialog("완료", $"격자 생성 완료!\nLevel 0~{maxLevel}\n총 {totalTiles}개 타일", "확인");
    }

    /// <summary>
    /// 레벨별 총 타일 개수 계산
    /// </summary>
    int GetTotalTileCount(int maxLevel)
    {
        if (maxLevel < 0) return 0;
        if (maxLevel == 0) return 1;
        
        // Level 0: 1개, Level 1~n: 각각 6×level개
        int count = 1; // Level 0
        for (int level = 1; level <= maxLevel; level++)
        {
            count += 6 * level;
        }
        return count;
    }

    // Cube 방향 벡터 (고정) - HexGridManager와 동일
    private readonly Vector3Int[] directions = new Vector3Int[]
    {
        new Vector3Int(1, -1, 0),
        new Vector3Int(1, 0, -1),
        new Vector3Int(0, 1, -1),
        new Vector3Int(-1, 1, 0),
        new Vector3Int(-1, 0, 1),
        new Vector3Int(0, -1, 1)
    };
    
    /// <summary>
    /// 큐브 좌표의 6방향 이웃 반환 (flat-top 헥사 기준)
    /// </summary>
    List<Vector3Int> GetNeighbors(Vector3Int cubeCoord)
    {
        List<Vector3Int> neighbors = new List<Vector3Int>();
        foreach (var dir in directions)
        {
            neighbors.Add(cubeCoord + dir);
        }
        return neighbors;
    }

    /// <summary>
    /// 특정 레벨의 모든 헥사 타일 좌표 반환 (면 인접 방식)
    /// Level 0: 중앙 1개
    /// Level n: Level (n-1)의 모든 타일과 면이 인접한 타일들만 포함 (빈공간 없음)
    /// Flat-top 헥사에서 정확한 면 인접 보장
    /// </summary>
    List<Vector3Int> GetTilesForLevel(int level, HashSet<Vector3Int> previousLevelTiles)
    {
        List<Vector3Int> tiles = new List<Vector3Int>();
        
        if (level == 0)
        {
            // Level 0: 중앙 타일 하나
            tiles.Add(Vector3Int.zero);
            return tiles;
        }
        
        // Level n: Level (n-1)의 모든 타일의 이웃 중에서,
        // Level (n-1)에 속하지 않는 타일들만 포함
        // 이렇게 하면 면이 인접하여 빈공간이 생기지 않음
        HashSet<Vector3Int> levelTilesSet = new HashSet<Vector3Int>();
        
        foreach (Vector3Int prevTile in previousLevelTiles)
        {
            List<Vector3Int> neighbors = GetNeighbors(prevTile);
            
            foreach (Vector3Int neighbor in neighbors)
            {
                // 이전 레벨에 속하지 않는 타일만 추가
                // 이웃이므로 이미 거리 1 (면 인접)임
                if (!previousLevelTiles.Contains(neighbor))
                {
                    levelTilesSet.Add(neighbor);
                }
            }
        }
        
        tiles.AddRange(levelTilesSet);
        return tiles;
    }

    /// <summary>
    /// 큐브 좌표를 월드 위치로 변환 (flat-top 헥사)
    /// Flat-top 헥사에서 면 인접을 보장하는 정확한 공식 사용
    /// </summary>
    Vector3 CubeToWorldPosition(Vector3Int cube)
    {
        if (useSpacingMethod)
        {
            // 방법 1: xSpacing/zSpacing/xOffset 사용 (기존 방식 - 비권장)
            int q = cube.x;
            int r = cube.y;
            
            // 큐브 좌표를 오프셋 좌표로 변환 (flat-top)
            int col = q;
            int row = r + (q + (q & 1)) / 2;
            
            // 홀수 행 여부 확인
            bool isOddRow = (Mathf.Abs(row) % 2 == 1);
            float rowXOffset = isOddRow ? xOffset : 0f;
            
            float x = rowXOffset + (col * xSpacing);
            float z = row * zSpacing;
            
            return new Vector3(x, 0, z);
        }
        else
        {
            // 방법 2: hexSize 사용 (Flat-Top 정석 공식)
            // hexSize는 circumradius (중심에서 꼭지점까지의 거리)
            // Flat-top 헥사에서 면 인접을 보장하는 공식:
            // - X축: 1.5 * hexSize * q (수평 간격)
            // - Z축: sqrt(3) * hexSize * (r + q/2) (수직 간격)
            float x = hexSize * (Mathf.Sqrt(3f) * (cube.y + cube.x * 0.5f)); 
            float z = hexSize * (1.5f * cube.x);

            return new Vector3(x, 0f, z);
        }
    }
    
    /// <summary>
    /// 두 큐브 좌표 간 거리 계산 (HexGridManager와 동일)
    /// </summary>
    int CubeDistance(Vector3Int a, Vector3Int b)
    {
        return (Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y) + Mathf.Abs(a.z - b.z)) / 2;
    }

    /// <summary>
    /// 프리팹의 바운드를 가져옵니다 (에디터에서)
    /// </summary>
    Bounds GetPrefabBounds(GameObject prefab)
    {
        Bounds bounds = new Bounds();
        bool hasBounds = false;
        
        // 프리팹을 임시로 인스턴스화하여 바운드 계산
        GameObject tempInstance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
        if (tempInstance != null)
        {
            Renderer[] renderers = tempInstance.GetComponentsInChildren<Renderer>();
            if (renderers.Length > 0)
            {
                bounds = renderers[0].bounds;
                hasBounds = true;
                
                for (int i = 1; i < renderers.Length; i++)
                {
                    bounds.Encapsulate(renderers[i].bounds);
                }
            }
            
            // 임시 인스턴스 제거
            DestroyImmediate(tempInstance);
        }
        
        if (!hasBounds)
        {
            // 기본값 반환
            bounds = new Bounds(Vector3.zero, new Vector3(4.33f, 1f, 5f));
        }
        
        return bounds;
    }
    
    void MeasureSpacing()
    {
        GameObject[] selected = Selection.gameObjects;
        if (selected.Length < 2)
        {
            EditorUtility.DisplayDialog("오류", "간격을 측정하려면 2개 이상의 타일을 선택해주세요.", "확인");
            return;
        }

        if (selected.Length == 2)
        {
            Vector3 pos1 = selected[0].transform.position;
            Vector3 pos2 = selected[1].transform.position;
            Vector3 diff = pos2 - pos1;
            
            Debug.Log($"타일 간격 측정:\n" +
                     $"타일 1: {pos1}\n" +
                     $"타일 2: {pos2}\n" +
                     $"차이: X={diff.x:F3}, Z={diff.z:F3}");
            
            EditorUtility.DisplayDialog(
                "간격 측정 결과",
                $"타일 간격:\nX: {diff.x:F3}\nZ: {diff.z:F3}",
                "확인"
            );
        }
        else
        {
            // 여러 개 선택된 경우 평균 계산
            float avgX = 0f, avgZ = 0f;
            int count = 0;
            
            for (int i = 0; i < selected.Length - 1; i++)
            {
                for (int j = i + 1; j < selected.Length; j++)
                {
                    Vector3 diff = selected[j].transform.position - selected[i].transform.position;
                    avgX += Mathf.Abs(diff.x);
                    avgZ += Mathf.Abs(diff.z);
                    count++;
                }
            }
            
            if (count > 0)
            {
                avgX /= count;
                avgZ /= count;
                
                Debug.Log($"평균 간격: X={avgX:F3}, Z={avgZ:F3}");
                EditorUtility.DisplayDialog(
                    "평균 간격 측정 결과",
                    $"평균 간격:\nX: {avgX:F3}\nZ: {avgZ:F3}",
                    "확인"
                );
            }
        }
    }
}

using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class HexGridManager : MonoBehaviour
{
    [Header("Grid Settings")]
    [Range(0, 20)]
    public int maxLevel = 3;

    [Tooltip("Hex radius size")]
    public float hexSize = 2.886f;

    [Header("Prefab")]
    public GameObject hexPrefab;

    [Header("Editor")]
    public bool autoUpdateInEditor = false;

    private Dictionary<Vector3Int, GameObject> grid =
        new Dictionary<Vector3Int, GameObject>();

    private readonly Vector3Int[] directions = new Vector3Int[]
    {
        new Vector3Int(1,-1,0),
        new Vector3Int(1,0,-1),
        new Vector3Int(0,1,-1),
        new Vector3Int(-1,1,0),
        new Vector3Int(-1,0,1),
        new Vector3Int(0,-1,1)
    };

    void OnValidate()
    {
        if (!Application.isPlaying && autoUpdateInEditor)
        {
            GenerateGrid();
        }
    }

    #region Generate

    public void GenerateGrid()
    {
        ClearGridImmediate();

        if (hexPrefab == null)
        {
            Debug.LogWarning("Hex Prefab missing.");
            return;
        }

        for (int level = 0; level <= maxLevel; level++)
        {
            var ring = GetRing(level);

            foreach (var cube in ring)
            {
                Vector3 worldPos = CubeToWorld(cube);

#if UNITY_EDITOR
                GameObject tile = (GameObject)UnityEditor.PrefabUtility
                    .InstantiatePrefab(hexPrefab, transform);
#else
                GameObject tile = Instantiate(hexPrefab, transform);
#endif

                tile.transform.position = worldPos;
                tile.name = $"Hex_{cube.x}_{cube.y}_{cube.z}";
                grid.Add(cube, tile);
            }
        }

        Debug.Log($"Generated {grid.Count} hex tiles.");
    }

    public void ClearGridImmediate()
    {
        grid.Clear();

        List<GameObject> children = new List<GameObject>();

        foreach (Transform child in transform)
            children.Add(child.gameObject);

#if UNITY_EDITOR
        foreach (var c in children)
            DestroyImmediate(c);
#else
        foreach (var c in children)
            Destroy(c);
#endif
    }

    #endregion

    #region Correct Ring Logic

    List<Vector3Int> GetRing(int radius)
    {
        List<Vector3Int> results = new List<Vector3Int>();

        if (radius == 0)
        {
            results.Add(Vector3Int.zero);
            return results;
        }

        // 🔥 핵심 수정 부분
        Vector3Int cube = directions[4] * radius;

        for (int i = 0; i < 6; i++)
        {
            for (int j = 0; j < radius; j++)
            {
                results.Add(cube);
                cube += directions[i];
            }
        }

        return results;
    }

    #endregion

    #region Cube → World (Flat Top)

    Vector3 CubeToWorld(Vector3Int cube)
    {
        float size = GetHexRadius();

        float x = size * (Mathf.Sqrt(3f) * (cube.z + cube.x * 0.5f)); 
        float z = size * (1.5f * cube.x);

        return new Vector3(x, 0f, z);
    }
    float GetHexRadius()
    {
        MeshRenderer renderer = hexPrefab.GetComponentInChildren<MeshRenderer>();
        Vector3 bounds = renderer.bounds.size;

        // Flat Top에서 width = 2 * size
        //return bounds.x * 0.5f;
        return hexSize;
    }

    #endregion

    #region Utility

    public int CubeDistance(Vector3Int a, Vector3Int b)
    {
        return (Mathf.Abs(a.x - b.x)
              + Mathf.Abs(a.y - b.y)
              + Mathf.Abs(a.z - b.z)) / 2;
    }

    #endregion
}
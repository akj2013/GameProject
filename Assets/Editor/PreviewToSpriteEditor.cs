using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;

/// <summary>
/// 프리팹/오브젝트를 프리뷰 카메라로 렌더해 PNG 스프라이트로 저장합니다.
/// Tree_PineWinter_Sprite 등 UI용 아이콘을 만들 때 사용 (New Scene → 오브젝트 넣고 → 위치 확인 → 사진 찍기와 동일한 결과).
/// </summary>
public class PreviewToSpriteEditor : EditorWindow
{
    GameObject _targetObject;
    string _outputFileName = "";
    int _textureSize = 256;
    bool _transparentBackground = true;
    Vector3 _cameraOffset = new Vector3(0f, 0.5f, 2f);
    Vector3 _targetLocalOffset = Vector3.zero;
    Vector3 _targetLocalEuler = Vector3.zero;
    const string DefaultOutputDir = "Assets/Resources/UI";

    // 미리보기용 (오프셋 조정 시 실시간 반영)
    GameObject _previewRoot;
    GameObject _previewInstance;
    Camera _previewCamera;
    RenderTexture _previewRT;
    GameObject _previewTarget;
    const int PreviewSize = 280;
    const float PreviewWorldY = 10000f;

    [MenuItem("Tools/WoodLand3D/오브젝트 → PNG 스프라이트 (프리뷰 렌더)")]
    public static void OpenWindow()
    {
        var w = GetWindow<PreviewToSpriteEditor>(true, "프리뷰 → PNG 스프라이트");
        w.minSize = new Vector2(340, 420);
        if (Selection.activeGameObject != null)
        {
            w._targetObject = Selection.activeGameObject;
            w._outputFileName = w.GetDefaultOutputName();
        }
    }

    void OnDisable()
    {
        CleanupPreview();
    }

    void Update()
    {
        if (_previewRT != null)
            Repaint();
    }

    static bool IsPrefabAsset(GameObject go)
    {
        if (go == null) return false;
        return PrefabUtility.IsPartOfPrefabAsset(go) || !go.scene.IsValid();
    }

    void EnsurePreview()
    {
        if (_targetObject == null)
        {
            CleanupPreview();
            return;
        }
        if (!IsPrefabAsset(_targetObject))
        {
            CleanupPreview();
            return;
        }
        if (_previewRoot != null && _previewTarget == _targetObject) return;
        CleanupPreview();
        _previewTarget = _targetObject;

        _previewRoot = new GameObject("PreviewToSprite_Root");
        _previewRoot.transform.position = new Vector3(PreviewWorldY, PreviewWorldY, PreviewWorldY);
        _previewRoot.hideFlags = HideFlags.HideAndDontSave;
        _previewRoot.SetActive(true);

        _previewInstance = (GameObject)PrefabUtility.InstantiatePrefab(_targetObject);
        if (_previewInstance == null) return;
        _previewInstance.transform.SetParent(_previewRoot.transform, false);
        _previewInstance.transform.localPosition = _targetLocalOffset;
        _previewInstance.transform.localRotation = Quaternion.Euler(_targetLocalEuler);

        int previewLayer = LayerMask.NameToLayer("Ignore Raycast");
        if (previewLayer < 0) previewLayer = 0;
        SetLayerRecursively(_previewRoot, previewLayer);

        GameObject camGo = new GameObject("PreviewToSprite_Cam");
        camGo.hideFlags = HideFlags.HideAndDontSave;
        camGo.transform.position = _previewRoot.transform.position + _cameraOffset;
        _previewCamera = camGo.AddComponent<Camera>();
        _previewCamera.orthographic = false;
        _previewCamera.fieldOfView = 35f;
        _previewCamera.nearClipPlane = 0.1f;
        _previewCamera.farClipPlane = 500f;
        _previewCamera.clearFlags = CameraClearFlags.SolidColor;
        _previewCamera.backgroundColor = _transparentBackground ? new Color(0, 0, 0, 0) : new Color(0.22f, 0.22f, 0.22f, 1f);
        _previewCamera.enabled = false;
        _previewCamera.cullingMask = 1 << previewLayer;

        _previewRT = new RenderTexture(PreviewSize, PreviewSize, 24, RenderTextureFormat.ARGB32);
        _previewRT.Create();
        _previewRT.filterMode = FilterMode.Bilinear;
    }

    void CleanupPreview()
    {
        if (_previewCamera != null && _previewCamera.gameObject != null)
            DestroyImmediate(_previewCamera.gameObject);
        _previewCamera = null;
        if (_previewRoot != null)
            DestroyImmediate(_previewRoot);
        _previewRoot = null;
        _previewInstance = null;
        if (_previewRT != null)
        {
            _previewRT.Release();
            DestroyImmediate(_previewRT);
        }
        _previewRT = null;
        _previewTarget = null;
    }

    void RenderPreview()
    {
        if (_previewRoot == null || _previewCamera == null || _previewRT == null) return;
        if (_previewInstance != null)
        {
            _previewInstance.transform.localPosition = _targetLocalOffset;
            _previewInstance.transform.localRotation = Quaternion.Euler(_targetLocalEuler);
        }
        Bounds bounds = ComputeBounds(_previewRoot);
        Vector3 center = _previewRoot.transform.position;
        _previewCamera.transform.position = center + _cameraOffset;
        _previewCamera.transform.LookAt(center + Vector3.up * (bounds.size.y * 0.2f));
        _previewCamera.backgroundColor = _transparentBackground ? new Color(0, 0, 0, 0) : new Color(0.22f, 0.22f, 0.22f, 1f);
        _previewCamera.targetTexture = _previewRT;
        _previewCamera.Render();
        _previewCamera.targetTexture = null;
    }

    string GetDefaultOutputName()
    {
        if (_targetObject == null) return "";
        string name = _targetObject.name;
        if (name.EndsWith("(Clone)")) name = name.Replace("(Clone)", "").Trim();
        return name + "_Sprite";
    }

    void OnGUI()
    {
        EditorGUILayout.Space(6);
        EditorGUILayout.HelpBox(
            "프리팹/씬 오브젝트를 렌더해 Resources/UI에 PNG 스프라이트로 저장합니다. \"투명 배경\"을 켜면 코너 색상(검은/회색 배경)을 제거해 저장하므로, RawImage에서 오브젝트만 보입니다.",
            MessageType.Info);

        EditorGUILayout.Space(4);
        _targetObject = (GameObject)EditorGUILayout.ObjectField("대상 오브젝트", _targetObject, typeof(GameObject), true);
        if (_targetObject != null && string.IsNullOrEmpty(_outputFileName))
            _outputFileName = GetDefaultOutputName();
        _outputFileName = EditorGUILayout.TextField("출력 파일명 (확장자 제외)", _outputFileName ?? "");

        EditorGUILayout.Space(2);
        _textureSize = EditorGUILayout.IntPopup("해상도", _textureSize, new[] { "128", "256", "512", "1024", "2048" }, new[] { 128, 256, 512, 1024, 2048 });
        EditorGUI.BeginChangeCheck();
        _transparentBackground = EditorGUILayout.Toggle("투명 배경", _transparentBackground);
        if (EditorGUI.EndChangeCheck()) Repaint();
        EditorGUI.BeginChangeCheck();
        _targetLocalOffset = EditorGUILayout.Vector3Field("대상 위치 오프셋 (카메라 기준)", _targetLocalOffset);
        _targetLocalEuler = EditorGUILayout.Vector3Field("대상 회전 (Euler, 도 단위)", _targetLocalEuler);
        if (EditorGUI.EndChangeCheck()) Repaint();

        EditorGUILayout.Space(4);
        bool canPreview = _targetObject != null && IsPrefabAsset(_targetObject);
        if (canPreview)
        {
            EnsurePreview();
            RenderPreview();
            if (_previewRT != null)
            {
                Rect previewRect = GUILayoutUtility.GetRect(PreviewSize, PreviewSize);
                GUI.Box(new Rect(previewRect.x - 1, previewRect.y - 1, previewRect.width + 2, previewRect.height + 2), "");
                GUI.DrawTexture(previewRect, _previewRT, ScaleMode.ScaleToFit, false);
                GUILayout.Label("미리보기 (오프셋 변경 시 실시간 갱신)", EditorStyles.miniLabel);
            }
        }
        else if (_targetObject != null)
            EditorGUILayout.HelpBox("미리보기는 프로젝트 창에서 프리팹 에셋을 선택했을 때만 표시됩니다.", MessageType.None);

        EditorGUILayout.Space(8);
        GUI.enabled = _targetObject != null && !string.IsNullOrWhiteSpace(_outputFileName);
        if (GUILayout.Button("렌더 후 PNG 스프라이트 저장", GUILayout.Height(32)))
            RenderAndSave();
        GUI.enabled = true;

        EditorGUILayout.Space(4);
        if (GUILayout.Button("선택 오브젝트로 설정"))
        {
            if (Selection.activeGameObject != null)
            {
                _targetObject = Selection.activeGameObject;
                _outputFileName = GetDefaultOutputName();
            }
        }
    }

    void RenderAndSave()
    {
        if (_targetObject == null)
        {
            Debug.LogError("대상 오브젝트가 없습니다.");
            return;
        }

        string outputDirFull = Path.Combine(Application.dataPath, "Resources", "UI");
        if (!Directory.Exists(outputDirFull))
            Directory.CreateDirectory(outputDirFull);

        string assetPath = Path.Combine(DefaultOutputDir, _outputFileName.Trim() + ".png");
        assetPath = AssetDatabase.GenerateUniqueAssetPath(assetPath);
        string fullPath = Path.Combine(Application.dataPath, assetPath.Replace("Assets/", ""));

        GameObject root = null;
        GameObject instance = null;
        Camera cam = null;
        RenderTexture rt = null;
        try
        {
            // 캡처용 루트와 인스턴스를 생성해, 에디터 씬/원본 오브젝트를 건드리지 않고 렌더합니다.
            root = new GameObject("PreviewCaptureRoot");
            root.hideFlags = HideFlags.HideAndDontSave;
            root.transform.position = Vector3.zero;

            if (IsPrefabAsset(_targetObject))
                instance = (GameObject)PrefabUtility.InstantiatePrefab(_targetObject);
            else
                instance = Object.Instantiate(_targetObject);

            if (instance == null)
            {
                Debug.LogError("렌더용 인스턴스 생성 실패.");
                return;
            }

            instance.transform.SetParent(root.transform, false);
            instance.transform.localPosition = _targetLocalOffset;
            instance.transform.localRotation = Quaternion.Euler(_targetLocalEuler);

            int previewLayer = LayerMask.NameToLayer("Ignore Raycast");
            if (previewLayer < 0) previewLayer = 0;
            SetLayerRecursively(root, previewLayer);

            GameObject camGo = new GameObject("PreviewCaptureCamera");
            camGo.hideFlags = HideFlags.HideAndDontSave;
            cam = camGo.AddComponent<Camera>();
            cam.orthographic = false;
            cam.fieldOfView = 35f;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 500f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = _transparentBackground ? new Color(0, 0, 0, 0) : new Color(0.22f, 0.22f, 0.22f, 1f);
            cam.enabled = false;
            cam.cullingMask = 1 << previewLayer;

            Bounds captureBounds = ComputeBounds(root);
            Vector3 captureCenter = root.transform.position;
            cam.transform.position = captureCenter + _cameraOffset;
            cam.transform.LookAt(captureCenter + Vector3.up * (captureBounds.size.y * 0.2f));

            rt = RenderTexture.GetTemporary(_textureSize, _textureSize, 24, RenderTextureFormat.ARGB32);
            RenderTexture prev = RenderTexture.active;
            cam.targetTexture = rt;
            cam.Render();
            RenderTexture.active = rt;

            Texture2D tex = new Texture2D(_textureSize, _textureSize, TextureFormat.RGBA32, false);
            tex.ReadPixels(new Rect(0, 0, _textureSize, _textureSize), 0, 0);
            // 카메라 배경 알파를 0으로 렌더하고, 오브젝트는 머티리얼에서 알파 1로 그려지므로
            // 별도의 코너 기반 배경 제거를 하지 않고 그대로 사용한다.
            tex.Apply();

            byte[] png = tex.EncodeToPNG();
            File.WriteAllBytes(fullPath, png);

            DestroyImmediate(tex);
            RenderTexture.active = prev;
            cam.targetTexture = null;

            AssetDatabase.Refresh();

            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePivot = new Vector2(0.5f, 0.5f);
                importer.alphaIsTransparency = true;
                importer.SaveAndReimport();
            }

            Debug.Log($"저장됨: {assetPath}");
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<Object>(assetPath);
        }
        finally
        {
            if (rt != null) RenderTexture.ReleaseTemporary(rt);
            if (cam != null && cam.gameObject != null) DestroyImmediate(cam.gameObject);
            if (root != null) DestroyImmediate(root);
        }
    }

    /// <summary>
    /// 코너 색상을 배경으로 간주해, 그 색에 가까운 픽셀의 알파를 0으로 만들어 투명하게 함.
    /// 렌더 파이프라인에서 검은 배경이 나와도 PNG는 투명 배경으로 저장됨.
    /// </summary>
    static void MakeBackgroundTransparent(Texture2D tex)
    {
        int w = tex.width;
        int h = tex.height;
        Color[] pixels = tex.GetPixels();
        if (pixels.Length == 0) return;

        float r = 0f, g = 0f, b = 0f;
        int n = 0;
        int margin = Mathf.Max(1, Mathf.Min(w, h) / 32);
        for (int y = 0; y < margin; y++)
        for (int x = 0; x < margin; x++)
        {
            int i = y * w + x;
            r += pixels[i].r; g += pixels[i].g; b += pixels[i].b;
            n++;
        }
        for (int y = 0; y < margin; y++)
        for (int x = w - margin; x < w; x++)
        {
            int i = y * w + x;
            r += pixels[i].r; g += pixels[i].g; b += pixels[i].b;
            n++;
        }
        for (int y = h - margin; y < h; y++)
        for (int x = 0; x < margin; x++)
        {
            int i = y * w + x;
            r += pixels[i].r; g += pixels[i].g; b += pixels[i].b;
            n++;
        }
        for (int y = h - margin; y < h; y++)
        for (int x = w - margin; x < w; x++)
        {
            int i = y * w + x;
            r += pixels[i].r; g += pixels[i].g; b += pixels[i].b;
            n++;
        }
        if (n == 0) return;
        r /= n; g /= n; b /= n;
        float thresholdSq = 0.04f;
        for (int i = 0; i < pixels.Length; i++)
        {
            Color c = pixels[i];
            float dr = c.r - r, dg = c.g - g, db = c.b - b;
            if (dr * dr + dg * dg + db * db <= thresholdSq)
                pixels[i] = new Color(c.r, c.g, c.b, 0f);
        }
        tex.SetPixels(pixels);
    }

    static void SetLayerRecursively(GameObject go, int layer)
    {
        if (go == null) return;
        go.layer = layer;
        for (int i = 0; i < go.transform.childCount; i++)
            SetLayerRecursively(go.transform.GetChild(i).gameObject, layer);
    }

    static Bounds ComputeBounds(GameObject go)
    {
        var renderers = go.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0) return new Bounds(go.transform.position, Vector3.one * 2f);
        Bounds b = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            b.Encapsulate(renderers[i].bounds);
        return b;
    }
}

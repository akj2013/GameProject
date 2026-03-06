using UnityEngine;
using UnityEditor;
using System.IO;

public static class HitEffectCreator
{
    const string k_OutputFolder = "Assets/Generated";
    const string k_PrefabPath = k_OutputFolder + "/HitEffect.prefab";
    const string k_AudioPath = k_OutputFolder + "/Hit_SFX.asset";

    [MenuItem("Tools/Generate Hit Effect Prefab")]
    public static void GenerateHitEffect()
    {
        if (!Directory.Exists(k_OutputFolder))
            Directory.CreateDirectory(k_OutputFolder);

        // 1) Create GameObject with ParticleSystem
        GameObject go = new GameObject("HitEffect");
        var ps = go.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.loop = false;
        main.playOnAwake = false;
        main.startLifetime = 0.5f;
        main.startSpeed = 2.0f;
        main.startSize = 0.22f;
        main.startColor = Color.white;
        main.gravityModifier = 0.2f;
        main.maxParticles = 64;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        // Emission: single burst
        var emission = ps.emission;
        emission.rateOverTime = 0;
        emission.SetBurst(0, new ParticleSystem.Burst(0f, 8, 12, 1, 0f));

        // Shape: cone
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 25f;
        shape.radius = 0.05f;
        shape.position = Vector3.zero;

        // Velocity over lifetime: slight spread
        var vel = ps.velocityOverLifetime;
        vel.enabled = true;
        vel.space = ParticleSystemSimulationSpace.Local;
        vel.x = new ParticleSystem.MinMaxCurve(-1.0f, 1.0f);
        vel.y = new ParticleSystem.MinMaxCurve(0.2f, 1.6f);
        vel.z = new ParticleSystem.MinMaxCurve(-1.0f, 1.0f);

        // Color over lifetime: yellow -> orange -> transparent
        var col = ps.colorOverLifetime;
        col.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(new Color(1f, 0.9f, 0.5f), 0f),
                new GradientColorKey(new Color(1f, 0.6f, 0.2f), 0.5f),
                new GradientColorKey(new Color(1f, 0.2f, 0.05f), 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(0.6f, 0.6f),
                new GradientAlphaKey(0f, 1f)
            });
        col.color = new ParticleSystem.MinMaxGradient(grad);

        // Size over lifetime: slightly shrink
        var size = ps.sizeOverLifetime;
        size.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f, 0.25f));
        size.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

        // Renderer: use default particle material if possible
        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;

        // 2) Create simple procedural AudioClip (short impact noise)
        int sampleRate = 44100;
        float lengthSec = 0.14f;
        int samples = Mathf.CeilToInt(sampleRate * lengthSec);
        AudioClip clip = AudioClip.Create("Hit_SFX", samples, 1, sampleRate, false);

        float[] data = new float[samples];
        // white-noise burst with exponential decay
        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / samples;
            float env = Mathf.Exp(-t * 6.0f); // decay
            float noise = (Random.value * 2f - 1f) * 0.6f;
            float tone = Mathf.Sin(2f * Mathf.PI * 700f * t) * 0.12f;
            data[i] = (noise + tone) * env;
        }
        clip.SetData(data, 0);

        // Save AudioClip as asset
        AssetDatabase.CreateAsset(clip, k_AudioPath);

        // 3) Add AudioSource to prefab root
        var audio = go.AddComponent<AudioSource>();
        audio.playOnAwake = false;
        audio.clip = clip;
        audio.spatialBlend = 0f; // 테스트용: 2D로 설정해서 거리 영향 제거
        audio.minDistance = 1f;
        audio.maxDistance = 12f;
        audio.rolloffMode = AudioRolloffMode.Linear;
        audio.volume = 1f;

        // 4) Save as prefab
        PrefabUtility.SaveAsPrefabAssetAndConnect(go, k_PrefabPath, InteractionMode.UserAction);

        // Cleanup temporary scene GameObject
        Object.DestroyImmediate(go);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("Hit Effect Generated",
            $"Created:\n- Prefab: {k_PrefabPath}\n- Audio: {k_AudioPath}\n\nAssign prefab to TreeHealth.hitEffectPrefab and audio to TreeHealth.hitSfx",
            "OK");
    }
}
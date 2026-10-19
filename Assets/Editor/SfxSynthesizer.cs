using System;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Writes original placeholder SFX (procedurally synthesised, no third-party audio) to
/// Assets/Resources/Audio/SFX so GameAudio can load them by id. Re-run to regenerate;
/// replace any .wav with a real recording of the same name to swap it in.
/// </summary>
public static class SfxSynthesizer
{
    const string OutDir = "Assets/Resources/Audio/SFX";
    const int Rate = 22050;

    [MenuItem("Tools/Gameplay Prototype/Generate Placeholder SFX")]
    public static void GenerateAll()
    {
        Directory.CreateDirectory(OutDir);
        var rng = new System.Random(1337); // deterministic output between runs

        // --- Hero sword -------------------------------------------------------------
        Write("sfx_slash_swing", 0.14f, (t, d) =>
            Noise(rng) * Env(t, 0.01f, d) * Sweep(t, d, 0.35f, 0.9f) * 0.55f, lowpass: 0.55f);
        Write("sfx_slash_hit", 0.12f, (t, d) =>
            (Noise(rng) * Decay(t, 40f) * 0.7f + Sine(t, Lerp(160f, 70f, t / d)) * Decay(t, 22f)) * 0.8f, lowpass: 0.45f);
        Write("sfx_slash_finisher", 0.22f, (t, d) =>
            (Noise(rng) * Decay(t, 18f) * 0.8f + Sine(t, Lerp(120f, 45f, t / d)) * Decay(t, 10f)
             + Square(t, Lerp(420f, 220f, t / d)) * Decay(t, 30f) * 0.15f) * 0.85f, lowpass: 0.4f);

        // --- Skills (hotbar 2, 3, 4, V, C) ------------------------------------------
        Write("sfx_skill_flame", 0.38f, (t, d) =>
            (Noise(rng) * Env(t, 0.02f, d) * 0.6f + Saw(t, 70f + 20f * Mathf.Sin(t * 40f)) * Decay(t, 6f) * 0.25f) * 0.8f, lowpass: 0.3f);
        Write("sfx_skill_dash", 0.22f, (t, d) =>
            Noise(rng) * Env(t, 0.03f, d) * Sweep(t, d, 0.2f, 1f) * 0.7f, lowpass: 0.65f);
        Write("sfx_skill_whirl", 0.36f, (t, d) =>
            Noise(rng) * Env(t, 0.02f, d) * (0.55f + 0.45f * Mathf.Sin(t * 2f * Mathf.PI * 9f)) * 0.6f, lowpass: 0.5f);
        Write("sfx_skill_guard", 0.32f, (t, d) =>
            (Sine(t, 880f) + Sine(t, 1320f) * 0.6f + Sine(t, 1760f) * 0.3f) * Env(t, 0.005f, d) * 0.3f);
        Write("sfx_skill_crescent", 0.45f, (t, d) =>
        {
            float charge = t < 0.12f ? Square(t, Lerp(300f, 900f, t / 0.12f)) * 0.18f : 0f;
            float swoosh = t >= 0.12f ? Noise(rng) * Env(t - 0.12f, 0.01f, d - 0.12f) * 0.75f : 0f;
            float boom = t >= 0.12f ? Sine(t, Lerp(110f, 40f, (t - 0.12f) / (d - 0.12f))) * Decay(t - 0.12f, 9f) * 0.7f : 0f;
            return charge + swoosh + boom;
        }, lowpass: 0.5f);

        // --- Enemies ---------------------------------------------------------------
        Write("sfx_enemy_hurt", 0.12f, (t, d) =>
            Square(t, Lerp(620f, 380f, t / d)) * Env(t, 0.004f, d) * 0.28f, lowpass: 0.6f);
        Write("sfx_enemy_death", 0.34f, (t, d) =>
            (Square(t, Lerp(700f, 160f, t / d)) * Env(t, 0.005f, d) * 0.25f
             + Noise(rng) * (t > 0.2f ? Decay(t - 0.2f, 25f) : 0f) * 0.5f), lowpass: 0.5f);
        Write("sfx_enemy_warn", 0.09f, (t, d) =>
            Square(t, t < 0.045f ? 980f : 1310f) * Env(t, 0.003f, d) * 0.2f);

        // --- Hero ------------------------------------------------------------------
        Write("sfx_hero_hurt", 0.16f, (t, d) =>
            (Square(t, Lerp(220f, 130f, t / d)) * 0.3f + Noise(rng) * 0.25f) * Env(t, 0.004f, d), lowpass: 0.45f);
        Write("sfx_hero_death", 0.8f, (t, d) =>
            Triangle(t, Lerp(440f, 110f, t / d)) * Env(t, 0.01f, d) * 0.45f);

        // --- UI / feedback -----------------------------------------------------------
        Write("sfx_ui_ready", 0.07f, (t, d) => Sine(t, 1568f) * Env(t, 0.002f, d) * 0.25f);
        Write("sfx_combo_milestone", 0.3f, (t, d) =>
        {
            float note = t < 0.1f ? 784f : (t < 0.2f ? 988f : 1175f);
            return Square(t, note) * Env(t % 0.1f, 0.003f, 0.1f) * 0.18f;
        });
        Write("sfx_gameover", 1.0f, (t, d) =>
        {
            float note = t < 0.3f ? 392f : (t < 0.6f ? 330f : 262f);
            float local = t < 0.3f ? t : (t < 0.6f ? t - 0.3f : t - 0.6f);
            float len = t < 0.6f ? 0.3f : 0.4f;
            return Triangle(t, note) * Env(local, 0.01f, len) * 0.4f;
        });

        // --- Ambience (loops) ---------------------------------------------------------
        Write("amb_forest_loop", 4f, (t, d) =>
        {
            float gust = 0.55f + 0.45f * Mathf.Sin(2f * Mathf.PI * t / d); // one gust per loop, seamless
            return Noise(rng) * 0.12f * gust;
        }, lowpass: 0.08f, loopFade: true);

        AssetDatabase.Refresh();
        foreach (var guid in AssetDatabase.FindAssets("t:AudioClip", new[] { OutDir }))
            ConfigureImporter(AssetDatabase.GUIDToAssetPath(guid));
        Debug.Log($"[SFX] Generated placeholder SFX in {OutDir}.");
    }

    static void ConfigureImporter(string path)
    {
        var importer = (AudioImporter)AssetImporter.GetAtPath(path);
        var s = importer.defaultSampleSettings;
        bool loop = Path.GetFileName(path).StartsWith("amb_");
        s.loadType = loop ? AudioClipLoadType.CompressedInMemory : AudioClipLoadType.DecompressOnLoad;
        s.compressionFormat = AudioCompressionFormat.Vorbis;
        s.quality = 0.7f;
        importer.defaultSampleSettings = s;
        importer.forceToMono = true;
        importer.loadInBackground = loop;
        importer.SaveAndReimport();
    }

    // ---- DSP helpers ---------------------------------------------------------------
    static float Lerp(float a, float b, float t) => a + (b - a) * Mathf.Clamp01(t);
    static float Noise(System.Random r) => (float)(r.NextDouble() * 2.0 - 1.0);
    static float Sine(float t, float f) => Mathf.Sin(2f * Mathf.PI * f * t);
    static float Square(float t, float f) => Mathf.Repeat(f * t, 1f) < 0.5f ? 1f : -1f;
    static float Saw(float t, float f) => Mathf.Repeat(f * t, 1f) * 2f - 1f;
    static float Triangle(float t, float f) => 1f - 4f * Mathf.Abs(Mathf.Repeat(f * t + 0.25f, 1f) - 0.5f);
    static float Decay(float t, float k) => Mathf.Exp(-k * Mathf.Max(0f, t));
    // Fast attack, smooth release to zero at the end of the clip (no clicks).
    static float Env(float t, float attack, float d)
    {
        if (t < attack) return t / attack;
        float r = Mathf.Clamp01((t - attack) / Mathf.Max(0.0001f, d - attack));
        return (1f - r) * (1f - r);
    }
    // Bell-shaped swell for whooshes: peaks at "peak" (0..1) of the clip.
    static float Sweep(float t, float d, float peak, float width)
    {
        float x = (t / d - peak) / (0.5f * width);
        return Mathf.Exp(-x * x * 2f);
    }

    static void Write(string name, float seconds, Func<float, float, float> sample, float lowpass = 1f, bool loopFade = false)
    {
        int n = Mathf.CeilToInt(seconds * Rate);
        var data = new float[n];
        float lp = 0f, peak = 0.0001f;
        for (int i = 0; i < n; i++)
        {
            float v = sample(i / (float)Rate, seconds);
            lp += (v - lp) * lowpass; // one-pole low-pass, 1 = bypass
            data[i] = lp;
            peak = Mathf.Max(peak, Mathf.Abs(lp));
        }
        if (loopFade)
        {
            // Crossfade the tail into the head so the loop point is seamless.
            int fade = Rate / 4;
            for (int i = 0; i < fade; i++)
            {
                float mix = i / (float)fade;
                data[i] = data[i] * mix + data[n - fade + i] * (1f - mix);
            }
            Array.Resize(ref data, n - fade);
        }
        float gain = Mathf.Min(1f, 0.9f / peak); // normalise loud clips, never boost quiet ones above their mix level
        if (peak < 0.3f) gain = 1f;
        using var fs = new FileStream($"{OutDir}/{name}.wav", FileMode.Create);
        using var w = new BinaryWriter(fs);
        int bytes = data.Length * 2;
        w.Write(new[] { 'R', 'I', 'F', 'F' }); w.Write(36 + bytes);
        w.Write(new[] { 'W', 'A', 'V', 'E' }); w.Write(new[] { 'f', 'm', 't', ' ' });
        w.Write(16); w.Write((short)1); w.Write((short)1); w.Write(Rate); w.Write(Rate * 2); w.Write((short)2); w.Write((short)16);
        w.Write(new[] { 'd', 'a', 't', 'a' }); w.Write(bytes);
        foreach (var s in data)
            w.Write((short)Mathf.Clamp(Mathf.RoundToInt(s * gain * 32767f), short.MinValue, short.MaxValue));
    }
}

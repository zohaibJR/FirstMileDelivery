using UnityEngine;

/// <summary>
/// One-shot 3D sound helper. Pass a null clip and nothing happens.
/// </summary>
public static class GameAudio
{
    public static AudioClip Or(AudioClip clip, AudioClip fallback) => clip != null ? clip : fallback;

    public static void Play(AudioClip clip, Vector3 position, float volume = 1f,
        float pitchVariation = 0f, float spatialBlend = 1f)
    {
        if (clip == null)
            return;

        GameObject go = new GameObject("SFX_" + clip.name);
        go.transform.position = position;

        AudioSource source = go.AddComponent<AudioSource>();
        source.clip = clip;
        source.volume = Mathf.Clamp01(volume);
        source.pitch = 1f + Random.Range(-pitchVariation, pitchVariation);
        source.spatialBlend = spatialBlend;
        source.Play();

        Object.Destroy(go, clip.length / Mathf.Max(0.1f, source.pitch) + 0.1f);
    }
}

/// <summary>
/// Generates simple placeholder sounds in code, so everything makes noise with zero audio assets.
/// Assign real clips in the Inspector later to replace them.
/// </summary>
public static class ProceduralSfx
{
    private const int SampleRate = 44100;

    private delegate float Sampler(float t, float n, float noise);

    private static AudioClip thud, click, step, pickup, whoosh, unlock, denied;

    public static AudioClip Thud
    {
        get
        {
            if (thud == null)
                thud = Build("sfx_thud", 0.25f, 0.18f, (t, n, noise) =>
                {
                    float body = Mathf.Sin(2f * Mathf.PI * (60f + 50f * (1f - n)) * t) * Mathf.Pow(1f - n, 2f);
                    return body * 0.9f + noise * Mathf.Pow(1f - n, 3f) * 0.6f;
                });
            return thud;
        }
    }

    public static AudioClip Click
    {
        get
        {
            if (click == null)
                click = Build("sfx_click", 0.05f, 0.5f, (t, n, noise) => noise * Mathf.Pow(1f - n, 4f) * 0.7f);
            return click;
        }
    }

    public static AudioClip Step
    {
        get
        {
            if (step == null)
                step = Build("sfx_step", 0.14f, 0.12f, (t, n, noise) =>
                {
                    float thump = Mathf.Sin(2f * Mathf.PI * 55f * t) * Mathf.Pow(1f - n, 3f) * 0.4f;
                    return noise * Mathf.Pow(1f - n, 3f) * 0.7f + thump;
                });
            return step;
        }
    }

    public static AudioClip Pickup
    {
        get
        {
            if (pickup == null)
                pickup = Build("sfx_pickup", 0.12f, 0.5f, (t, n, noise) =>
                    Mathf.Sin(2f * Mathf.PI * (500f + 700f * n) * t) * (1f - n) * 0.4f);
            return pickup;
        }
    }

    public static AudioClip Whoosh
    {
        get
        {
            if (whoosh == null)
                whoosh = Build("sfx_whoosh", 0.3f, 0.08f, (t, n, noise) =>
                    noise * Mathf.Sin(Mathf.PI * n) * 0.6f);
            return whoosh;
        }
    }

    public static AudioClip Unlock
    {
        get
        {
            if (unlock == null)
                unlock = Build("sfx_unlock", 0.25f, 0.5f, (t, n, noise) =>
                    Mathf.Sin(2f * Mathf.PI * (n < 0.5f ? 660f : 880f) * t) * (1f - n) * 0.4f);
            return unlock;
        }
    }

    public static AudioClip Denied
    {
        get
        {
            if (denied == null)
                denied = Build("sfx_denied", 0.2f, 0.5f, (t, n, noise) =>
                    Mathf.Sign(Mathf.Sin(2f * Mathf.PI * 110f * t)) * (1f - n) * 0.25f);
            return denied;
        }
    }

    private static AudioClip Build(string name, float duration, float noiseSmoothing, Sampler sampler)
    {
        int count = Mathf.CeilToInt(duration * SampleRate);
        float[] data = new float[count];
        System.Random rng = new System.Random(name.GetHashCode());

        // Low-pass filtered noise has a small amplitude, so scale it back up.
        float norm = 1f / (0.577f * Mathf.Sqrt(noiseSmoothing / (2f - noiseSmoothing)));
        float lowPassed = 0f;

        for (int i = 0; i < count; i++)
        {
            float t = i / (float)SampleRate;
            float n = i / (float)count;
            float raw = (float)(rng.NextDouble() * 2.0 - 1.0);
            lowPassed += (raw - lowPassed) * noiseSmoothing;
            data[i] = Mathf.Clamp(sampler(t, n, lowPassed * norm * 0.5f), -1f, 1f);
        }

        AudioClip clip = AudioClip.Create(name, count, 1, SampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }
}
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Makes an object softly glow while the player looks at it.
/// PlayerInteractor adds this automatically, so you never need to add it by hand.
/// Works with materials that have an emission property (Standard and URP/HDRP Lit).
/// </summary>
public sealed class InteractableHighlight : MonoBehaviour
{
    [SerializeField] private Color color = new Color(1f, 0.82f, 0.35f);
    [SerializeField, Min(0f)] private float intensity = 0.55f;
    [SerializeField, Min(0f)] private float pulseSpeed = 4f;
    [SerializeField, Range(0f, 1f)] private float pulseDepth = 0.35f;

    private struct Entry
    {
        public Material material;
        public bool hadKeyword;
        public Color originalEmission;
    }

    private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

    private readonly List<Entry> entries = new List<Entry>();
    private bool prepared;
    private bool highlighted;

    private void Awake()
    {
        enabled = false;
    }

    public void SetHighlighted(bool on)
    {
        if (on == highlighted)
            return;

        if (on)
        {
            Prepare();
            if (entries.Count == 0)
                return;
        }

        highlighted = on;
        enabled = on;

        if (!on)
            Restore();
    }

    private void Prepare()
    {
        if (prepared)
            return;
        prepared = true;

        foreach (Renderer r in GetComponentsInChildren<Renderer>())
        {
            if (r is ParticleSystemRenderer || r is TrailRenderer || r is LineRenderer)
                continue;

            // Accessing .materials creates per-renderer instances, so shared assets stay untouched.
            Material[] mats = r.materials;
            foreach (Material m in mats)
            {
                if (m == null || !m.HasProperty(EmissionColorId))
                    continue;

                entries.Add(new Entry
                {
                    material = m,
                    hadKeyword = m.IsKeywordEnabled("_EMISSION"),
                    originalEmission = m.GetColor(EmissionColorId)
                });
            }
        }
    }

    private void Update()
    {
        float pulse = 1f - pulseDepth + pulseDepth * (0.5f + 0.5f * Mathf.Sin(Time.time * pulseSpeed));
        Color glow = new Color(color.r, color.g, color.b, 0f) * (intensity * pulse);

        foreach (Entry e in entries)
        {
            if (e.material == null)
                continue;

            e.material.EnableKeyword("_EMISSION");
            e.material.SetColor(EmissionColorId, e.originalEmission + glow);
        }
    }

    private void Restore()
    {
        foreach (Entry e in entries)
        {
            if (e.material == null)
                continue;

            e.material.SetColor(EmissionColorId, e.originalEmission);
            if (!e.hadKeyword)
                e.material.DisableKeyword("_EMISSION");
        }
    }

    private void OnDestroy()
    {
        foreach (Entry e in entries)
        {
            if (e.material != null)
                Destroy(e.material);
        }
        entries.Clear();
    }
}
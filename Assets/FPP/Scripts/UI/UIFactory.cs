using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Builds UI from code, so the HUD and inventory need no prefab or Canvas setup.
/// </summary>
public static class UIFactory
{
    private static Sprite ringSprite, dotSprite, vignetteSprite;

    /// <summary>Font used for all generated text. If null, the TextMeshPro default font is used.</summary>
    public static TMP_FontAsset DefaultFont { get; set; }

    public static Sprite RingSprite
    {
        get
        {
            if (ringSprite == null)
                ringSprite = MakeSprite(128, r => Mathf.Clamp01((r - 0.80f) / 0.05f) * Mathf.Clamp01((1f - r) / 0.05f));
            return ringSprite;
        }
    }

    public static Sprite DotSprite
    {
        get
        {
            if (dotSprite == null)
                dotSprite = MakeSprite(64, r => Mathf.Clamp01((1f - r) / 0.15f));
            return dotSprite;
        }
    }

    /// <summary>Clear in the middle, dark at the edges. Tint with Image.color.</summary>
    public static Sprite VignetteSprite
    {
        get
        {
            if (vignetteSprite == null)
                vignetteSprite = MakeSprite(256, r => Mathf.Pow(Smooth(0.35f, 1.2f, r), 1.3f) * 0.85f);
            return vignetteSprite;
        }
    }

    public static Canvas CreateCanvas(string name, int sortingOrder, Transform parent)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);

        Canvas canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortingOrder;

        CanvasScaler scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        return canvas;
    }

    public static RectTransform Rect(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        return rt;
    }

    public static void Place(RectTransform rt, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
    {
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = pivot;
        rt.anchoredPosition = position;
        rt.sizeDelta = size;
    }

    public static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    public static Image CreateImage(string name, Transform parent, Color color, Sprite sprite = null)
    {
        RectTransform rt = Rect(name, parent);
        Image image = rt.gameObject.AddComponent<Image>();
        image.sprite = sprite;
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    public static TextMeshProUGUI CreateText(string name, Transform parent, float fontSize,
        TextAlignmentOptions alignment, Color color, bool wrap = false)
    {
        RectTransform rt = Rect(name, parent);
        TextMeshProUGUI text = rt.gameObject.AddComponent<TextMeshProUGUI>();
        if (DefaultFont != null)
            text.font = DefaultFont;

        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = color;
        text.raycastTarget = false;
        text.richText = true;
        text.overflowMode = TextOverflowModes.Overflow;
        text.outlineWidth = 0.18f;
        text.outlineColor = new Color32(0, 0, 0, 255);

        // Obsolete in newer TextMeshPro versions but still works, and compiles on older ones.
#pragma warning disable CS0618
        text.enableWordWrapping = wrap;
#pragma warning restore CS0618

        return text;
    }

    private static Sprite MakeSprite(int size, Func<float, float> alphaFromRadius)
    {
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear
        };

        Color32[] pixels = new Color32[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = (x + 0.5f) / size * 2f - 1f;
                float dy = (y + 0.5f) / size * 2f - 1f;
                float r = Mathf.Sqrt(dx * dx + dy * dy);
                byte a = (byte)(Mathf.Clamp01(alphaFromRadius(r)) * 255f);
                pixels[y * size + x] = new Color32(255, 255, 255, a);
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply();

        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }

    private static float Smooth(float a, float b, float x)
    {
        float t = Mathf.Clamp01((x - a) / (b - a));
        return t * t * (3f - 2f * t);
    }
}
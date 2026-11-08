using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The on-screen HUD (built from code, no Canvas setup needed). Put this on the FPP Player.
/// Shows: crosshair, interaction prompt with the right button for keyboard or gamepad,
/// hold-to-interact ring, carry hints, toast messages, and the inspect screen
/// (dimmed edges plus a name and description panel).
/// Requires TextMeshPro (Window > TextMeshPro > Import TMP Essential Resources, if asked).
/// </summary>
public sealed class InteractionHud : MonoBehaviour
{
    [Header("References (auto-found if empty)")]
    [SerializeField] private PlayerInteractor interactor;
    [SerializeField] private ItemCarrier carrier;

    [Header("Font")]
    [Tooltip("TMP Font Asset created from your .ttf. Empty = TextMeshPro default font.")]
    [SerializeField] private TMP_FontAsset font;

    [Header("Look")]
    [SerializeField] private Color crosshairColor = new Color(1f, 1f, 1f, 0.8f);
    [SerializeField] private Color targetColor = new Color(1f, 0.84f, 0.3f, 1f);
    [SerializeField, Min(0.5f)] private float toastDuration = 2.5f;

    private Image crosshair, ringBack, ringFill;
    private TextMeshProUGUI prompt, hints, toast, inspectTitle, inspectBody, inspectFooter;
    private CanvasGroup inspectGroup;
    private PickupItem shownItem;
    private float toastTimer;

    private void Awake()
    {
        if (interactor == null) interactor = GetComponent<PlayerInteractor>();
        if (carrier == null) carrier = GetComponent<ItemCarrier>();
        if (font != null)
            UIFactory.DefaultFont = font;

        BuildUi();
    }

    private void OnEnable()
    {
        Notifier.Message += OnMessage;
    }

    private void OnDisable()
    {
        Notifier.Message -= OnMessage;
    }

    private void BuildUi()
    {
        Canvas canvas = UIFactory.CreateCanvas("Interaction HUD", 10, transform);
        Transform root = canvas.transform;
        Vector2 center = new Vector2(0.5f, 0.5f);

        // Crosshair and hold ring
        crosshair = UIFactory.CreateImage("Crosshair", root, crosshairColor, UIFactory.DotSprite);
        UIFactory.Place(crosshair.rectTransform, center, center, Vector2.zero, new Vector2(6f, 6f));

        ringBack = UIFactory.CreateImage("RingBack", root, new Color(1f, 1f, 1f, 0.25f), UIFactory.RingSprite);
        UIFactory.Place(ringBack.rectTransform, center, center, Vector2.zero, new Vector2(72f, 72f));

        ringFill = UIFactory.CreateImage("RingFill", root, targetColor, UIFactory.RingSprite);
        UIFactory.Place(ringFill.rectTransform, center, center, Vector2.zero, new Vector2(72f, 72f));
        ringFill.type = Image.Type.Filled;
        ringFill.fillMethod = Image.FillMethod.Radial360;
        ringFill.fillOrigin = (int)Image.Origin360.Top;
        ringFill.fillClockwise = true;
        ringFill.fillAmount = 0f;

        // Interaction prompt
        prompt = UIFactory.CreateText("Prompt", root, 30f, TextAlignmentOptions.Top, Color.white);
        UIFactory.Place(prompt.rectTransform, center, new Vector2(0.5f, 1f), new Vector2(0f, -62f), new Vector2(1000f, 60f));

        // Carry hints (bottom left)
        hints = UIFactory.CreateText("Hints", root, 26f, TextAlignmentOptions.BottomLeft, Color.white);
        UIFactory.Place(hints.rectTransform, Vector2.zero, Vector2.zero, new Vector2(40f, 40f), new Vector2(1100f, 130f));

        // Toast (top center)
        toast = UIFactory.CreateText("Toast", root, 30f, TextAlignmentOptions.Top, Color.white);
        UIFactory.Place(toast.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -60f), new Vector2(1000f, 60f));

        // Inspect screen
        RectTransform groupRect = UIFactory.Rect("Inspect", root);
        UIFactory.Stretch(groupRect);
        inspectGroup = groupRect.gameObject.AddComponent<CanvasGroup>();
        inspectGroup.alpha = 0f;
        inspectGroup.interactable = false;
        inspectGroup.blocksRaycasts = false;

        Image vignette = UIFactory.CreateImage("Vignette", groupRect, Color.black, UIFactory.VignetteSprite);
        UIFactory.Stretch(vignette.rectTransform);

        Image panel = UIFactory.CreateImage("Panel", groupRect, new Color(0f, 0f, 0f, 0.5f));
        UIFactory.Place(panel.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-70f, 0f), new Vector2(520f, 440f));

        inspectTitle = UIFactory.CreateText("Title", panel.transform, 40f, TextAlignmentOptions.Top, Color.white, true);
        inspectTitle.fontStyle = FontStyles.Bold;
        UIFactory.Place(inspectTitle.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -24f), new Vector2(480f, 60f));

        inspectBody = UIFactory.CreateText("Body", panel.transform, 26f, TextAlignmentOptions.TopLeft, new Color(0.9f, 0.9f, 0.9f), true);
        UIFactory.Place(inspectBody.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -100f), new Vector2(460f, 320f));

        inspectFooter = UIFactory.CreateText("Footer", groupRect, 26f, TextAlignmentOptions.Bottom, Color.white);
        UIFactory.Place(inspectFooter.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 50f), new Vector2(1500f, 50f));

        SetRingVisible(false);
        prompt.text = string.Empty;
        hints.text = string.Empty;
        toast.text = string.Empty;
    }

    private void Update()
    {
        InputGlyphs.Poll();

        bool inspecting = carrier != null && carrier.IsInspecting;

        UpdateCrosshairAndPrompt(inspecting);
        UpdateHints(inspecting);
        UpdateInspect(inspecting);
        UpdateToast();
    }

    private void UpdateCrosshairAndPrompt(bool inspecting)
    {
        bool cursorLocked = Cursor.lockState == CursorLockMode.Locked;
        IInteractable target = interactor != null ? interactor.CurrentTarget : null;
        bool showTarget = target != null && cursorLocked && !inspecting;

        crosshair.enabled = cursorLocked && !inspecting;
        crosshair.color = showTarget ? targetColor : crosshairColor;
        crosshair.rectTransform.sizeDelta = Vector2.one * (showTarget ? 10f : 6f);

        if (showTarget)
        {
            string badge = InputGlyphs.Pick(interactor.InteractKey, "X");
            prompt.text = interactor.TargetIsHold
                ? $"Hold {badge}  {target.InteractionPrompt}"
                : $"{badge}  {target.InteractionPrompt}";
        }
        else
        {
            prompt.text = string.Empty;
        }

        bool showRing = showTarget && interactor.TargetIsHold;
        SetRingVisible(showRing);
        if (showRing)
            ringFill.fillAmount = interactor.HoldProgress;
    }

    private void UpdateHints(bool inspecting)
    {
        if (carrier == null || !carrier.IsHolding || inspecting)
        {
            hints.text = string.Empty;
            return;
        }

        string inspect = InputGlyphs.UsingGamepad
            ? InputGlyphs.Badge("Y")
            : $"{InputGlyphs.Badge(InputGlyphs.KeyName(carrier.InspectKey))}/{InputGlyphs.Badge("RMB")}";

        string text = $"<b>{carrier.HeldItem.DisplayName}</b>\n" +
                      $"{inspect} {(carrier.HoldToInspect ? "Hold to inspect" : "Inspect")}   " +
                      $"{InputGlyphs.Pick(carrier.DropKey, "B")} Drop";

        if (carrier.LeftMouseThrows)
            text += $"   {InputGlyphs.Pick("LMB", "RB")} Throw";

        if (carrier.CanStoreHeld)
            text += $"   {InputGlyphs.Pick(carrier.StoreKey, "LB")} Store";

        hints.text = text;
    }

    private void UpdateInspect(bool inspecting)
    {
        float targetAlpha = inspecting ? 1f : 0f;
        inspectGroup.alpha = Mathf.MoveTowards(inspectGroup.alpha, targetAlpha, Time.unscaledDeltaTime * 6f);

        if (!inspecting || carrier.HeldItem == null)
        {
            shownItem = null;
            return;
        }

        PickupItem item = carrier.HeldItem;
        if (item != shownItem)
        {
            shownItem = item;
            inspectTitle.text = item.DisplayName;

            string body = string.IsNullOrWhiteSpace(item.Description) ? string.Empty : item.Description;
            if (item.Weight > 0f)
                body += (body.Length > 0 ? "\n\n" : string.Empty) + $"<color=#9AA7B0>Weight: {item.Weight:0.##}</color>";
            inspectBody.text = body;
        }

        string rotate = InputGlyphs.UsingGamepad ? "Right stick: Rotate" : "Mouse: Rotate   Scroll: Zoom";
        string back = carrier.HoldToInspect
            ? "Release to go back"
            : $"{InputGlyphs.Pick(carrier.InspectKey, "Y")} Back";

        string footer = $"{rotate}   {back}   {InputGlyphs.Pick(carrier.DropKey, "B")} Drop";
        if (carrier.CanStoreHeld)
            footer += $"   {InputGlyphs.Pick(carrier.StoreKey, "LB")} Store";

        inspectFooter.text = footer;
    }

    private void UpdateToast()
    {
        if (toastTimer <= 0f)
        {
            if (toast.text.Length > 0)
                toast.text = string.Empty;
            return;
        }

        toastTimer -= Time.unscaledDeltaTime;
        toast.alpha = Mathf.Clamp01(toastTimer / 0.5f);
    }

    private void OnMessage(string message)
    {
        if (toast == null)
            return;

        toast.text = message;
        toast.alpha = 1f;
        toastTimer = toastDuration;
    }

    private void SetRingVisible(bool visible)
    {
        ringBack.enabled = visible;
        ringFill.enabled = visible;
    }
}
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The on-screen HUD for interactions. Put this on the FPP Player.
/// Shows: crosshair, interaction prompt with the right button for keyboard or gamepad,
/// hold-to-interact ring, carry hints, toast messages, and the inspect screen.
/// Supports drag-and-drop UI references from your Canvas.
/// </summary>
public sealed class InteractionHud : MonoBehaviour
{
    [Header("References (auto-found if empty)")]
    [SerializeField] private PlayerInteractor interactor;
    [SerializeField] private ItemCarrier carrier;

    [Header("UI References (Drag & Drop from Canvas)")]
    [Tooltip("Parent panel of the Interaction HUD (optional).")]
    [SerializeField] private GameObject panelRoot;
    [Tooltip("Image for the center crosshair dot.")]
    [SerializeField] private Image crosshair;
    [Tooltip("Image for the hold-to-interact background ring.")]
    [SerializeField] private Image ringBack;
    [Tooltip("Image for the hold-to-interact radial fill ring.")]
    [SerializeField] private Image ringFill;
    [Tooltip("TextMeshProUGUI for the interaction prompt (e.g. [E] Open).")]
    [SerializeField] private TextMeshProUGUI prompt;
    [Tooltip("TextMeshProUGUI for carry control hints (bottom left).")]
    [SerializeField] private TextMeshProUGUI hints;
    [Tooltip("TextMeshProUGUI for queued toast messages (top center).")]
    [SerializeField] private TextMeshProUGUI toast;

    [Header("Inspect Screen References (Optional)")]
    [Tooltip("CanvasGroup on the Inspect panel (faded in when inspecting).")]
    [SerializeField] private CanvasGroup inspectGroup;
    [Tooltip("TextMeshProUGUI for the inspected item's name.")]
    [SerializeField] private TextMeshProUGUI inspectTitle;
    [Tooltip("TextMeshProUGUI for the inspected item's description and weight.")]
    [SerializeField] private TextMeshProUGUI inspectBody;
    [Tooltip("TextMeshProUGUI for inspect controls hint.")]
    [SerializeField] private TextMeshProUGUI inspectFooter;

    [Header("Font")]
    [Tooltip("TMP Font Asset created from your .ttf. Empty = TextMeshPro default font.")]
    [SerializeField] private TMP_FontAsset font;

    [Header("Look")]
    [SerializeField] private Color crosshairColor = new Color(1f, 1f, 1f, 0.8f);
    [SerializeField] private Color targetColor = new Color(1f, 0.84f, 0.3f, 1f);
    [SerializeField, Min(0.5f)] private float toastDuration = 2.5f;
    [Tooltip("When several messages arrive together, each one is shown for at least this long.")]
    [SerializeField, Min(0.3f)] private float minToastTime = 1.2f;

    private PickupItem shownItem;
    private float toastTimer;
    private float toastShownFor;
    private readonly Queue<string> toastQueue = new Queue<string>();

    private void Awake()
    {
        if (interactor == null) interactor = GetComponent<PlayerInteractor>();
        if (carrier == null) carrier = GetComponent<ItemCarrier>();
        if (font != null)
            UIFactory.DefaultFont = font;

        if (crosshair == null || prompt == null)
        {
            // Fallback: build procedurally if references are not assigned in Inspector.
            BuildUi();
        }
        else
        {
            InitializeAssignedUi();
        }
    }

    private void OnEnable()
    {
        Notifier.Message += OnMessage;
    }

    private void OnDisable()
    {
        Notifier.Message -= OnMessage;
    }

    private void InitializeAssignedUi()
    {
        if (crosshair != null && crosshair.sprite == null)
            crosshair.sprite = UIFactory.DotSprite;

        if (ringBack != null && ringBack.sprite == null)
            ringBack.sprite = UIFactory.RingSprite;

        if (ringFill != null)
        {
            if (ringFill.sprite == null)
                ringFill.sprite = UIFactory.RingSprite;

            ringFill.type = Image.Type.Filled;
            ringFill.fillMethod = Image.FillMethod.Radial360;
            ringFill.fillOrigin = (int)Image.Origin360.Top;
            ringFill.fillClockwise = true;
            ringFill.fillAmount = 0f;
        }

        if (inspectGroup != null)
        {
            inspectGroup.alpha = 0f;
            inspectGroup.interactable = false;
            inspectGroup.blocksRaycasts = false;
        }

        SetRingVisible(false);
        if (prompt != null) prompt.text = string.Empty;
        if (hints != null) hints.text = string.Empty;
        if (toast != null) toast.text = string.Empty;
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

        if (crosshair != null)
        {
            crosshair.enabled = cursorLocked && !inspecting;
            crosshair.color = showTarget ? targetColor : crosshairColor;
            crosshair.rectTransform.sizeDelta = Vector2.one * (showTarget ? 10f : 6f);
        }

        if (prompt != null)
        {
            if (showTarget && interactor != null)
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
        }

        bool showRing = showTarget && interactor != null && interactor.TargetIsHold;
        SetRingVisible(showRing);
        if (showRing && ringFill != null && interactor != null)
            ringFill.fillAmount = interactor.HoldProgress;
    }

    private void UpdateHints(bool inspecting)
    {
        if (hints == null)
            return;

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

        hints.text = text;
    }

    private void UpdateInspect(bool inspecting)
    {
        if (inspectGroup == null)
            return;

        float targetAlpha = inspecting ? 1f : 0f;
        inspectGroup.alpha = Mathf.MoveTowards(inspectGroup.alpha, targetAlpha, Time.unscaledDeltaTime * 6f);

        if (!inspecting || carrier == null || carrier.HeldItem == null)
        {
            shownItem = null;
            return;
        }

        PickupItem item = carrier.HeldItem;
        if (item != shownItem)
        {
            shownItem = item;
            if (inspectTitle != null) inspectTitle.text = item.DisplayName;

            string body = string.IsNullOrWhiteSpace(item.Description) ? string.Empty : item.Description;
            if (item.Weight > 0f)
                body += (body.Length > 0 ? "\n\n" : string.Empty) + $"<color=#9AA7B0>Weight: {item.Weight:0.##}</color>";

            if (inspectBody != null) inspectBody.text = body;
        }

        string rotate = InputGlyphs.UsingGamepad ? "Right stick: Rotate" : "Mouse: Rotate   Scroll: Zoom";
        string back = carrier.HoldToInspect
            ? "Release to go back"
            : $"{InputGlyphs.Pick(carrier.InspectKey, "Y")} Back";

        string footer = $"{rotate}   {back}   {InputGlyphs.Pick(carrier.DropKey, "B")} Drop";

        if (inspectFooter != null) inspectFooter.text = footer;
    }

    private void UpdateToast()
    {
        if (toast == null)
            return;

        if (toastTimer <= 0f)
        {
            if (toastQueue.Count > 0)
                ShowToast(toastQueue.Dequeue());
            else if (toast.text.Length > 0)
                toast.text = string.Empty;
            return;
        }

        toastTimer -= Time.unscaledDeltaTime;
        toastShownFor += Time.unscaledDeltaTime;

        if (toastQueue.Count > 0 && toastShownFor >= minToastTime)
        {
            ShowToast(toastQueue.Dequeue());
            return;
        }

        toast.alpha = Mathf.Clamp01(toastTimer / 0.5f);
    }

    private void OnMessage(string message)
    {
        if (toast == null)
            return;

        if (toastTimer <= 0f)
        {
            ShowToast(message);
            return;
        }

        if (toastQueue.Count >= 4)
            toastQueue.Dequeue();

        toastQueue.Enqueue(message);
    }

    private void ShowToast(string message)
    {
        if (toast == null)
            return;

        toast.text = message;
        toast.alpha = 1f;
        toastTimer = toastDuration;
        toastShownFor = 0f;
    }

    private void SetRingVisible(bool visible)
    {
        if (ringBack != null) ringBack.enabled = visible;
        if (ringFill != null) ringFill.enabled = visible;
    }
}

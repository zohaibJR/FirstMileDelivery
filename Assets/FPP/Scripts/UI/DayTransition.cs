using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The "next morning" screen: fades to black, changes the day, shows "Day N" and a short message,
/// then fades back in. Put this on the FPP Player.
/// Supports drag-and-drop UI references from your Canvas.
/// </summary>
public sealed class DayTransition : MonoBehaviour
{
    public static DayTransition Instance { get; private set; }

    [Header("UI References (Drag & Drop from Canvas)")]
    [Tooltip("The parent Day Transition panel (has a CanvasGroup for fading).")]
    [SerializeField] private CanvasGroup screenGroup;
    [Tooltip("CanvasGroup on the text container (faded in while screen is black).")]
    [SerializeField] private CanvasGroup textGroup;
    [Tooltip("TextMeshProUGUI for the Day Title (e.g. Day 2).")]
    [SerializeField] private TextMeshProUGUI titleText;
    [Tooltip("TextMeshProUGUI for the reason/greeting subtitle (e.g. Good morning!).")]
    [SerializeField] private TextMeshProUGUI subtitleText;

    [Header("Timing (seconds)")]
    [SerializeField, Min(0.1f)] private float fadeOutTime = 1f;
    [SerializeField, Min(0.1f)] private float holdTime = 2f;
    [SerializeField, Min(0.1f)] private float fadeInTime = 1.2f;

    private PlayerInputLock inputLock;
    private bool playing;

    public bool IsPlaying => playing;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("DayTransition: more than one in the scene. Only one is allowed.", this);
            return;
        }

        Instance = this;
        inputLock = PlayerInputLock.Get(gameObject);
    }

    private void Start()
    {
        if (screenGroup == null || titleText == null)
        {
            // Fallback: build procedurally if references are not assigned in Inspector.
            BuildUi();
        }
        else
        {
            InitializeAssignedUi();
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void InitializeAssignedUi()
    {
        if (screenGroup != null)
        {
            screenGroup.alpha = 0f;
            screenGroup.interactable = false;
            screenGroup.blocksRaycasts = false;
        }

        if (textGroup != null)
        {
            textGroup.alpha = 0f;
            textGroup.interactable = false;
            textGroup.blocksRaycasts = false;
        }

        if (titleText != null) titleText.text = string.Empty;
        if (subtitleText != null) subtitleText.text = string.Empty;
    }

    /// <summary>Plays the transition. Called by TimeManager.RequestNextDay.</summary>
    public void Play(string reason)
    {
        if (playing)
            return;

        StartCoroutine(Run(reason));
    }

    private void BuildUi()
    {
        Vector2 center = new Vector2(0.5f, 0.5f);

        Canvas canvas = UIFactory.CreateCanvas("Day Transition", 40, transform);
        screenGroup = canvas.gameObject.AddComponent<CanvasGroup>();
        screenGroup.alpha = 0f;
        screenGroup.interactable = false;
        screenGroup.blocksRaycasts = false;

        Image black = UIFactory.CreateImage("Black", canvas.transform, Color.black);
        UIFactory.Stretch(black.rectTransform);

        RectTransform textRoot = UIFactory.Rect("Texts", canvas.transform);
        UIFactory.Stretch(textRoot);
        textGroup = textRoot.gameObject.AddComponent<CanvasGroup>();
        textGroup.alpha = 0f;

        titleText = UIFactory.CreateText("Title", textRoot, 96f, TextAlignmentOptions.Center, Color.white);
        titleText.fontStyle = FontStyles.Bold;
        UIFactory.Place(titleText.rectTransform, center, center, new Vector2(0f, 40f), new Vector2(1400f, 150f));

        subtitleText = UIFactory.CreateText("Subtitle", textRoot, 38f, TextAlignmentOptions.Center, new Color(1f, 1f, 1f, 0.85f));
        UIFactory.Place(subtitleText.rectTransform, center, center, new Vector2(0f, -70f), new Vector2(1400f, 70f));
    }

    private IEnumerator Run(string reason)
    {
        playing = true;

        TimeManager time = TimeManager.Instance;
        if (time != null)
            time.Paused = true;

        if (screenGroup != null)
            screenGroup.blocksRaycasts = true;

        // Refused while airborne; the fade still plays, the player just isn't frozen.
        bool locked = inputLock != null && inputLock.Lock(this);

        if (screenGroup != null)
            yield return Fade(screenGroup, 0f, 1f, fadeOutTime);
        else
            yield return new WaitForSecondsRealtime(fadeOutTime);

        // The screen is black now: change the day where nobody can see the world reset.
        if (time != null)
            time.CompleteNextDay();

        if (titleText != null)
            titleText.text = time != null ? $"Day {time.Day}" : string.Empty;

        if (subtitleText != null)
            subtitleText.text = string.IsNullOrEmpty(reason) ? string.Empty : reason;

        if (textGroup != null)
            yield return Fade(textGroup, 0f, 1f, 0.5f);

        yield return new WaitForSecondsRealtime(holdTime);

        if (textGroup != null)
            yield return Fade(textGroup, 1f, 0f, 0.4f);

        if (screenGroup != null)
            yield return Fade(screenGroup, 1f, 0f, fadeInTime);
        else
            yield return new WaitForSecondsRealtime(fadeInTime);

        if (screenGroup != null)
            screenGroup.blocksRaycasts = false;

        if (locked && inputLock != null)
            inputLock.Unlock(this);

        if (time != null)
            time.Paused = false;

        playing = false;
    }

    private static IEnumerator Fade(CanvasGroup group, float from, float to, float duration)
    {
        if (group == null)
            yield break;

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
            group.alpha = Mathf.Lerp(from, to, k);
            yield return null;
        }

        group.alpha = to;
    }
}

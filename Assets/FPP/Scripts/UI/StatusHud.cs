using TMPro;
using UnityEngine;

/// <summary>
/// Top-left status display. Put this on the FPP Player.
/// Shows: money (counts up smoothly), floating +$ / -$ payment notifications,
/// the current day, and how many deliveries are left.
/// Supports drag-and-drop UI references from your Canvas.
/// </summary>
public sealed class StatusHud : MonoBehaviour
{
    [Header("UI References (Drag & Drop from Canvas)")]
    [Tooltip("Parent panel of the Status HUD (optional, used for toggling visibility).")]
    [SerializeField] private GameObject panelRoot;
    [Tooltip("TextMeshProUGUI for the wallet balance (e.g. $100).")]
    [SerializeField] private TextMeshProUGUI moneyText;
    [Tooltip("TextMeshProUGUI for floating +$ / -$ transaction feedback.")]
    [SerializeField] private TextMeshProUGUI floatText;
    [Tooltip("TextMeshProUGUI for Day and Clock time (e.g. Day 1   7:00 AM).")]
    [SerializeField] private TextMeshProUGUI dayText;
    [Tooltip("TextMeshProUGUI for remaining deliveries count.")]
    [SerializeField] private TextMeshProUGUI deliveriesText;

    [Header("Look")]
    [SerializeField] private Color moneyColor = new Color(0.55f, 1f, 0.65f);
    [SerializeField] private Color gainColor = new Color(0.55f, 1f, 0.65f);
    [SerializeField] private Color lossColor = new Color(1f, 0.5f, 0.45f);
    [SerializeField, Min(0.5f)] private float floatDuration = 1.8f;

    private EconomyManager economy;
    private DailyBatchManager batch;
    private TimeManager time;
    private string lastDayLabel;

    private float displayedMoney;
    private int lastShownMoney = int.MinValue;
    private float floatTimer;
    private Vector2 floatBasePosition;

    private void Start()
    {
        if (moneyText == null || dayText == null || deliveriesText == null)
        {
            // Fallback: build from code if Inspector fields are not yet assigned.
            BuildUi();
        }
        else if (floatText != null)
        {
            floatBasePosition = floatText.rectTransform.anchoredPosition;
            floatText.text = string.Empty;
        }

        economy = EconomyManager.Instance;
        batch = DailyBatchManager.Instance;
        time = TimeManager.Instance;

        if (economy != null)
        {
            displayedMoney = economy.Balance;
            economy.TransactionMade += OnTransaction;
        }
        else
        {
            Debug.LogWarning("StatusHud: no EconomyManager in the scene.", this);
        }

        if (batch != null)
            batch.BatchChanged += RefreshDelivery;
        else
            Debug.LogWarning("StatusHud: no DailyBatchManager in the scene.", this);

        RefreshDelivery();
        UpdateMoneyText(true);
    }

    private void OnDestroy()
    {
        if (economy != null)
            economy.TransactionMade -= OnTransaction;

        if (batch != null)
            batch.BatchChanged -= RefreshDelivery;
    }

    private void BuildUi()
    {
        Canvas canvas = UIFactory.CreateCanvas("Status HUD", 11, transform);
        Transform root = canvas.transform;
        Vector2 topLeft = new Vector2(0f, 1f);

        moneyText = UIFactory.CreateText("Money", root, 46f, TextAlignmentOptions.TopLeft, moneyColor);
        moneyText.fontStyle = FontStyles.Bold;
        UIFactory.Place(moneyText.rectTransform, topLeft, topLeft, new Vector2(40f, -30f), new Vector2(300f, 60f));

        floatText = UIFactory.CreateText("MoneyChange", root, 36f, TextAlignmentOptions.TopLeft, gainColor);
        floatText.fontStyle = FontStyles.Bold;
        floatBasePosition = new Vector2(320f, -34f);
        UIFactory.Place(floatText.rectTransform, topLeft, topLeft, floatBasePosition, new Vector2(300f, 50f));
        floatText.text = string.Empty;

        dayText = UIFactory.CreateText("Day", root, 28f, TextAlignmentOptions.TopLeft, new Color(1f, 1f, 1f, 0.9f));
        UIFactory.Place(dayText.rectTransform, topLeft, topLeft, new Vector2(40f, -92f), new Vector2(500f, 36f));

        deliveriesText = UIFactory.CreateText("Deliveries", root, 28f, TextAlignmentOptions.TopLeft, Color.white);
        UIFactory.Place(deliveriesText.rectTransform, topLeft, topLeft, new Vector2(40f, -128f), new Vector2(800f, 40f));
    }

    private void Update()
    {
        if (economy != null)
        {
            float target = economy.Balance;
            float speed = Mathf.Max(40f, Mathf.Abs(target - displayedMoney) * 4f);
            displayedMoney = Mathf.MoveTowards(displayedMoney, target, speed * Time.unscaledDeltaTime);
            UpdateMoneyText(false);
        }

        UpdateFloatingText();
        UpdateDayLabel();
    }

    private void UpdateDayLabel()
    {
        string label;

        if (time != null)
        {
            label = $"Day {time.Day}   {time.FormatTime()}";
            if (time.IsPastMidnight)
                label += "   <color=#FF8A80>Very late!</color>";
        }
        else if (batch != null)
        {
            label = $"Day {batch.Day}";
        }
        else
        {
            label = string.Empty;
        }

        if (label == lastDayLabel)
            return;

        lastDayLabel = label;
        if (dayText != null)
            dayText.text = label;
    }

    private void UpdateMoneyText(bool force)
    {
        int shown = Mathf.RoundToInt(displayedMoney);
        if (!force && shown == lastShownMoney)
            return;

        lastShownMoney = shown;
        if (moneyText != null)
            moneyText.text = "$" + shown.ToString("N0");
    }

    private void UpdateFloatingText()
    {
        if (floatText == null || floatTimer <= 0f)
            return;

        floatTimer -= Time.unscaledDeltaTime;
        float progress = 1f - Mathf.Clamp01(floatTimer / floatDuration);

        floatText.rectTransform.anchoredPosition = floatBasePosition + Vector2.down * (progress * -40f);
        floatText.alpha = 1f - progress * progress;

        if (floatTimer <= 0f)
            floatText.text = string.Empty;
    }

    private void OnTransaction(int change, string reason)
    {
        if (floatText == null)
            return;

        floatText.text = change >= 0 ? $"+${change}" : $"-${-change}";
        floatText.color = change >= 0 ? gainColor : lossColor;
        floatText.alpha = 1f;
        floatText.rectTransform.anchoredPosition = floatBasePosition;
        floatTimer = floatDuration;
    }

    private void RefreshDelivery()
    {
        if (deliveriesText == null)
            return;

        if (batch == null || !batch.HasBatch)
        {
            deliveriesText.text = "No deliveries today";
            return;
        }

        string text = $"Deliveries left: {batch.RemainingCount} / {batch.TotalCount}";
        if (!batch.IsCollected)
            text += "   <size=70%><color=#FFD54A>Collect them at the warehouse</color></size>";

        deliveriesText.text = text;
    }
}

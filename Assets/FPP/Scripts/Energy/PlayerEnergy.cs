using System;
using UnityEngine;

/// <summary>
/// Tracks the player's stamina and energy. Put this on FPP Player.
/// - Energy naturally depletes throughout the workday.
/// - Carries packages faster: heavier boxes drain more stamina.
/// - Sprinting burns additional energy.
/// - Going to bed restores 100% energy.
/// - Collapsing from exhaustion (0%) passes out and wakes up with only partial energy (penalty).
/// </summary>
public sealed class PlayerEnergy : MonoBehaviour
{
    public static PlayerEnergy Instance { get; private set; }

    [Header("Energy Stats")]
    [SerializeField, Min(10f)] private float maxEnergy = 100f;
    [SerializeField] private float currentEnergy = 100f;

    [Header("Drain Rates (per in-game hour)")]
    [Tooltip("Base energy consumed per in-game hour just by being active.")]
    [SerializeField, Min(0f)] private float baseDrainPerHour = 3.5f;
    [Tooltip("Extra energy consumed per in-game hour while carrying any item.")]
    [SerializeField, Min(0f)] private float carryBaseDrainPerHour = 1.5f;
    [Tooltip("Additional drain per in-game hour per kilogram of the carried item.")]
    [SerializeField, Min(0f)] private float carryWeightDrainPerKgPerHour = 0.6f;
    [Tooltip("Extra energy consumed per in-game hour while sprinting.")]
    [SerializeField, Min(0f)] private float sprintDrainPerHour = 4.0f;

    [Header("Fatigue & Wake-up")]
    [Tooltip("Energy level below which the player is considered fatigued.")]
    [SerializeField, Range(5f, 40f)] private float lowEnergyThreshold = 20f;
    [Tooltip("Energy restored when properly sleeping in bed.")]
    [SerializeField, Range(50f, 100f)] private float sleepWakeEnergy = 100f;
    [Tooltip("Energy penalty wake-up level if the player collapsed/passed out from 0 energy.")]
    [SerializeField, Range(30f, 80f)] private float passOutWakeEnergy = 60f;

    private FppPlayerController controller;
    private ItemCarrier carrier;
    private TimeManager timeManager;

    private bool warnedLowEnergy;
    private bool isPassingOut;
    private bool wentToBedProperly;

    public float CurrentEnergy => currentEnergy;
    public float MaxEnergy => maxEnergy;
    public float NormalizedEnergy => Mathf.Clamp01(currentEnergy / maxEnergy);
    public bool IsLowEnergy => currentEnergy <= lowEnergyThreshold;
    public bool IsExhausted => currentEnergy <= 0.01f;

    public event Action<float> EnergyChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
        controller = GetComponent<FppPlayerController>();
        carrier = GetComponent<ItemCarrier>();
        currentEnergy = maxEnergy;
    }

    private void Start()
    {
        timeManager = TimeManager.Instance;
        if (timeManager != null)
        {
            timeManager.DayStarted += OnDayStarted;
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;

        if (timeManager != null)
            timeManager.DayStarted -= OnDayStarted;
    }

    private void Update()
    {
        if (isPassingOut)
            return;

        if (timeManager != null && timeManager.Paused)
            return;

        if (DayTransition.Instance != null && DayTransition.Instance.IsPlaying)
            return;

        // Convert delta time to in-game hours passed this frame
        float minutesPerSecond = timeManager != null ? timeManager.GameMinutesPerRealSecond : 1f;
        float inGameHoursThisFrame = (minutesPerSecond / 60f) * Time.deltaTime;

        float drain = baseDrainPerHour * inGameHoursThisFrame;

        // Additional drain when carrying an item
        if (carrier != null && carrier.IsHolding && carrier.HeldItem != null)
        {
            float weight = carrier.HeldItem.Weight;
            drain += (carryBaseDrainPerHour + weight * carryWeightDrainPerKgPerHour) * inGameHoursThisFrame;
        }

        // Additional drain when sprinting
        if (controller != null && controller.CurrentSpeed > 5.5f)
        {
            drain += sprintDrainPerHour * inGameHoursThisFrame;
        }

        if (drain > 0f)
            ConsumeEnergy(drain);

        // Movement effect: disable sprinting when completely exhausted
        if (controller != null)
        {
            controller.EnableSprinting = currentEnergy > 5f;
        }

        // Low energy warning
        if (currentEnergy <= lowEnergyThreshold && !warnedLowEnergy)
        {
            warnedLowEnergy = true;
            Notifier.Show("You're feeling exhausted. Head home to sleep soon!");
            GameAudio.Play(ProceduralSfx.Denied, transform.position, 0.6f);
        }
        else if (currentEnergy > lowEnergyThreshold)
        {
            warnedLowEnergy = false;
        }

        // Collapse when energy reaches 0
        if (currentEnergy <= 0f && !isPassingOut)
        {
            PassOut();
        }
    }

    public void ConsumeEnergy(float amount)
    {
        if (amount <= 0f)
            return;

        float prev = currentEnergy;
        currentEnergy = Mathf.Clamp(currentEnergy - amount, 0f, maxEnergy);

        if (Mathf.Abs(prev - currentEnergy) > 0.001f)
            EnergyChanged?.Invoke(currentEnergy);
    }

    public void RestoreEnergy(float amount)
    {
        if (amount <= 0f)
            return;

        currentEnergy = Mathf.Clamp(currentEnergy + amount, 0f, maxEnergy);
        EnergyChanged?.Invoke(currentEnergy);
    }

    /// <summary>Called by BedInteractable before advancing day to mark restful sleep.</summary>
    public void MarkPlannedSleep()
    {
        wentToBedProperly = true;
    }

    private void PassOut()
    {
        isPassingOut = true;
        wentToBedProperly = false;

        Notifier.Show("You collapsed from exhaustion!");

        if (timeManager != null)
        {
            timeManager.RequestNextDay("You collapsed from exhaustion.");
        }
    }

    private void OnDayStarted(int day)
    {
        isPassingOut = false;
        warnedLowEnergy = false;

        if (wentToBedProperly)
        {
            currentEnergy = sleepWakeEnergy;
        }
        else
        {
            // Penalized wake-up from passing out
            currentEnergy = passOutWakeEnergy;
        }

        wentToBedProperly = false;
        EnergyChanged?.Invoke(currentEnergy);
    }

    // --- Inspector Context Menus for Testing ---
    [ContextMenu("Debug: Drain 25 Energy")]
    private void DebugDrain25() => ConsumeEnergy(25f);

    [ContextMenu("Debug: Set Energy to 5%")]
    private void DebugLowEnergy()
    {
        currentEnergy = 5f;
        EnergyChanged?.Invoke(currentEnergy);
    }

    [ContextMenu("Debug: Refill to 100%")]
    private void DebugRefill() => RestoreEnergy(100f);
}

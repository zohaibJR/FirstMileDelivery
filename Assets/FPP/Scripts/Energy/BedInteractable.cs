using UnityEngine;

/// <summary>
/// Put this on a Bed GameObject (with a Collider).
/// Interacting with the bed allows the player to advance to the next morning and refill energy to 100%.
/// Available during the evening (after 6:00 PM / 18:00) or anytime energy is low.
/// </summary>
public sealed class BedInteractable : MonoBehaviour, IInteractable
{
    [Header("Sleeping Hours")]
    [Tooltip("Earliest in-game hour the player can choose to go to bed normally (18 = 6:00 PM).")]
    [SerializeField, Range(12f, 24f)] private float earliestSleepHour = 18f;

    [Tooltip("Allows sleeping during the day if energy is low.")]
    [SerializeField] private bool allowSleepIfLowEnergy = true;
    [SerializeField, Range(10f, 50f)] private float lowEnergyThreshold = 30f;

    public string InteractionPrompt
    {
        get
        {
            if (CanSleep())
                return "Sleep until morning (7:00 AM)";

            return $"Bed (Available after {FormatEarliestHour()} or when tired)";
        }
    }

    public bool CanInteract(PlayerInteractor interactor) => true;

    public void Interact(PlayerInteractor interactor)
    {
        if (!CanSleep())
        {
            Notifier.Show($"It's too early to sleep! You have deliveries to finish before {FormatEarliestHour()}.");
            GameAudio.Play(ProceduralSfx.Denied, transform.position, 0.7f);
            return;
        }

        // Mark intentional sleep so energy is fully restored (not penalized)
        if (PlayerEnergy.Instance != null)
        {
            PlayerEnergy.Instance.MarkPlannedSleep();
        }

        GameAudio.Play(ProceduralSfx.Thud, transform.position, 0.8f);

        if (TimeManager.Instance != null)
        {
            TimeManager.Instance.RequestNextDay("Good morning! Well rested.");
        }
        else
        {
            Notifier.Show("Good morning! Well rested.");
        }
    }

    private bool CanSleep()
    {
        TimeManager time = TimeManager.Instance;
        PlayerEnergy energy = PlayerEnergy.Instance;

        // Can always sleep if energy is critically low
        if (allowSleepIfLowEnergy && energy != null && energy.CurrentEnergy <= lowEnergyThreshold)
            return true;

        if (time == null)
            return true;

        // Evening (after closing hour) or very late after midnight
        if (time.IsPastMidnight || time.TimeOfDay >= earliestSleepHour)
            return true;

        return false;
    }

    private string FormatEarliestHour()
    {
        int hour = Mathf.FloorToInt(earliestSleepHour);
        if (hour > 12)
            return $"{hour - 12}:00 PM";
        return $"{hour}:00 AM";
    }
}

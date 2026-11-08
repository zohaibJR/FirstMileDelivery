using UnityEngine;

/// <summary>
/// The two daily payments. Put it on the "GameSystems" object.
/// - Daily salary: paid when the player collects the day's packages at the warehouse (showing up for work).
/// - Completion bonus: paid when every package of the day has been delivered.
/// Package rewards themselves are paid by DeliveryPoint.
/// </summary>
public sealed class DailyRewards : MonoBehaviour
{
    [Header("Daily Pay")]
    [Tooltip("Paid when the player collects the day's batch at the counter.")]
    [SerializeField, Min(0)] private int attendanceSalary = 20;
    [Tooltip("Paid when the last package of the day is delivered.")]
    [SerializeField, Min(0)] private int completionBonus = 15;

    private DailyBatchManager batch;

    private void Start()
    {
        batch = DailyBatchManager.Instance;

        if (batch == null)
        {
            Debug.LogWarning("DailyRewards: no DailyBatchManager in the scene, so nothing will be paid.", this);
            return;
        }

        batch.BatchCollected += OnCollected;
        batch.BatchCompleted += OnCompleted;
    }

    private void OnDestroy()
    {
        if (batch == null)
            return;

        batch.BatchCollected -= OnCollected;
        batch.BatchCompleted -= OnCompleted;
    }

    private void OnCollected()
    {
        if (attendanceSalary <= 0)
            return;

        Pay(attendanceSalary, "Daily salary");
        Notifier.Show($"Daily salary   +${attendanceSalary}");
    }

    private void OnCompleted()
    {
        string message = "All deliveries complete!";

        if (completionBonus > 0)
        {
            Pay(completionBonus, "Completion bonus");
            message += $"   Bonus +${completionBonus}";
        }

        Notifier.Show(message);
    }

    private static void Pay(int amount, string reason)
    {
        EconomyManager economy = EconomyManager.Instance;

        if (economy == null)
        {
            Debug.LogWarning("DailyRewards: no EconomyManager in the scene, so nothing was paid.");
            return;
        }

        economy.Earn(amount, reason);
    }
}
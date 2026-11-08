using System;
using UnityEngine;

/// <summary>
/// Weekly bills (rent + electricity). Put one on the Manager object.
/// Every billing day, at the new morning, the bills are paid automatically from the wallet.
/// Anything the player can't afford becomes debt. Old debt is paid first when money allows.
/// </summary>
public sealed class BillsManager : MonoBehaviour
{
    public static BillsManager Instance { get; private set; }

    [Header("Schedule")]
    [Tooltip("The first day bills are charged (morning).")]
    [SerializeField, Min(2)] private int firstBillDay = 8;
    [Tooltip("Days between bills. 7 = weekly.")]
    [SerializeField, Min(1)] private int billingIntervalDays = 7;

    [Header("Amounts")]
    [SerializeField, Min(0)] private int weeklyRent = 70;
    [SerializeField, Min(0)] private int weeklyElectricity = 15;

    public int Debt { get; private set; }
    public int WeeklyRent => weeklyRent;
    public int WeeklyElectricity => weeklyElectricity;

    public int VehicleRental { get; private set; }
    public int WeeklyTotal => weeklyRent + weeklyElectricity + VehicleRental;

    /// <summary>Fired when the debt amount changes. Parameter is the new debt.</summary>
    public event Action<int> DebtChanged;

    /// <summary>Fired after a billing day was processed.</summary>
    public event Action BillsProcessed;

    private TimeManager time;

    /// <summary>The next day number on which bills are charged.</summary>
    /// 
    public int NextBillDay
    {
        get
        {
            int today = time != null ? time.Day : 1;
            if (today < firstBillDay)
                return firstBillDay;

            int cycles = (today - firstBillDay) / billingIntervalDays + 1;
            return firstBillDay + cycles * billingIntervalDays;
        }
    }

    /// <summary>Weekly vehicle rental fee. 0 = no vehicle rented. Set by the rental office.</summary>
    public void SetVehicleRental(int weeklyFee)
    {
        VehicleRental = Mathf.Max(0, weeklyFee);
    }

    public int DaysUntilNextBill => time != null ? NextBillDay - time.Day : 0;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("BillsManager: more than one in the scene. Only one is allowed.", this);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        time = TimeManager.Instance;

        if (time != null)
            time.DayStarted += OnDayStarted;
        else
            Debug.LogWarning("BillsManager: no TimeManager in the scene, bills will never be charged.", this);

        if (EconomyManager.Instance == null)
            Debug.LogWarning("BillsManager: no EconomyManager in the scene.", this);
    }

    private void OnDestroy()
    {
        if (time != null)
            time.DayStarted -= OnDayStarted;

        if (Instance == this)
            Instance = null;
    }

    private void OnDayStarted(int day)
    {
        if (day < firstBillDay)
            return;

        if ((day - firstBillDay) % billingIntervalDays != 0)
            return;

        ChargeBills();
    }

    /// <summary>Charges old debt first, then this week's rent and electricity.</summary>
    public void ChargeBills()
    {
        EconomyManager economy = EconomyManager.Instance;
        if (economy == null)
            return;

        // 1. Old debt first
        if (Debt > 0 && economy.TrySpend(Debt, "Debt payment"))
        {
            Notifier.Show($"Old debt paid   -${Debt}");
            SetDebt(0);
        }

        // 2. This week's bills
        PayBill(economy, "Rent", weeklyRent);
        PayBill(economy, "Electricity", weeklyElectricity);
        PayBill(economy, "Vehicle rental", VehicleRental);

        BillsProcessed?.Invoke();
    }

    private void PayBill(EconomyManager economy, string name, int amount)
    {
        if (amount <= 0)
            return;

        if (economy.TrySpend(amount, name))
        {
            Notifier.Show($"{name} paid   -${amount}");
        }
        else
        {
            SetDebt(Debt + amount);
            Notifier.Show($"<color=#FF8A80>Couldn't pay {name}! ${amount} added to your debt</color>");
        }
    }

    /// <summary>Pays as much of the debt as the player can afford. Returns the amount paid.</summary>
    public int PayDebt()
    {
        EconomyManager economy = EconomyManager.Instance;
        if (economy == null || Debt <= 0)
            return 0;

        int amount = Mathf.Min(Debt, economy.Balance);
        if (amount <= 0)
            return 0;

        if (!economy.TrySpend(amount, "Debt payment"))
            return 0;

        SetDebt(Debt - amount);
        return amount;
    }

    /// <summary>Used by the save system later.</summary>
    public void SetDebt(int amount)
    {
        amount = Mathf.Max(0, amount);
        if (amount == Debt)
            return;

        Debt = amount;
        DebtChanged?.Invoke(Debt);
    }

    [ContextMenu("Debug: Charge Bills Now")]
    private void DebugChargeNow()
    {
        if (Application.isPlaying)
            ChargeBills();
    }

    [ContextMenu("Debug: Add $50 Debt")]
    private void DebugAddDebt()
    {
        if (Application.isPlaying)
            SetDebt(Debt + 50);
    }
}
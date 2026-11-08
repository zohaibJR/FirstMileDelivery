using System;
using UnityEngine;

/// <summary>
/// The player's wallet. Put one in the scene (for example on the "GameSystems" object).
/// Everything that earns or spends money goes through here: deliveries, salary, bills, rental.
/// </summary>
public sealed class EconomyManager : MonoBehaviour
{
    public static EconomyManager Instance { get; private set; }

    [SerializeField, Min(0)] private int startingMoney = 50;

    public int Balance { get; private set; }

    /// <summary>Fired whenever the balance changes. Parameter is the new balance.</summary>
    public event Action<int> BalanceChanged;

    /// <summary>Fired for every payment. Parameters: change (+earned / -spent) and a reason.</summary>
    public event Action<int, string> TransactionMade;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("EconomyManager: more than one in the scene. Only one is allowed.", this);
            return;
        }

        Instance = this;
        Balance = startingMoney;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public bool CanAfford(int amount) => amount <= Balance;

    public void Earn(int amount, string reason)
    {
        if (amount <= 0)
            return;

        Balance += amount;
        BalanceChanged?.Invoke(Balance);
        TransactionMade?.Invoke(amount, reason);
    }

    /// <summary>Spends money if the player has enough. Returns false (and spends nothing) otherwise.</summary>
    public bool TrySpend(int amount, string reason)
    {
        if (amount <= 0)
            return true;

        if (!CanAfford(amount))
            return false;

        Balance -= amount;
        BalanceChanged?.Invoke(Balance);
        TransactionMade?.Invoke(-amount, reason);
        return true;
    }

    /// <summary>Used by the save system later.</summary>
    public void SetBalance(int amount)
    {
        Balance = Mathf.Max(0, amount);
        BalanceChanged?.Invoke(Balance);
    }

    [ContextMenu("Debug: Earn $100")]
    private void DebugEarn()
    {
        if (Application.isPlaying)
            Earn(100, "Debug");
    }
}
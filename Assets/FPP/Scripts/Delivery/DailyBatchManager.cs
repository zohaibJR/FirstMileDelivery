using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>One package the player must deliver today.</summary>
[Serializable]
public class PackageAssignment
{
    public PackageDefinition definition;
    public string houseId;
    public string address;
    public bool collected;
    public bool delivered;
}

[Serializable]
public class PackageTypeWeight
{
    public PackageDefinition definition;
    [Min(0f)] public float weight = 1f;
}

/// <summary>
/// Creates the list of packages assigned to the player for the day.
/// Put one in the scene (for example on an empty object called "GameSystems").
/// The same day number always gives the same batch (handy for testing and saving).
/// Later the TimeManager will call StartDay() every morning.
/// </summary>
public sealed class DailyBatchManager : MonoBehaviour
{
    public static DailyBatchManager Instance { get; private set; }

    [Header("Package Types (weight = how common)")]
    [SerializeField] private List<PackageTypeWeight> packageTypes = new List<PackageTypeWeight>();

    [Header("Batch Size")]
    [SerializeField, Min(1)] private int minPackages = 3;
    [SerializeField, Min(1)] private int maxPackages = 5;
    [Tooltip("Off: every package goes to a different house. On: a house can get several packages.")]
    [SerializeField] private bool allowRepeatHouses = false;

    [Header("Day")]
    [SerializeField, Min(1)] private int startDay = 1;
    [SerializeField] private bool generateOnStart = true;
    [Tooltip("Change this number to get a different set of batches.")]
    [SerializeField] private int seed = 12345;

    private readonly List<PackageAssignment> assignments = new List<PackageAssignment>();

    public IReadOnlyList<PackageAssignment> Assignments => assignments;
    public int Day { get; private set; }
    public bool HasBatch => assignments.Count > 0;
    public bool IsCollected { get; private set; }
    public int TotalCount => assignments.Count;

    public int RemainingCount
    {
        get
        {
            int remaining = 0;
            foreach (PackageAssignment a in assignments)
                if (!a.delivered)
                    remaining++;
            return remaining;
        }
    }

    /// <summary>Fired whenever the batch changes (new day, collected, delivered).</summary>
    public event Action BatchChanged;

    /// <summary>Fired once when the last package of the day is delivered (daily bonus hooks in here).</summary>
    public event Action BatchCompleted;

    /// <summary>Fired once when the player collects the whole batch at the counter (daily salary hooks in here).</summary>
    public event Action BatchCollected;

    private TimeManager subscribedTime;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("DailyBatchManager: more than one in the scene. Only one is allowed.", this);
            return;
        }

        Instance = this;
    }

    private void OnDestroy()
    {
        if (subscribedTime != null)
            subscribedTime.DayStarted -= StartDay;

        if (Instance == this)
            Instance = null;
    }

    private void Start()
    {
        // With a TimeManager in the scene, it decides the day number and starts every new morning.
        TimeManager time = TimeManager.Instance;

        if (time != null)
        {
            subscribedTime = time;
            time.DayStarted += StartDay;

            if (generateOnStart)
                StartDay(time.Day);
        }
        else if (generateOnStart)
        {
            StartDay(startDay);
        }
    }

    /// <summary>Starts a new day and creates its batch of packages.</summary>
    public void StartDay(int day)
    {
        Day = Mathf.Max(1, day);
        GenerateBatch();
    }

    /// <summary>Called by the counter when the player collects the whole batch.</summary>
    public void MarkCollected()
    {
        IsCollected = true;
        foreach (PackageAssignment a in assignments)
            a.collected = true;

        BatchChanged?.Invoke();
        BatchCollected?.Invoke();
    }

    public int DeliveredCount => TotalCount - RemainingCount;

    /// <summary>
    /// Marks one matching assignment (same house and package type) as delivered.
    /// Returns false if no open assignment matches (for example a hand-placed test package).
    /// </summary>
    public bool TryMarkDelivered(string houseId, PackageDefinition definition)
    {
        foreach (PackageAssignment a in assignments)
        {
            if (a.delivered || a.houseId != houseId || a.definition != definition)
                continue;

            a.delivered = true;
            BatchChanged?.Invoke();

            if (RemainingCount == 0)
                BatchCompleted?.Invoke();

            return true;
        }

        return false;
    }

    private void GenerateBatch()
    {
        assignments.Clear();
        IsCollected = false;

        IReadOnlyList<HouseId> houses = HouseId.All;

        if (houses.Count == 0)
        {
            Debug.LogWarning("DailyBatchManager: no HouseId objects in the scene, so no packages can be assigned.", this);
            BatchChanged?.Invoke();
            return;
        }

        if (!HasValidPackageType())
        {
            Debug.LogWarning("DailyBatchManager: Package Types list is empty. Add your Small, Medium and Large definitions.", this);
            BatchChanged?.Invoke();
            return;
        }

        System.Random rng = new System.Random(seed + Day * 7919);

        int max = Mathf.Max(minPackages, maxPackages);
        int count = rng.Next(minPackages, max + 1);

        if (!allowRepeatHouses)
            count = Mathf.Min(count, houses.Count);

        // Shuffle a copy of the houses (Fisher-Yates)
        List<HouseId> shuffled = new List<HouseId>(houses);
        for (int i = shuffled.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            HouseId temp = shuffled[i];
            shuffled[i] = shuffled[j];
            shuffled[j] = temp;
        }

        for (int i = 0; i < count; i++)
        {
            HouseId house = allowRepeatHouses ? houses[rng.Next(houses.Count)] : shuffled[i];

            assignments.Add(new PackageAssignment
            {
                definition = PickDefinition(rng),
                houseId = house.Id,
                address = house.Address
            });
        }

        BatchChanged?.Invoke();
    }

    private bool HasValidPackageType()
    {
        foreach (PackageTypeWeight t in packageTypes)
            if (t.definition != null && t.weight > 0f)
                return true;
        return false;
    }

    private PackageDefinition PickDefinition(System.Random rng)
    {
        float total = 0f;
        foreach (PackageTypeWeight t in packageTypes)
            if (t.definition != null)
                total += t.weight;

        double roll = rng.NextDouble() * total;

        foreach (PackageTypeWeight t in packageTypes)
        {
            if (t.definition == null)
                continue;

            roll -= t.weight;
            if (roll <= 0.0)
                return t.definition;
        }

        return packageTypes[packageTypes.Count - 1].definition;
    }

    // Right click the component header in Play mode to use this.
    [ContextMenu("Debug: Start Next Day")]
    private void DebugNextDay()
    {
        if (!Application.isPlaying)
            return;

        if (TimeManager.Instance != null)
            TimeManager.Instance.RequestNextDay("Debug");
        else
            StartDay(Day + 1);
    }
}
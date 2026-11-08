using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;

/// <summary>
/// The warehouse pickup counter. Put this on the counter object (needs a Collider).
/// - Only works during the opening hours (needs a TimeManager in the scene).
/// - Shows today's assigned deliveries on an optional board (3D TextMeshPro).
/// - Looking at it and pressing E collects the whole batch: every package is spawned
///   on the counter, ready to be picked up and delivered.
/// - When a new day starts, packages nobody delivered are returned to the warehouse.
/// </summary>
public sealed class PackageCounter : MonoBehaviour, IInteractable
{
    [SerializeField] private DailyBatchManager manager;

    [Header("Opening Hours")]
    [Tooltip("Off: the counter is always open.")]
    [SerializeField] private bool useOpeningHours = true;
    [SerializeField, Range(0f, 24f)] private float openHour = 7f;
    [SerializeField, Range(0f, 24f)] private float closeHour = 18f;

    [Header("Spawning")]
    [Tooltip("Empty objects on the counter top. Packages appear here. If empty, packages appear in front of the counter.")]
    [SerializeField] private Transform[] spawnPoints;
    [Tooltip("Height between stacked packages when there are more packages than spawn points.")]
    [SerializeField, Min(0.1f)] private float stackHeight = 0.45f;

    [Header("Board (optional)")]
    [Tooltip("A 3D TextMeshPro text object (GameObject > 3D Object > Text - TextMeshPro).")]
    [SerializeField] private TMP_Text boardText;

    private readonly List<Package> spawned = new List<Package>();
    private TimeManager time;
    private ItemCarrier lastCarrier;

    public IReadOnlyList<Package> SpawnedPackages => spawned;

    /// <summary>True while the warehouse is open (always true without a TimeManager).</summary>
    public bool IsOpen => !useOpeningHours || time == null || time.IsBetween(openHour, closeHour);

    public string InteractionPrompt
    {
        get
        {
            if (manager == null)
                return "No packages";

            if (!IsOpen)
                return $"Warehouse closed (opens {FormatHour(openHour)})";

            return $"Collect today's packages ({manager.TotalCount})";
        }
    }

    private void Start()
    {
        if (manager == null)
            manager = DailyBatchManager.Instance;

        time = TimeManager.Instance;

        if (manager != null)
            manager.BatchChanged += RefreshBoard;
        else
            Debug.LogWarning("PackageCounter: no DailyBatchManager found in the scene.", this);

        if (time != null)
        {
            time.HourChanged += OnHourChanged;
            time.DayStarted += OnDayStarted;
        }

        RefreshBoard();
    }

    private void OnDestroy()
    {
        if (manager != null)
            manager.BatchChanged -= RefreshBoard;

        if (time != null)
        {
            time.HourChanged -= OnHourChanged;
            time.DayStarted -= OnDayStarted;
        }
    }

    public bool CanInteract(PlayerInteractor interactor)
    {
        return manager != null && manager.HasBatch && !manager.IsCollected;
    }

    public void Interact(PlayerInteractor interactor)
    {
        if (!CanInteract(interactor))
            return;

        if (!IsOpen)
        {
            Notifier.Show($"The warehouse is closed. It opens at {FormatHour(openHour)}");
            GameAudio.Play(ProceduralSfx.Denied, transform.position, 0.7f);
            return;
        }

        lastCarrier = interactor.GetComponent<ItemCarrier>();

        spawned.Clear();
        IReadOnlyList<PackageAssignment> assignments = manager.Assignments;

        for (int i = 0; i < assignments.Count; i++)
        {
            PackageAssignment a = assignments[i];
            GameObject prefab = a.definition != null ? a.definition.Prefab : null;

            if (prefab == null)
            {
                Debug.LogWarning($"PackageCounter: '{(a.definition != null ? a.definition.name : "null")}' has no Prefab assigned.", this);
                continue;
            }

            Quaternion rotation = Quaternion.Euler(0f, Random.Range(-20f, 20f), 0f) * transform.rotation;
            GameObject instance = Instantiate(prefab, GetSpawnPosition(i), rotation);

            Package package = instance.GetComponent<Package>();
            if (package != null)
            {
                package.Initialize(a.definition, a.houseId, a.address);
                spawned.Add(package);
            }
            else
            {
                Debug.LogWarning($"PackageCounter: prefab '{prefab.name}' has no Package component.", this);
            }
        }

        if (spawned.Count == 0)
        {
            Notifier.Show("Package prefab missing. Check the Console.");
            return;
        }

        // Message first, then MarkCollected (which makes DailyRewards pay the salary and show its message).
        Notifier.Show($"Collected {spawned.Count} packages");
        GameAudio.Play(ProceduralSfx.Pickup, transform.position, 0.8f, 0.05f, 0.3f);
        manager.MarkCollected();
    }

    private Vector3 GetSpawnPosition(int index)
    {
        if (spawnPoints != null && spawnPoints.Length > 0)
        {
            Transform point = spawnPoints[index % spawnPoints.Length];
            int layer = index / spawnPoints.Length;
            return point.position + Vector3.up * (layer * stackHeight);
        }

        // Fallback: a small grid in front of the counter, dropping to the floor.
        int column = index % 3;
        int row = index / 3;
        return transform.position
            + transform.forward * 0.9f
            + transform.right * (column - 1) * 0.6f
            + Vector3.up * (1.2f + row * 0.5f);
    }

    private void OnHourChanged(int hour)
    {
        RefreshBoard();
    }

    private void OnDayStarted(int day)
    {
        ReturnLeftovers();
    }

    /// <summary>New morning: packages that were never delivered go back to the warehouse.</summary>
    private void ReturnLeftovers()
    {
        int returned = 0;

        foreach (Package package in spawned)
        {
            if (package == null || package.IsDelivered)
                continue;

            PickupItem item = package.GetComponent<PickupItem>();
            if (item != null && item.IsHeld && lastCarrier != null)
                lastCarrier.DetachHeld();

            Destroy(package.gameObject);
            returned++;
        }

        spawned.Clear();

        if (returned > 0)
            Notifier.Show("Undelivered packages were returned to the warehouse");
    }

    private string FormatHour(float hour)
    {
        return time != null ? time.Format(hour) : $"{hour:0}:00";
    }

    private void RefreshBoard()
    {
        if (boardText == null)
            return;

        if (manager == null || !manager.HasBatch)
        {
            boardText.text = "<b>NO PACKAGES TODAY</b>";
            return;
        }

        StringBuilder sb = new StringBuilder();
        sb.AppendLine($"<b>DAY {manager.Day} DELIVERIES</b>");

        if (useOpeningHours)
        {
            string state = IsOpen
                ? "<color=#7CFC9A>OPEN</color>"
                : "<color=#FF8A80>CLOSED</color>";
            sb.AppendLine($"{FormatHour(openHour)} - {FormatHour(closeHour)}   {state}");
        }

        sb.AppendLine(manager.IsCollected
            ? "<color=#7CFC9A>Collected</color>"
            : "<color=#FFD54A>Ready to collect</color>");
        sb.AppendLine();

        IReadOnlyList<PackageAssignment> assignments = manager.Assignments;
        for (int i = 0; i < assignments.Count; i++)
        {
            PackageAssignment a = assignments[i];
            string size = a.definition != null ? a.definition.Size.ToString() : "?";
            sb.AppendLine($"{i + 1}. {size} - {a.address}");
        }

        boardText.text = sb.ToString();
    }
}
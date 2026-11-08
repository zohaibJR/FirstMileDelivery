using System;
using UnityEngine;

/// <summary>
/// The game clock. Put one in the scene (for example on the "GameSystems" object).
/// - Time runs forward all the time (scaled by "Game Minutes Per Real Second").
/// - The day number only goes up when a new morning starts (sleeping, or passing out when it gets too late).
/// - Other systems listen to HourChanged / DayEnded / DayStarted.
/// Sleeping (Milestone 06) will simply call RequestNextDay("Good morning!").
/// </summary>
[DefaultExecutionOrder(-50)]
public sealed class TimeManager : MonoBehaviour
{
    public static TimeManager Instance { get; private set; }

    [Header("Clock")]
    [SerializeField, Min(1)] private int startDay = 1;
    [Tooltip("The time at the start of the game, and the time you wake up every morning.")]
    [SerializeField, Range(0f, 24f)] private float morningHour = 7f;
    [Tooltip("In-game minutes that pass every real second. 1 = a full 24 hour day takes 24 real minutes. 2 = 12 real minutes.")]
    [SerializeField, Min(0f)] private float gameMinutesPerRealSecond = 1f;
    [SerializeField] private bool use24HourClock = false;

    [Header("Late Night (temporary until sleeping exists)")]
    [Tooltip("If the player is still awake very late, they pass out and the next morning starts.")]
    [SerializeField] private bool passOutWhenTooLate = true;
    [Tooltip("Hour AFTER midnight when the player passes out. 2 = 2:00 AM.")]
    [SerializeField, Range(0f, 6f)] private float passOutHour = 2f;

    /// <summary>Current day number (goes up at each new morning).</summary>
    public int Day { get; private set; }

    /// <summary>Time of day in hours, 0 to 24 (7.5 = 7:30 AM).</summary>
    public float TimeOfDay { get; private set; }

    /// <summary>True after midnight but before the next morning (the "very late" part of the day).</summary>
    public bool IsPastMidnight { get; private set; }

    /// <summary>Other systems can freeze the clock (the day transition does).</summary>
    public bool Paused { get; set; }

    public float NormalizedTime => TimeOfDay / 24f;
    public int Hour => Mathf.FloorToInt(TimeOfDay);
    public int Minute => Mathf.FloorToInt((TimeOfDay - Hour) * 60f);
    public float MorningHour => morningHour;

    public float GameMinutesPerRealSecond
    {
        get => gameMinutesPerRealSecond;
        set => gameMinutesPerRealSecond = Mathf.Max(0f, value);
    }

    /// <summary>Fired when the hour changes. Parameter is the new hour (0 to 23).</summary>
    public event Action<int> HourChanged;

    /// <summary>Fired when a day ends (just before the next one starts). Parameter is the day that ended.</summary>
    public event Action<int> DayEnded;

    /// <summary>Fired at the start of every new day. Parameter is the new day number.</summary>
    public event Action<int> DayStarted;

    private int lastHour;
    private bool nextDayPending;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("TimeManager: more than one in the scene. Only one is allowed.", this);
            return;
        }

        Instance = this;
        Day = startDay;
        TimeOfDay = morningHour;
        lastHour = Hour;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void Update()
    {
        if (Paused || gameMinutesPerRealSecond <= 0f)
            return;

        Advance(gameMinutesPerRealSecond / 60f * Time.deltaTime);
    }

    private void Advance(float hours)
    {
        TimeOfDay += hours;

        if (TimeOfDay >= 24f)
        {
            TimeOfDay -= 24f;
            IsPastMidnight = true;
        }

        int hour = Hour;
        if (hour != lastHour)
        {
            lastHour = hour;
            HourChanged?.Invoke(hour);
        }

        if (passOutWhenTooLate && IsPastMidnight && TimeOfDay >= passOutHour)
            RequestNextDay("You passed out from exhaustion.");
    }

    /// <summary>
    /// Starts the next morning. If a DayTransition exists it fades to black first,
    /// otherwise the new day starts instantly. Safe to call more than once.
    /// </summary>
    public void RequestNextDay(string reason = null)
    {
        if (nextDayPending)
            return;

        nextDayPending = true;

        if (DayTransition.Instance != null)
            DayTransition.Instance.Play(reason);
        else
            CompleteNextDay();
    }

    /// <summary>Does the actual day change. Called by DayTransition when the screen is black.</summary>
    public void CompleteNextDay()
    {
        DayEnded?.Invoke(Day);

        Day++;
        TimeOfDay = morningHour;
        IsPastMidnight = false;
        lastHour = Hour;
        nextDayPending = false;

        DayStarted?.Invoke(Day);
        HourChanged?.Invoke(lastHour);
    }

    /// <summary>True between open (included) and close (excluded). Always false after midnight.</summary>
    public bool IsBetween(float openHour, float closeHour)
    {
        if (IsPastMidnight)
            return false;

        return TimeOfDay >= openHour && TimeOfDay < closeHour;
    }

    public void SetTime(float hours, bool pastMidnight = false)
    {
        TimeOfDay = Mathf.Repeat(hours, 24f);
        IsPastMidnight = pastMidnight;
        lastHour = Hour;
        HourChanged?.Invoke(lastHour);
    }

    public string FormatTime() => Format(TimeOfDay);

    /// <summary>Turns an hour value (7.5) into text ("7:30 AM" or "07:30").</summary>
    public string Format(float hours)
    {
        int totalMinutes = Mathf.FloorToInt(Mathf.Repeat(hours, 24f) * 60f);
        int h = totalMinutes / 60;
        int m = totalMinutes % 60;

        if (use24HourClock)
            return $"{h:00}:{m:00}";

        string suffix = h >= 12 ? "PM" : "AM";
        int h12 = h % 12;
        if (h12 == 0)
            h12 = 12;

        return $"{h12}:{m:00} {suffix}";
    }

    // ---- Right click the component header in Play mode to use these ----

    [ContextMenu("Debug: +1 hour")]
    private void DebugPlusHour()
    {
        if (Application.isPlaying)
            Advance(1f);
    }

    [ContextMenu("Debug: Jump to 5:55 PM (just before the warehouse closes)")]
    private void DebugBeforeClosing()
    {
        if (Application.isPlaying)
            SetTime(17f + 55f / 60f);
    }

    [ContextMenu("Debug: Jump to 1:55 AM (just before passing out)")]
    private void DebugBeforePassOut()
    {
        if (Application.isPlaying)
            SetTime(1f + 55f / 60f, true);
    }

    [ContextMenu("Debug: Next morning")]
    private void DebugNextMorning()
    {
        if (Application.isPlaying)
            RequestNextDay("Debug");
    }
}
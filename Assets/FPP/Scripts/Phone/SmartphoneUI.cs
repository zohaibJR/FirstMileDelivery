using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// The smartphone. Put this on the FPP Player.
/// Tab (gamepad View) opens and closes it. Q / E (gamepad LB / RB) switch pages.
/// While it is open the player is frozen, but the game clock keeps running.
/// It can't be opened while driving, inspecting an item, or during the day transition.
/// </summary>
public sealed class SmartphoneUI : MonoBehaviour
{
    public static SmartphoneUI Instance { get; private set; }

    [Header("UI References (drag from Main Canvas)")]
    [Tooltip("The Phone_Panel object. It is hidden at start and shown when the phone opens.")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private TextMeshProUGUI clockText;
    [Tooltip("In page order: Deliveries, Map, Info.")]
    [SerializeField] private TextMeshProUGUI[] tabLabels;
    [Tooltip("In the same order as the tab labels: Page_Deliveries, Page_Map, Page_Info.")]
    [SerializeField] private GameObject[] pages;

    [Header("Look")]
    [SerializeField] private Color activeTabColor = new Color(1f, 0.84f, 0.29f);
    [SerializeField] private Color inactiveTabColor = new Color(0.6f, 0.65f, 0.72f);

    public bool IsOpen { get; private set; }
    public int CurrentPage { get; private set; }

    public event Action Opened;
    public event Action Closed;
    public event Action<int> PageChanged;

    private PlayerInputLock inputLock;
    private ItemCarrier carrier;
    private TimeManager time;
    private string lastClock;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("SmartphoneUI: more than one in the scene. Only one is allowed.", this);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        inputLock = GetComponent<PlayerInputLock>();
        if (inputLock == null)
            inputLock = gameObject.AddComponent<PlayerInputLock>();

        carrier = GetComponent<ItemCarrier>();
        time = TimeManager.Instance;

        if (time != null)
            time.DayEnded += OnDayEnded;

        if (panelRoot == null)
            Debug.LogWarning("SmartphoneUI: Panel Root is not assigned.", this);

        ShowPage(0);
        SetPanelVisible(false);
    }

    private void OnDestroy()
    {
        if (time != null)
            time.DayEnded -= OnDayEnded;

        if (IsOpen && inputLock != null)
            inputLock.Unlock(this);

        if (Instance == this)
            Instance = null;
    }

    private void Update()
    {
        Keyboard kb = Keyboard.current;
        Gamepad pad = Gamepad.current;

        bool toggle = (kb != null && kb.tabKey.wasPressedThisFrame)
                   || (pad != null && pad.selectButton.wasPressedThisFrame);

        if (!IsOpen)
        {
            if (toggle)
                TryOpen();
            return;
        }

        if (toggle)
        {
            Close();
            return;
        }

        bool previous = (kb != null && kb.qKey.wasPressedThisFrame)
                     || (pad != null && pad.leftShoulder.wasPressedThisFrame);
        bool next = (kb != null && kb.eKey.wasPressedThisFrame)
                 || (pad != null && pad.rightShoulder.wasPressedThisFrame);

        if (previous)
            SetPage(CurrentPage - 1);
        else if (next)
            SetPage(CurrentPage + 1);

        UpdateClock();
    }

    private void OnDayEnded(int day)
    {
        if (IsOpen)
            Close();
    }

    private void TryOpen()
    {
        // Not while another system (vehicle, inspect, transition...) has the player.
        if (inputLock.IsLocked)
            return;

        if (carrier != null && carrier.IsInspecting)
            return;

        inputLock.Lock(this);
        if (!inputLock.IsLocked)
            return;

        IsOpen = true;
        SetPanelVisible(true);
        lastClock = null;
        UpdateClock();

        GameAudio.Play(ProceduralSfx.Click, transform.position, 0.6f, 0f, 0f);
        Opened?.Invoke();
    }

    public void Close()
    {
        if (!IsOpen)
            return;

        IsOpen = false;
        SetPanelVisible(false);
        inputLock.Unlock(this);

        GameAudio.Play(ProceduralSfx.Click, transform.position, 0.5f, 0f, 0f);
        Closed?.Invoke();
    }

    public void SetPage(int index)
    {
        int count = pages != null ? pages.Length : 0;
        if (count == 0)
            return;

        index = (index % count + count) % count;
        if (index == CurrentPage)
            return;

        ShowPage(index);
        GameAudio.Play(ProceduralSfx.Click, transform.position, 0.5f, 0.05f, 0f);
        PageChanged?.Invoke(index);
    }

    private void ShowPage(int index)
    {
        CurrentPage = index;

        if (pages != null)
        {
            for (int i = 0; i < pages.Length; i++)
                if (pages[i] != null)
                    pages[i].SetActive(i == index);
        }

        if (tabLabels != null)
        {
            for (int i = 0; i < tabLabels.Length; i++)
                if (tabLabels[i] != null)
                    tabLabels[i].color = i == index ? activeTabColor : inactiveTabColor;
        }
    }

    private void SetPanelVisible(bool visible)
    {
        if (panelRoot != null)
            panelRoot.SetActive(visible);
    }

    private void UpdateClock()
    {
        if (clockText == null || time == null)
            return;

        string label = $"Day {time.Day}   {time.FormatTime()}";
        if (label == lastClock)
            return;

        lastClock = label;
        clockText.text = label;
    }
}
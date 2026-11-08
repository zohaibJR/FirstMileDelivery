using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Inventory screen (built from code). Put this on the FPP Player next to Inventory.
/// Tab (or gamepad Select) opens it and freezes the player. Up/Down (or W/S, D-pad) selects,
/// Q (or gamepad B) drops the selected item into the world.
/// </summary>
public sealed class InventoryPanel : MonoBehaviour
{
    [Header("References (auto-found if empty)")]
    [SerializeField] private Inventory inventory;
    [SerializeField] private Camera viewCamera;

    [Header("Font")]
    [Tooltip("Same TMP Font Asset as the Interaction Hud. Empty = TextMeshPro default font.")]
    [SerializeField] private TMP_FontAsset font;

    [Header("Input")]
    [SerializeField] private Key toggleKey = Key.Tab;
    [SerializeField] private Key dropKey = Key.Q;

    [Header("Layout")]
    [SerializeField, Range(4, 12)] private int visibleRows = 10;

    private sealed class Row
    {
        public Image background;
        public Image icon;
        public TextMeshProUGUI label;
        public TextMeshProUGUI count;
    }

    private static readonly Color RowIdle = new Color(1f, 1f, 1f, 0.05f);
    private static readonly Color RowSelected = new Color(1f, 0.84f, 0.3f, 0.28f);

    private GameObject root;
    private Row[] rows;
    private TextMeshProUGUI weightText, detailTitle, detailBody, footer;
    private PlayerInputLock inputLock;
    private int selected;
    private int scroll;
    private bool isOpen;

    public bool IsOpen => isOpen;

    private void Awake()
    {
        if (inventory == null) inventory = GetComponent<Inventory>();
        if (viewCamera == null) viewCamera = GetComponentInChildren<Camera>(true);
        if (viewCamera == null) viewCamera = Camera.main;

        inputLock = PlayerInputLock.Get(gameObject);
        if (font != null)
            UIFactory.DefaultFont = font;

        BuildUi();

        if (inventory != null)
            inventory.Changed += OnInventoryChanged;
    }

    private void OnDestroy()
    {
        if (inventory != null)
            inventory.Changed -= OnInventoryChanged;
    }

    private void OnDisable()
    {
        if (isOpen)
            Close();
    }

    private void BuildUi()
    {
        Canvas canvas = UIFactory.CreateCanvas("Inventory UI", 20, transform);
        root = canvas.gameObject;
        Transform rootT = canvas.transform;

        Image dim = UIFactory.CreateImage("Dim", rootT, new Color(0f, 0f, 0f, 0.6f));
        UIFactory.Stretch(dim.rectTransform);

        Vector2 mid = new Vector2(0.5f, 0.5f);
        Image panel = UIFactory.CreateImage("Panel", rootT, new Color(0.07f, 0.08f, 0.1f, 0.96f));
        UIFactory.Place(panel.rectTransform, mid, mid, Vector2.zero, new Vector2(1100f, 660f));

        Vector2 topLeft = new Vector2(0f, 1f);
        Vector2 topRight = new Vector2(1f, 1f);

        TextMeshProUGUI title = UIFactory.CreateText("Title", panel.transform, 44f, TextAlignmentOptions.TopLeft, Color.white);
        title.text = "Inventory";
        title.fontStyle = FontStyles.Bold;
        UIFactory.Place(title.rectTransform, topLeft, topLeft, new Vector2(30f, -18f), new Vector2(500f, 60f));

        weightText = UIFactory.CreateText("Weight", panel.transform, 26f, TextAlignmentOptions.TopRight, new Color(0.7f, 0.75f, 0.8f));
        UIFactory.Place(weightText.rectTransform, topRight, topRight, new Vector2(-30f, -28f), new Vector2(500f, 40f));

        rows = new Row[visibleRows];
        for (int i = 0; i < visibleRows; i++)
        {
            Row row = new Row();

            row.background = UIFactory.CreateImage("Row" + i, panel.transform, RowIdle);
            UIFactory.Place(row.background.rectTransform, topLeft, topLeft, new Vector2(30f, -90f - i * 50f), new Vector2(560f, 46f));

            row.icon = UIFactory.CreateImage("Icon", row.background.transform, Color.white);
            UIFactory.Place(row.icon.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(8f, 0f), new Vector2(34f, 34f));

            row.label = UIFactory.CreateText("Label", row.background.transform, 26f, TextAlignmentOptions.MidlineLeft, Color.white);
            UIFactory.Place(row.label.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(54f, 0f), new Vector2(400f, 40f));

            row.count = UIFactory.CreateText("Count", row.background.transform, 26f, TextAlignmentOptions.MidlineRight, new Color(0.8f, 0.85f, 0.9f));
            UIFactory.Place(row.count.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-12f, 0f), new Vector2(100f, 40f));

            rows[i] = row;
        }

        Image detail = UIFactory.CreateImage("Detail", panel.transform, new Color(1f, 1f, 1f, 0.05f));
        UIFactory.Place(detail.rectTransform, topRight, topRight, new Vector2(-30f, -90f), new Vector2(430f, 500f));

        detailTitle = UIFactory.CreateText("DetailTitle", detail.transform, 34f, TextAlignmentOptions.TopLeft, Color.white, true);
        detailTitle.fontStyle = FontStyles.Bold;
        UIFactory.Place(detailTitle.rectTransform, topLeft, topLeft, new Vector2(20f, -16f), new Vector2(390f, 50f));

        detailBody = UIFactory.CreateText("DetailBody", detail.transform, 24f, TextAlignmentOptions.TopLeft, new Color(0.88f, 0.88f, 0.9f), true);
        UIFactory.Place(detailBody.rectTransform, topLeft, topLeft, new Vector2(20f, -76f), new Vector2(390f, 400f));

        footer = UIFactory.CreateText("Footer", panel.transform, 24f, TextAlignmentOptions.Bottom, new Color(0.8f, 0.85f, 0.9f));
        UIFactory.Place(footer.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 14f), new Vector2(1000f, 40f));

        root.SetActive(false);
    }

    private void Update()
    {
        if (inventory == null)
            return;

        Keyboard keyboard = Keyboard.current;
        Mouse mouse = Mouse.current;
        Gamepad gamepad = Gamepad.current;

        bool togglePressed =
            (keyboard != null && keyboard[toggleKey].wasPressedThisFrame) ||
            (gamepad != null && gamepad.selectButton.wasPressedThisFrame);

        if (!isOpen)
        {
            if (togglePressed && Cursor.lockState == CursorLockMode.Locked)
                Open();
            return;
        }

        InputGlyphs.Poll();

        bool escapePressed = keyboard != null && keyboard.escapeKey.wasPressedThisFrame;
        if (togglePressed || escapePressed)
        {
            Close();
            return;
        }

        int move = 0;
        if (keyboard != null)
        {
            if (keyboard.upArrowKey.wasPressedThisFrame || keyboard.wKey.wasPressedThisFrame) move--;
            if (keyboard.downArrowKey.wasPressedThisFrame || keyboard.sKey.wasPressedThisFrame) move++;
        }
        if (gamepad != null)
        {
            if (gamepad.dpad.up.wasPressedThisFrame) move--;
            if (gamepad.dpad.down.wasPressedThisFrame) move++;
        }
        if (mouse != null)
        {
            float wheel = mouse.scroll.ReadValue().y;
            if (wheel > 0.1f) move--;
            else if (wheel < -0.1f) move++;
        }

        if (move != 0)
        {
            selected += move;
            Refresh();
        }

        bool dropPressed =
            (keyboard != null && keyboard[dropKey].wasPressedThisFrame) ||
            (gamepad != null && gamepad.buttonEast.wasPressedThisFrame);

        if (dropPressed && inventory.Slots.Count > 0)
            inventory.DropFromSlot(selected, viewCamera != null ? viewCamera.transform : transform);

        footer.text = $"{InputGlyphs.Pick("W/S", "D-Pad")} Select   " +
                      $"{InputGlyphs.Pick(dropKey, "B")} Drop   " +
                      $"{InputGlyphs.Pick(toggleKey, "View")} Close";
    }

    private void Open()
    {
        if (inputLock.IsLocked || !inputLock.Lock(this))
            return;

        isOpen = true;
        root.SetActive(true);
        Refresh();
    }

    private void Close()
    {
        isOpen = false;
        root.SetActive(false);
        inputLock.Unlock(this);
    }

    private void OnInventoryChanged()
    {
        if (isOpen)
            Refresh();
    }

    private void Refresh()
    {
        var slots = inventory.Slots;

        selected = Mathf.Clamp(selected, 0, Mathf.Max(0, slots.Count - 1));

        if (selected < scroll)
            scroll = selected;
        else if (selected >= scroll + visibleRows)
            scroll = selected - visibleRows + 1;

        for (int i = 0; i < rows.Length; i++)
        {
            Row row = rows[i];
            int index = scroll + i;

            if (index >= inventory.SlotCapacity)
            {
                row.background.gameObject.SetActive(false);
                continue;
            }

            row.background.gameObject.SetActive(true);
            row.background.color = index == selected ? RowSelected : RowIdle;

            if (index < slots.Count && slots[index].item != null)
            {
                ItemData item = slots[index].item;
                row.label.text = item.DisplayName;
                row.label.color = Color.white;
                row.count.text = slots[index].count > 1 ? "x" + slots[index].count : string.Empty;
                row.icon.sprite = item.Icon;
                row.icon.enabled = item.Icon != null;
            }
            else
            {
                row.label.text = "Empty";
                row.label.color = new Color(1f, 1f, 1f, 0.3f);
                row.count.text = string.Empty;
                row.icon.enabled = false;
            }
        }

        // Details
        if (slots.Count > 0 && slots[selected].item != null)
        {
            ItemData item = slots[selected].item;
            detailTitle.text = item.DisplayName;

            string body = string.IsNullOrWhiteSpace(item.Description) ? "No description." : item.Description;
            body += $"\n\n<color=#9AA7B0>Weight: {item.Weight:0.##}</color>";
            detailBody.text = body;
        }
        else
        {
            detailTitle.text = string.Empty;
            detailBody.text = "<color=#9AA7B0>Nothing here yet.</color>";
        }

        weightText.text = inventory.MaxWeight > 0f
            ? $"Weight {inventory.TotalWeight:0.#} / {inventory.MaxWeight:0.#}"
            : $"Weight {inventory.TotalWeight:0.#}";
    }
}
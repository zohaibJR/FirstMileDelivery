using UnityEngine;

public enum PickupMode
{
    /// <summary>Held in front of the camera (big objects like crates).</summary>
    Carry,
    /// <summary>Goes straight into the inventory (keys, notes, small items).</summary>
    Store
}

/// <summary>
/// Data for one kind of item. Create with: right click in Project, Create > Game > Item Data.
/// </summary>
[CreateAssetMenu(fileName = "NewItem", menuName = "Game/Item Data")]
public class ItemData : ScriptableObject
{
    [SerializeField] private string itemId;
    [SerializeField] private string displayName = "New Item";
    [SerializeField, TextArea(2, 6)] private string description;
    [SerializeField] private Sprite icon;

    [Header("Rules")]
    [SerializeField, Min(0f)] private float weight = 0.5f;
    [SerializeField, Min(1)] private int maxStack = 10;
    [SerializeField] private PickupMode pickupMode = PickupMode.Carry;
    [Tooltip("Carry items only: allows pressing the Store key while holding it.")]
    [SerializeField] private bool canStoreWhenCarried = true;

    [Header("World")]
    [Tooltip("Prefab spawned when the item is dropped from the inventory. It needs a PickupItem.")]
    [SerializeField] private GameObject worldPrefab;

    public string Id => string.IsNullOrEmpty(itemId) ? name : itemId;
    public string DisplayName => displayName;
    public string Description => description;
    public Sprite Icon => icon;
    public float Weight => weight;
    public int MaxStack => maxStack;
    public PickupMode Mode => pickupMode;
    public bool CanStoreWhenCarried => canStoreWhenCarried;
    public GameObject WorldPrefab => worldPrefab;
}
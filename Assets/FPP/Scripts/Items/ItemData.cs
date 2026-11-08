using UnityEngine;

public enum PickupMode
{
    /// <summary>Held in front of the camera (packages, crates, keys, tools).</summary>
    Carry
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

    [Header("World")]
    [Tooltip("Prefab spawned when the item is placed or dropped in the world. Needs a PickupItem.")]
    [SerializeField] private GameObject worldPrefab;

    public string Id => string.IsNullOrEmpty(itemId) ? name : itemId;
    public string DisplayName => displayName;
    public string Description => description;
    public Sprite Icon => icon;
    public float Weight => weight;
    public GameObject WorldPrefab => worldPrefab;
}

using UnityEngine;

public enum PackageSize
{
    Small,
    Medium,
    Large
}

/// <summary>
/// Data for one TYPE of package (Small / Medium / Large).
/// Create with: right click in Project, Create > Game > Package Definition.
/// Rewards are fixed per package type.
/// </summary>
[CreateAssetMenu(fileName = "Package_Small", menuName = "Game/Package Definition")]
public class PackageDefinition : ScriptableObject
{
    [Header("Info")]
    [SerializeField] private string displayName = "Small Package";
    [SerializeField] private PackageSize size = PackageSize.Small;
    [SerializeField, TextArea(2, 4)] private string description = "Handle with care.";

    [Header("Gameplay")]
    [Tooltip("Weight in kg. Used as the Rigidbody mass (and later for energy cost while carrying).")]
    [SerializeField, Min(0.1f)] private float weight = 1.5f;
    [Tooltip("Fixed money paid for a successful delivery.")]
    [SerializeField, Min(0)] private int reward = 5;

    [Header("Look")]
    [Tooltip("World size of the box in metres (applied as the object's scale).")]
    [SerializeField] private Vector3 dimensions = new Vector3(0.35f, 0.25f, 0.3f);
    [SerializeField] private Color tint = new Color(0.79f, 0.65f, 0.42f);

    [Header("Spawning")]
    [Tooltip("Prefab used when this package is spawned in the world. Needs PickupItem + Package.")]
    [SerializeField] private GameObject prefab;

    public string DisplayName => displayName;
    public PackageSize Size => size;
    public string Description => description;
    public float Weight => weight;
    public int Reward => reward;
    public Vector3 Dimensions => dimensions;
    public Color Tint => tint;
    public GameObject Prefab => prefab;
}
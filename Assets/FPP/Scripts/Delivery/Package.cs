using UnityEngine;

/// <summary>
/// Put this on the package prefab, next to PickupItem (and a Rigidbody + Collider).
/// It turns a plain pickup into a delivery package: size, weight and look come from a
/// PackageDefinition, and it remembers where it has to be delivered.
/// The daily batch system later fills in the destination at runtime.
/// </summary>
[RequireComponent(typeof(PickupItem))]
public sealed class Package : MonoBehaviour
{
    [SerializeField] private PackageDefinition definition;

    [Header("Destination (the daily system sets this at runtime; fill in by hand to test)")]
    [SerializeField] private string destinationId = "";
    [SerializeField] private string destinationLabel = "";

    [Header("Look")]
    [Tooltip("Apply scale and colour from the definition. Turn off if your prefab has its own model.")]
    [SerializeField] private bool applyDefinitionVisuals = true;

    private PickupItem pickup;

    public PackageDefinition Definition => definition;
    public string DestinationId => destinationId;
    public string DestinationLabel => destinationLabel;
    public int Reward => definition != null ? definition.Reward : 0;
    public bool IsDelivered { get; private set; }

    private void Awake()
    {
        pickup = GetComponent<PickupItem>();
        Apply();
    }

    /// <summary>Used by the spawner right after creating the package.</summary>
    public void Initialize(PackageDefinition newDefinition, string newDestinationId, string newDestinationLabel)
    {
        definition = newDefinition;
        destinationId = newDestinationId;
        destinationLabel = newDestinationLabel;
        Apply();
    }

    public void MarkDelivered()
    {
        IsDelivered = true;

        // A delivered package can't be picked up (or paid) again.
        if (pickup != null)
            pickup.SetPickupEnabled(false);
    }

    private void Apply()
    {
        if (definition == null)
            return;

        if (pickup == null)
            pickup = GetComponent<PickupItem>();

        if (applyDefinitionVisuals)
        {
            transform.localScale = definition.Dimensions;
            ApplyTint(definition.Tint);
        }

        Rigidbody body = GetComponent<Rigidbody>();
        if (body != null)
            body.mass = definition.Weight;

        pickup.SetInfo(definition.DisplayName, BuildDescription(), definition.Weight);
    }

    private string BuildDescription()
    {
        string destination = string.IsNullOrEmpty(destinationLabel) ? "Unknown" : destinationLabel;

        string text = $"<b>Deliver to:</b> {destination}\n" +
                      $"<b>Size:</b> {definition.Size}\n" +
                      $"<b>Pays:</b> ${definition.Reward}";

        if (!string.IsNullOrWhiteSpace(definition.Description))
            text += "\n\n" + definition.Description;

        return text;
    }

    private void ApplyTint(Color color)
    {
        MaterialPropertyBlock block = new MaterialPropertyBlock();

        foreach (Renderer r in GetComponentsInChildren<Renderer>())
        {
            r.GetPropertyBlock(block);
            block.SetColor("_BaseColor", color); // URP
            block.SetColor("_Color", color);     // Built-in
            r.SetPropertyBlock(block);
        }
    }
}
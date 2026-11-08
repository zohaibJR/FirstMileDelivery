using UnityEngine;

/// <summary>
/// Put this on any pickup prefab (needs a Collider and a Rigidbody).
/// Optionally assign an ItemData to give it a name, description, weight and inventory behaviour.
/// Without ItemData it simply uses the name below and is carried in front of the camera.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class PickupItem : MonoBehaviour, IInteractable
{
    [Header("Item")]
    [Tooltip("Optional. Overrides the name and description below.")]
    [SerializeField] private ItemData data;
    [SerializeField] private string itemName = "Cube";
    [SerializeField, TextArea(2, 5)] private string description;

    [Header("Impact Sound")]
    [SerializeField] private AudioClip impactClip;
    [SerializeField] private bool useProceduralImpact = true;
    [SerializeField, Min(0f)] private float minImpactSpeed = 1.5f;

    private Rigidbody body;
    private Collider[] colliders;
    private float nextImpactTime;

    public bool IsHeld { get; private set; }
    public ItemData Data => data;
    // Runtime override (used by Package so each package can show its own name and destination).
    private bool hasOverride;
    private string overrideName;
    private string overrideDescription;
    private float overrideWeight;

    public string DisplayName => hasOverride ? overrideName : (data != null ? data.DisplayName : itemName);
    public string Description => hasOverride ? overrideDescription : (data != null ? data.Description : description);
    public float Weight => hasOverride ? overrideWeight : (data != null ? data.Weight : 0f);

    /// <summary>Overrides the name, description and weight shown in the HUD and inspect panel.</summary>
    public void SetInfo(string newName, string newDescription, float newWeight)
    {
        hasOverride = true;
        overrideName = newName;
        overrideDescription = newDescription;
        overrideWeight = newWeight;
    }
    public bool IsStoreOnly => data != null && data.Mode == PickupMode.Store;

    public string InteractionPrompt => IsStoreOnly ? $"Take {DisplayName}" : $"Pick up {DisplayName}";

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
        colliders = GetComponentsInChildren<Collider>(true);
    }

    private bool pickupEnabled = true;

    /// <summary>Turn off to make this item un-pickable (for example after a package is delivered).</summary>
    public void SetPickupEnabled(bool enabledForPickup)
    {
        pickupEnabled = enabledForPickup;
    }

    public bool CanInteract(PlayerInteractor interactor)
    {
        if (IsHeld || !pickupEnabled)
            return false;

        // Small store-only items can always be taken. Carried items need free hands.
        if (IsStoreOnly)
            return true;

        ItemCarrier carrier = interactor.GetComponent<ItemCarrier>();
        return carrier == null || !carrier.IsHolding;
    }

    public void Interact(PlayerInteractor interactor)
    {
        if (IsStoreOnly)
        {
            Inventory inventory = interactor.GetComponent<Inventory>();
            if (inventory != null)
            {
                TryStore(inventory);
                return;
            }

            Debug.LogWarning("PickupItem: store-only item but the player has no Inventory. Carrying it instead.", this);
        }

        ItemCarrier carrier = interactor.GetComponent<ItemCarrier>();
        if (carrier == null)
        {
            Debug.LogWarning("PickupItem: no ItemCarrier found on the player.", this);
            return;
        }

        carrier.Pick(this);
    }

    /// <summary>Moves this item into the inventory and removes it from the world.</summary>
    public bool TryStore(Inventory inventory)
    {
        if (data == null || inventory == null)
            return false;

        if (inventory.Add(data, 1) <= 0)
        {
            Notifier.Show("Can't carry any more");
            GameAudio.Play(ProceduralSfx.Denied, transform.position, 0.6f, 0f, 0.3f);
            return false;
        }

        Notifier.Show($"Picked up {data.DisplayName}");
        GameAudio.Play(ProceduralSfx.Pickup, transform.position, 0.8f, 0.05f, 0.3f);
        Destroy(gameObject);
        return true;
    }

    /// <summary>Called by ItemCarrier. Turns off physics so the item can follow the camera.</summary>
    public void OnPickedUp()
    {
        IsHeld = true;
        body.isKinematic = true;
        body.detectCollisions = false;
        foreach (Collider c in colliders)
            c.enabled = false;
    }

    /// <summary>Called by ItemCarrier. Turns physics back on, optionally with a throw impulse.</summary>
    public void OnReleased(Vector3 velocityChange)
    {
        IsHeld = false;
        foreach (Collider c in colliders)
            c.enabled = true;
        body.detectCollisions = true;
        body.isKinematic = false;

        if (velocityChange.sqrMagnitude > 0.0001f)
            body.AddForce(velocityChange, ForceMode.VelocityChange);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (IsHeld || Time.timeSinceLevelLoad < 1f || Time.time < nextImpactTime)
            return;

        float speed = collision.relativeVelocity.magnitude;
        if (speed < minImpactSpeed)
            return;

        nextImpactTime = Time.time + 0.12f;

        AudioClip clip = GameAudio.Or(impactClip, useProceduralImpact ? ProceduralSfx.Thud : null);
        float volume = Mathf.Clamp01(speed / 8f) * 0.9f + 0.1f;
        GameAudio.Play(clip, collision.GetContact(0).point, volume, 0.15f);
    }
}
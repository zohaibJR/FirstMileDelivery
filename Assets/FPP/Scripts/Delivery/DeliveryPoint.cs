using UnityEngine;

/// <summary>
/// Where a package is handed over. Put this on the house (the same object as HouseId, so you
/// can aim at the house) or on its front door / porch object (needs a Collider, and a HouseId
/// on itself or a parent).
/// While the player holds a package, looking at this and pressing E:
///  - wrong house  : refuses with a message, nothing is paid
///  - correct house: pays the fixed reward ONCE, updates the remaining counter, and puts the
///                   package down so it can't be picked up or paid again.
/// </summary>
public sealed class DeliveryPoint : MonoBehaviour, IInteractable
{
    [Tooltip("Auto-found on this object or a parent if empty.")]
    [SerializeField] private HouseId house;

    [Header("Placement")]
    [Tooltip("Optional empty object on the porch where delivered packages are put down. If empty, the package is put down right where the player held it.")]
    [SerializeField] private Transform placePoint;
    [SerializeField, Min(0f)] private float scatter = 0.15f;
    [Tooltip("Seconds until a delivered package disappears. 0 = never.")]
    [SerializeField, Min(0f)] private float cleanupDelay = 120f;

    private ItemCarrier carrier;

    public string InteractionPrompt
    {
        get
        {
            Package package = GetHeldPackage();
            if (package == null || package.Definition == null)
                return "Deliver";
            return $"Deliver {package.Definition.DisplayName}";
        }
    }

    private void Awake()
    {
        if (house == null)
            house = GetComponentInParent<HouseId>();

        if (house == null)
            Debug.LogWarning("DeliveryPoint: no HouseId found on this object or its parents.", this);
    }

    public bool CanInteract(PlayerInteractor interactor)
    {
        if (carrier == null)
            carrier = interactor.GetComponent<ItemCarrier>();

        Package package = GetHeldPackage();
        return house != null && package != null && !package.IsDelivered;
    }

    public void Interact(PlayerInteractor interactor)
    {
        Package package = GetHeldPackage();
        if (house == null || package == null || package.IsDelivered)
            return;

        // 1. Validate destination
        if (package.DestinationId != house.Id)
        {
            string target = string.IsNullOrEmpty(package.DestinationLabel) ? "another house" : package.DestinationLabel;
            Notifier.Show($"Wrong house! This package is for {target}");
            GameAudio.Play(ProceduralSfx.Denied, transform.position, 0.7f);
            return;
        }

        // 2. Take it out of the player's hands
        PickupItem item = carrier.DetachHeld();
        if (item == null)
            return;

        // 3. Lock it FIRST so it can never be paid twice
        package.MarkDelivered();

        // 4. Update the day's list
        DailyBatchManager batch = DailyBatchManager.Instance;
        bool wasAssigned = batch != null && batch.TryMarkDelivered(house.Id, package.Definition);

        if (!wasAssigned)
            Debug.Log($"DeliveryPoint: '{package.name}' was not part of today's batch (probably a hand-placed test package). Paid anyway.", this);

        // 5. Pay once
        int reward = package.Reward;
        if (EconomyManager.Instance != null)
            EconomyManager.Instance.Earn(reward, $"Delivery to {house.Address}");
        else
            Debug.LogWarning("DeliveryPoint: no EconomyManager in the scene, so nothing was paid.", this);

        // 6. Feedback
        Notifier.Show($"Delivered to {house.Address}   +${reward}");
        GameAudio.Play(ProceduralSfx.Unlock, transform.position, 0.8f, 0.05f, 0.4f);

        if (batch != null && batch.HasBatch && wasAssigned && batch.RemainingCount == 0)
            Notifier.Show("All deliveries complete!");

        // 7. Put the package down
        PlacePackage(item);
    }

    private void PlacePackage(PickupItem item)
    {
        if (placePoint != null)
        {
            Vector2 jitter = Random.insideUnitCircle * scatter;
            Vector3 position = placePoint.position + new Vector3(jitter.x, 0.1f, jitter.y);
            item.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f));
        }

        item.OnReleased(Vector3.zero);

        if (cleanupDelay > 0f)
            Destroy(item.gameObject, cleanupDelay);
    }

    private Package GetHeldPackage()
    {
        if (carrier == null || carrier.HeldItem == null)
            return null;

        return carrier.HeldItem.GetComponent<Package>();
    }
}
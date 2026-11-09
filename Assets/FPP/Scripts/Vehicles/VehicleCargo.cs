using UnityEngine;

/// <summary>
/// The truck bed. Put this on a child of the truck that has a BoxCollider covering the open bed.
/// - Holding a package, look at the bed and press E: it snaps into the next free spot, visible and fixed in place.
/// - Hands empty, look at a package in the bed and press E: you pick that package up.
/// Spots are a grid (columns x rows) inside the BoxCollider: local X = across, local Z = along.
/// </summary>
[RequireComponent(typeof(BoxCollider))]
public sealed class VehicleCargo : MonoBehaviour, IInteractable
{
    [Header("Layout")]
    [SerializeField, Min(1)] private int columns = 2;
    [SerializeField, Min(1)] private int rows = 4;
    [SerializeField, Min(1f)] private float aimDistance = 4f;

    private BoxCollider bay;
    private Transform vehicleRoot;
    private Collider[] vehicleColliders;
    private Package[] slots;
    private ItemCarrier carrier;

    public int Capacity => columns * rows;

    public int Count
    {
        get
        {
            int n = 0;
            if (slots == null)
                return 0;

            for (int i = 0; i < slots.Length; i++)
                if (slots[i] != null)
                    n++;
            return n;
        }
    }

    public string InteractionPrompt
    {
        get
        {
            if (carrier != null && carrier.IsHolding)
            {
                Package held = carrier.HeldItem.GetComponent<Package>();
                if (held == null)
                    return "Cargo bed";

                return Count >= Capacity
                    ? $"Truck bed is full ({Count}/{Capacity})"
                    : $"Load package ({Count}/{Capacity})";
            }

            Package aimed = FindAimedPackage();
            if (aimed != null)
                return $"Take {aimed.DestinationLabel}";

            return $"Cargo bed ({Count}/{Capacity})";
        }
    }

    private void Awake()
    {
        bay = GetComponent<BoxCollider>();
        slots = new Package[Capacity];

        Rigidbody body = GetComponentInParent<Rigidbody>();
        vehicleRoot = body != null ? body.transform : transform;
        vehicleColliders = vehicleRoot.GetComponentsInChildren<Collider>();
    }

    public bool CanInteract(PlayerInteractor interactor)
    {
        if (carrier == null)
            carrier = interactor.GetComponent<ItemCarrier>();

        return true;
    }

    public void Interact(PlayerInteractor interactor)
    {
        if (carrier == null)
            carrier = interactor.GetComponent<ItemCarrier>();

        if (carrier == null)
            return;

        if (carrier.IsHolding)
            Load();
        else
            Unload();
    }

    private void Update()
    {
        // If a package was picked up some other way, free its spot.
        for (int i = 0; i < slots.Length; i++)
        {
            Package p = slots[i];
            if (p == null)
                continue;

            PickupItem item = p.GetComponent<PickupItem>();
            if (item != null && item.IsHeld)
                Release(i);
        }
    }

    private void Load()
    {
        Package package = carrier.HeldItem != null ? carrier.HeldItem.GetComponent<Package>() : null;
        if (package == null || package.IsDelivered)
        {
            Notifier.Show("Only packages go in the truck bed");
            return;
        }

        int index = FirstFreeSlot();
        if (index < 0)
        {
            Notifier.Show("The truck bed is full");
            GameAudio.Play(ProceduralSfx.Denied, transform.position, 0.7f);
            return;
        }

        PickupItem item = carrier.DetachHeld();
        if (item == null)
            return;

        item.OnReleased(Vector3.zero);
        PlaceInSlot(package, index);

        Notifier.Show($"Loaded {package.DestinationLabel}   ({Count}/{Capacity})");
        GameAudio.Play(ProceduralSfx.Thud, transform.position, 0.8f);
    }

    private void Unload()
    {
        if (Count == 0)
        {
            Notifier.Show("The truck bed is empty");
            return;
        }

        Package package = FindAimedPackage();
        if (package == null)
            return;

        int index = System.Array.IndexOf(slots, package);
        PickupItem item = package.GetComponent<PickupItem>();
        if (index < 0 || item == null)
            return;

        Release(index);
        carrier.Pick(item);

        Notifier.Show($"Took {package.DestinationLabel}   ({Count}/{Capacity})");
        GameAudio.Play(ProceduralSfx.Pickup, transform.position, 0.8f);
    }

    private int FirstFreeSlot()
    {
        for (int i = 0; i < slots.Length; i++)
            if (slots[i] == null)
                return i;
        return -1;
    }

    private void PlaceInSlot(Package package, int index)
    {

        Transform t = package.transform;
        t.SetParent(vehicleRoot, true);
        t.rotation = vehicleRoot.rotation;

        Bounds bayInRoot = GetBayBoundsInRootSpace(bay, vehicleRoot);
        Vector3 worldFloor = vehicleRoot.TransformPoint(SlotLocalInRoot(bayInRoot, index, columns, rows));
        t.position = worldFloor;

        // Lower or raise it so the bottom of the package sits exactly on the bed floor.
        Renderer r = package.GetComponentInChildren<Renderer>();
        if (r != null)
            t.position += Vector3.up * (worldFloor.y - r.bounds.min.y);

        Rigidbody rb = package.GetComponent<Rigidbody>();
        if (rb != null)
        {
            if (!rb.isKinematic)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            rb.interpolation = RigidbodyInterpolation.None;
            rb.isKinematic = true;
        }

        SetCollisionWithVehicle(package, false);
        slots[index] = package;
    }

    // The bed area measured in the truck root's own axes: X = across, Z = along the truck.
    // This works even if CargoBed itself is scaled or rotated.
    private static Bounds GetBayBoundsInRootSpace(BoxCollider box, Transform root)
    {
        Vector3 c = box.center;
        Vector3 h = box.size * 0.5f;
        Bounds b = new Bounds(root.InverseTransformPoint(box.transform.TransformPoint(c - h)), Vector3.zero);

        for (int x = -1; x <= 1; x += 2)
            for (int y = -1; y <= 1; y += 2)
                for (int z = -1; z <= 1; z += 2)
                    b.Encapsulate(root.InverseTransformPoint(box.transform.TransformPoint(c + new Vector3(x * h.x, y * h.y, z * h.z))));

        return b;
    }

    private static Vector3 SlotLocalInRoot(Bounds b, int index, int columns, int rows)
    {
        int column = index % columns;
        int row = index / columns;

        float x = b.min.x + (column + 0.5f) * (b.size.x / columns);
        float z = b.min.z + (row + 0.5f) * (b.size.z / rows);
        return new Vector3(x, b.min.y, z);
    }

    private void Release(int index)
    {
        Package package = slots[index];
        slots[index] = null;

        if (package == null)
            return;

        SetCollisionWithVehicle(package, true);
        package.transform.SetParent(null, true);
    }

    private void SetCollisionWithVehicle(Package package, bool collide)
    {
        foreach (Collider pc in package.GetComponentsInChildren<Collider>())
        {
            foreach (Collider vc in vehicleColliders)
            {
                if (vc != null && vc != pc)
                    Physics.IgnoreCollision(pc, vc, !collide);
            }
        }
    }

    private Package FindAimedPackage()
    {
        Camera cam = Camera.main;
        if (cam == null || slots == null)
            return null;

        Ray ray = new Ray(cam.transform.position, cam.transform.forward);

        // 1. A package the ray goes straight through
        Package best = null;
        float bestDistance = aimDistance;

        for (int i = 0; i < slots.Length; i++)
        {
            Package p = slots[i];
            if (p == null)
                continue;

            Renderer r = p.GetComponentInChildren<Renderer>();
            if (r != null && r.bounds.IntersectRay(ray, out float d) && d < bestDistance)
            {
                best = p;
                bestDistance = d;
            }
        }

        if (best != null)
            return best;

        // 2. Otherwise the package nearest to the spot of the bed you are looking at
        if (!bay.Raycast(ray, out RaycastHit hit, aimDistance))
            return null;

        float nearest = float.MaxValue;
        for (int i = 0; i < slots.Length; i++)
        {
            Package p = slots[i];
            if (p == null)
                continue;

            float d = (p.transform.position - hit.point).sqrMagnitude;
            if (d < nearest)
            {
                nearest = d;
                best = p;
            }
        }

        return best;
    }

    // Select the CargoBed object in the Scene view to see the spots as yellow dots.
    private void OnDrawGizmosSelected()
    {
        BoxCollider box = GetComponent<BoxCollider>();
        if (box == null)
            return;

        Rigidbody body = GetComponentInParent<Rigidbody>();
        Transform root = body != null ? body.transform : transform;
        Bounds b = GetBayBoundsInRootSpace(box, root);

        Gizmos.color = Color.yellow;
        for (int i = 0; i < columns * rows; i++)
            Gizmos.DrawWireSphere(root.TransformPoint(SlotLocalInRoot(b, i, columns, rows)), 0.07f);
    }
}
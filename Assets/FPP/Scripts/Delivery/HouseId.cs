using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Put this on every house in the town (on the house root or its front door).
/// Gives the house a unique ID and a readable address. The daily batch system picks
/// destinations from all HouseId objects that exist in the scene.
/// </summary>
public sealed class HouseId : MonoBehaviour
{
    [Tooltip("Unique ID, for example H-01. Must be different for every house.")]
    [SerializeField] private string id = "H-01";

    [Tooltip("What the player sees on packages, the board and later the phone.")]
    [SerializeField] private string address = "1 Oak Street";

    [Tooltip("Optional. Where a package should be handed over (porch). Defaults to this object.")]
    [SerializeField] private Transform deliveryPoint;

    private static readonly List<HouseId> Registry = new List<HouseId>();

    public static IReadOnlyList<HouseId> All => Registry;

    public string Id => id;
    public string Address => address;
    public Vector3 DeliveryPosition => deliveryPoint != null ? deliveryPoint.position : transform.position;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        Registry.Clear();
    }

    private void OnEnable()
    {
        foreach (HouseId other in Registry)
        {
            if (other != null && other.id == id)
                Debug.LogWarning($"HouseId: duplicate ID '{id}' on '{name}' and '{other.name}'. IDs must be unique!", this);
        }

        Registry.Add(this);
    }

    private void OnDisable()
    {
        Registry.Remove(this);
    }

    public static bool TryGet(string houseId, out HouseId house)
    {
        foreach (HouseId h in Registry)
        {
            if (h != null && h.id == houseId)
            {
                house = h;
                return true;
            }
        }

        house = null;
        return false;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 0.84f, 0.3f);
        Gizmos.DrawWireSphere(DeliveryPosition, 0.25f);

#if UNITY_EDITOR
        UnityEditor.Handles.Label(DeliveryPosition + Vector3.up * 0.5f, $"{id}\n{address}");
#endif
    }
}
using UnityEngine;

public enum PackageStatus
{
    Waiting,
    Picked,
    Delivered
}


public class PackageData : MonoBehaviour
{
    public string packageID;
    public PackageStatus status = PackageStatus.Waiting;

    [Header("Package Address")]
    public HouseAddress address;  // Now enum dropdown

    public CustomerNames customerName;
}

using System.Collections;
using UnityEngine;

public class PackageSpawner : MonoBehaviour
{
    [Header("Spawner Settings")]
    public GameObject packagePrefab;
    public float spawnInterval = 10f;
    public Transform spawnPoint;

    [Header("Package Options")]
    public HouseAddress[] possibleAddresses;
    public CustomerNames[] possibleCustomers;

    [Header("Area Control")]
    public PackageAreaCounter areaHouse; // Drag your AreaHouse here

    private void Start()
    {
        StartCoroutine(SpawnPackages());
    }

    private IEnumerator SpawnPackages()
    {
        while (true)
        {
            if (areaHouse != null && areaHouse.CanSpawn())
            {
                SpawnPackage();
            }

            yield return new WaitForSeconds(spawnInterval);
        }
    }

    private void SpawnPackage()
    {
        GameObject packageObj = Instantiate(
            packagePrefab,
            spawnPoint.position,
            Quaternion.identity
        );

        PackageData packageData = packageObj.GetComponent<PackageData>();

        if (packageData == null)
        {
            Debug.LogError("Package prefab is missing PackageData component!");
            return;
        }

        packageData.address =
            possibleAddresses[Random.Range(0, possibleAddresses.Length)];

        packageData.customerName =
            possibleCustomers[Random.Range(0, possibleCustomers.Length)];

        packageData.packageID = System.Guid.NewGuid().ToString();
    }
}

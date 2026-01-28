using System.Collections;
using UnityEngine;

public class PackageSpawner : MonoBehaviour
{
    [Header("Spawner Settings")]
    public GameObject packagePrefab;       // Assign your Package prefab here
    public float spawnInterval = 10f;      // Time in seconds between spawns
    public Transform spawnPoint;           // Optional: Where the package spawns

    [Header("Package Options")]
    public HouseAddress[] possibleAddresses;  // Assign all possible HouseAddress enum values
    public CustomerNames[] possibleCustomers; // Assign all possible CustomerNames enum values

    private void Start()
    {
        // Start the spawning coroutine
        StartCoroutine(SpawnPackages());
    }

    private IEnumerator SpawnPackages()
    {
        while (true)
        {
            SpawnPackage();
            yield return new WaitForSeconds(spawnInterval);
        }
    }

    private void SpawnPackage()
    {
        // Instantiate package prefab
        GameObject packageObj = Instantiate(packagePrefab, spawnPoint.position, Quaternion.identity);

        // Get PackageData component
        PackageData packageData = packageObj.GetComponent<PackageData>();

        if (packageData != null)
        {
            // Randomly assign HouseAddress and CustomerNames
            packageData.address = possibleAddresses[Random.Range(0, possibleAddresses.Length)];
            packageData.customerName = possibleCustomers[Random.Range(0, possibleCustomers.Length)];

            // Optional: Assign a unique packageID
            packageData.packageID = System.Guid.NewGuid().ToString();
        }
        else
        {
            Debug.LogError("Package prefab is missing PackageData component!");
        }
    }
}

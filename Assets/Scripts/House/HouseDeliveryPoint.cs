using UnityEngine;
using System.Collections;

public class HouseDeliveryPoint : MonoBehaviour
{
    public OrderUIController UIcontroller;

    [Header("This House Address")]
    public HouseAddress houseAddress;   // Enum dropdown

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Package"))
        {
            PackageData package = other.GetComponent<PackageData>();

            if (package == null) return;

            Debug.Log("Package detected at " + houseAddress);

            // Compare addresses
            if (package.address == houseAddress)
            {
                package.status = PackageStatus.Delivered;

                Debug.Log($"✔ ORDER COMPLETED: Package {package.packageID} delivered to {houseAddress}");
                UIcontroller.ActivateOrderCompleteText();
                StartCoroutine(DestroyPackageAfterDelay(other.gameObject));
            }
            else
            {
                Debug.Log($"❌ WRONG HOUSE! Package belongs to {package.address}, not {houseAddress}");
            }

            // ⏱ Destroy package after 3 seconds (no other logic changed)

        }
    }

    private IEnumerator DestroyPackageAfterDelay(GameObject packageObj)
    {
        // Random delay between 5 and 20 seconds
        float delay = Random.Range(5f, 20f);
        yield return new WaitForSeconds(delay);

        Destroy(packageObj);
    }

}
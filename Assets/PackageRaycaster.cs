using UnityEngine;

public class PackageRaycaster : MonoBehaviour
{
    public Camera playerCamera;
    public float rayDistance = 2f;

    public bool isPackagePickedUp = false;

    private Transform currentPackage;   // store the detected package

    void Update()
    {
        RaycastHit hit;

        // Raycast from camera forward
        if (Physics.Raycast(playerCamera.transform.position,
                            playerCamera.transform.forward,
                            out hit, rayDistance))
        {
            // Check hit object tag
            if (hit.collider.CompareTag("Package"))
            {
                Debug.Log("RayCast on Package");

                // Save the package we are looking at
                currentPackage = hit.collider.transform;

                CheckPickup();
            }
            else
            {
                // Not looking at package anymore
                currentPackage = null;
            }
        }
        else
        {
            currentPackage = null;
        }
    }

    // Draw ray in Scene + Play mode
    void OnDrawGizmos()
    {
        if (playerCamera == null) return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(playerCamera.transform.position,
                       playerCamera.transform.forward * rayDistance);
    }

    void CheckPickup()
    {
        if (Input.GetKeyDown(KeyCode.E) && currentPackage != null)
        {
            Debug.Log("Package Picked Up: " + currentPackage.name);
            isPackagePickedUp = true;

            // Destroy or pick-up logic goes here
            // Example: Destroy(currentPackage.gameObject);
        }
    }
}

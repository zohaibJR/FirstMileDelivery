using UnityEngine;

public class PackageAreaCounter : MonoBehaviour
{
    public int maxPackages = 4;
    public int currentPackages = 0;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Package"))
        {

            currentPackages++;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Package"))
        {
            currentPackages--;
        }
    }

    public bool CanSpawn()
    {
        return currentPackages < maxPackages;
    }
}

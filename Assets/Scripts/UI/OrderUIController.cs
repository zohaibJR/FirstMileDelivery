using UnityEngine;
using System.Collections; // Required for using IEnumerator

public class OrderUIController : MonoBehaviour
{
    public GameObject OrderCompleteText;    // Start is called once before the first execution of Update after the MonoBehaviour is created

    public void ActivateOrderCompleteText()
    {
        OrderCompleteText.SetActive(true);
        StartCoroutine(DeactiveOrderCompleteText());
    }

    IEnumerator DeactiveOrderCompleteText()
    {
        yield return new WaitForSeconds(2f);
        OrderCompleteText.SetActive(false);

    }
}

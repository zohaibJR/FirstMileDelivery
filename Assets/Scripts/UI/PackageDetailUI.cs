using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PackageDetailUI : MonoBehaviour
{
    public GameObject packageDetailPanel;
    public TMP_Text packageIDText;
    public TMP_Text packageAddressText;
    public TMP_Text customerNameText;

    private void Start()
    {
        packageDetailPanel.SetActive(false);
    }

    public void ActivatePanel(PackageData DataObj)
    {
        packageDetailPanel.SetActive(true);
        packageIDText.text = DataObj.packageID.ToString();
        packageAddressText.text = DataObj.address.ToString();
        customerNameText.text = DataObj.customerName.ToString();
    }

    public void DectivatePanel()
    {
        packageDetailPanel.SetActive(false);
    }

}

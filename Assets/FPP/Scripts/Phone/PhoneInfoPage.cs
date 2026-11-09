using System.Text;
using TMPro;
using UnityEngine;

/// <summary>
/// Fills the phone's Info page: money, energy, debt and upcoming bills.
/// Put this on the FPP Player (next to SmartphoneUI). It refreshes while the phone is open.
/// </summary>
public sealed class PhoneInfoPage : MonoBehaviour
{
    [Tooltip("The text on Page_Info that shows the information.")]
    [SerializeField] private TextMeshProUGUI infoText;

    private SmartphoneUI phone;
    private string lastText;

    private void Start()
    {
        phone = SmartphoneUI.Instance;

        if (phone != null)
            phone.Opened += Refresh;
        else
            Debug.LogWarning("PhoneInfoPage: no SmartphoneUI in the scene.", this);

        Refresh();
    }

    private void OnDestroy()
    {
        if (phone != null)
            phone.Opened -= Refresh;
    }

    private void Update()
    {
        // Energy and the clock change all the time, so refresh while the phone is open.
        if (phone != null && phone.IsOpen)
            Refresh();
    }

    private void Refresh()
    {
        if (infoText == null)
            return;

        StringBuilder sb = new StringBuilder();

        EconomyManager economy = EconomyManager.Instance;
        if (economy != null)
            sb.AppendLine($"<b>Money</b>   <color=#8CFFA6>${economy.Balance:N0}</color>");

        PlayerEnergy energy = PlayerEnergy.Instance;
        if (energy != null)
            sb.AppendLine($"<b>Energy</b>   {Mathf.RoundToInt(energy.CurrentEnergy)}%");

        BillsManager bills = BillsManager.Instance;
        if (bills != null)
        {
            sb.AppendLine();

            if (bills.Debt > 0)
                sb.AppendLine($"<b>Debt</b>   <color=#FF8A80>${bills.Debt:N0}</color>");
            else
                sb.AppendLine("<b>Debt</b>   none");

            sb.AppendLine();

            int days = bills.DaysUntilNextBill;
            string when = days == 1 ? "tomorrow" : $"in {days} days";
            sb.AppendLine($"<b>Next bills</b>   Day {bills.NextBillDay} ({when})");
            sb.AppendLine($"Rent   ${bills.WeeklyRent}");
            sb.AppendLine($"Electricity   ${bills.WeeklyElectricity}");

            if (bills.VehicleRental > 0)
                sb.AppendLine($"Vehicle rental   ${bills.VehicleRental}");

            sb.AppendLine($"<b>Total   ${bills.WeeklyTotal}</b>");
        }

        string text = sb.ToString();
        if (text == lastText)
            return;

        lastText = text;
        infoText.text = text;
    }
}
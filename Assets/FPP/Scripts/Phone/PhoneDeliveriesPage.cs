using System.Text;
using TMPro;
using UnityEngine;

/// <summary>
/// Fills the phone's Deliveries page with today's packages. Put this on the FPP Player (next to SmartphoneUI).
/// Updates when the phone opens and whenever the batch changes (collected, delivered, new day).
/// </summary>
public sealed class PhoneDeliveriesPage : MonoBehaviour
{
    [Tooltip("The text on Page_Deliveries that shows the list.")]
    [SerializeField] private TextMeshProUGUI listText;

    private DailyBatchManager batch;
    private SmartphoneUI phone;

    private void Start()
    {
        batch = DailyBatchManager.Instance;
        phone = SmartphoneUI.Instance;

        if (batch != null)
            batch.BatchChanged += Refresh;
        else
            Debug.LogWarning("PhoneDeliveriesPage: no DailyBatchManager in the scene.", this);

        if (phone != null)
            phone.Opened += Refresh;

        Refresh();
    }

    private void OnDestroy()
    {
        if (batch != null)
            batch.BatchChanged -= Refresh;

        if (phone != null)
            phone.Opened -= Refresh;
    }

    private void Refresh()
    {
        if (listText == null)
            return;

        if (batch == null || !batch.HasBatch)
        {
            listText.text = "No deliveries today";
            return;
        }

        StringBuilder sb = new StringBuilder();
        sb.AppendLine($"<b>Day {batch.Day}   {batch.RemainingCount} of {batch.TotalCount} left</b>");

        if (!batch.IsCollected)
            sb.AppendLine("<color=#FFD54A>Collect your packages at the warehouse</color>");

        sb.AppendLine();

        var assignments = batch.Assignments;
        for (int i = 0; i < assignments.Count; i++)
        {
            PackageAssignment a = assignments[i];
            string size = a.definition != null ? a.definition.Size.ToString() : "?";
            int reward = a.definition != null ? a.definition.Reward : 0;

            if (a.delivered)
                sb.AppendLine($"<color=#6B7785><s>{i + 1}. {a.address}</s>   Done</color>");
            else
                sb.AppendLine($"{i + 1}. {a.address}   <color=#9AA4B2>{size}  ${reward}</color>");
        }

        listText.text = sb.ToString();
    }
}
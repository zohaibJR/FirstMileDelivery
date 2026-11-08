using UnityEngine;

/// <summary>
/// A desk (or mailbox) at home where the player pays off debt. Needs a Collider on the same object.
/// Pays as much of the debt as the player can afford.
/// </summary>
public sealed class BillsDesk : MonoBehaviour, IInteractable
{
    public string InteractionPrompt
    {
        get
        {
            BillsManager bills = BillsManager.Instance;
            if (bills == null)
                return "Bills";

            if (bills.Debt <= 0)
                return $"No debt. Next bills Day {bills.NextBillDay}";

            return $"Pay debt (${bills.Debt})";
        }
    }

    public bool CanInteract(PlayerInteractor interactor)
    {
        return BillsManager.Instance != null;
    }

    public void Interact(PlayerInteractor interactor)
    {
        BillsManager bills = BillsManager.Instance;
        if (bills == null)
            return;

        if (bills.Debt <= 0)
        {
            Notifier.Show($"You have no debt. Next bills: ${bills.WeeklyTotal} on Day {bills.NextBillDay}");
            return;
        }

        int paid = bills.PayDebt();

        if (paid <= 0)
        {
            Notifier.Show("You don't have any money to pay your debt");
            GameAudio.Play(ProceduralSfx.Denied, transform.position, 0.7f);
            return;
        }

        if (bills.Debt > 0)
            Notifier.Show($"Paid ${paid} of your debt. ${bills.Debt} left");
        else
            Notifier.Show($"Debt fully paid   -${paid}");

        GameAudio.Play(ProceduralSfx.Click, transform.position, 0.8f);
    }
}
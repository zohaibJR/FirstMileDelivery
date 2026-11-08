using UnityEngine;

/// <summary>
/// The vehicle rental counter. Put this on the Counter object (needs a Collider).
/// Renting: needs no debt and enough money for the deposit. The deposit is paid now,
/// and the weekly fee is added to the weekly bills.
/// Returning: the deposit is given back and the weekly fee stops.
/// </summary>
public sealed class RentalOffice : MonoBehaviour, IInteractable
{
    [Header("Agreement")]
    [SerializeField] private string vehicleName = "Delivery Van";
    [SerializeField, Min(0)] private int deposit = 100;
    [SerializeField, Min(0)] private int weeklyFee = 40;

    [Header("Vehicle")]
    [Tooltip("Where the vehicle appears (the ParkingSpot). Optional until the vehicle exists.")]
    [SerializeField] private Transform parkingSpot;
    [Tooltip("The vehicle prefab. Leave empty until the vehicle is built.")]
    [SerializeField] private GameObject vehiclePrefab;

    private GameObject spawnedVehicle;

    public bool IsRented { get; private set; }
    public GameObject SpawnedVehicle => spawnedVehicle;

    public string InteractionPrompt
    {
        get
        {
            if (IsRented)
                return $"Return {vehicleName} (get ${deposit} deposit back)";

            return $"Rent {vehicleName} (${deposit} deposit, ${weeklyFee}/week)";
        }
    }

    public bool CanInteract(PlayerInteractor interactor)
    {
        return EconomyManager.Instance != null && BillsManager.Instance != null;
    }

    public void Interact(PlayerInteractor interactor)
    {
        if (IsRented)
            ReturnVehicle();
        else
            RentVehicle();
    }

    private void RentVehicle()
    {
        EconomyManager economy = EconomyManager.Instance;
        BillsManager bills = BillsManager.Instance;

        if (bills.Debt > 0)
        {
            Notifier.Show("Pay your debt first. We don't rent to people with debt");
            GameAudio.Play(ProceduralSfx.Denied, transform.position, 0.7f);
            return;
        }

        if (!economy.TrySpend(deposit, $"{vehicleName} deposit"))
        {
            Notifier.Show($"You need ${deposit} for the deposit");
            GameAudio.Play(ProceduralSfx.Denied, transform.position, 0.7f);
            return;
        }

        IsRented = true;
        bills.SetVehicleRental(weeklyFee);

        if (vehiclePrefab != null)
        {
            Vector3 position = parkingSpot != null ? parkingSpot.position : transform.position + transform.forward * 4f;
            Quaternion rotation = parkingSpot != null ? parkingSpot.rotation : transform.rotation;
            spawnedVehicle = Instantiate(vehiclePrefab, position, rotation);
        }

        Notifier.Show($"{vehicleName} rented   Deposit -${deposit}");
        Notifier.Show($"${weeklyFee} per week will be added to your bills");
        GameAudio.Play(ProceduralSfx.Unlock, transform.position, 0.8f);
    }

    private void ReturnVehicle()
    {
        EconomyManager economy = EconomyManager.Instance;
        BillsManager bills = BillsManager.Instance;

        if (spawnedVehicle != null)
            Destroy(spawnedVehicle);

        spawnedVehicle = null;
        IsRented = false;
        bills.SetVehicleRental(0);
        economy.Earn(deposit, $"{vehicleName} deposit refund");

        Notifier.Show($"{vehicleName} returned   Deposit +${deposit}");
        GameAudio.Play(ProceduralSfx.Click, transform.position, 0.8f);
    }
}
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Interactable that needs the interact button HELD (a progress ring fills).
/// Good for opening a chest, hacking a panel, repairing something, etc.
/// </summary>
public class HoldInteractable : MonoBehaviour, IHoldInteractable
{
    [SerializeField] private string prompt = "Use";
    [SerializeField, Min(0.1f)] private float holdDuration = 1.5f;
    [SerializeField] private bool singleUse = false;
    [SerializeField] private UnityEvent onComplete;

    private bool used;

    public string InteractionPrompt => prompt;
    public float HoldDuration => holdDuration;

    public bool CanInteract(PlayerInteractor interactor) => !(singleUse && used);

    public void Interact(PlayerInteractor interactor)
    {
        used = true;
        onComplete?.Invoke();
    }
}
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Generic interactable. Add it to any object with a Collider and hook up
/// the "On Interact" event in the Inspector (turn on a light, play a sound, open UI...).
/// </summary>
public class InteractableObject : MonoBehaviour, IInteractable
{
    [SerializeField] private string prompt = "Interact";
    [SerializeField] private bool singleUse = false;
    [SerializeField, Min(0f)] private float cooldown = 0.25f;
    [SerializeField] private UnityEvent onInteract;

    private bool used;
    private float nextAllowedTime;

    public string InteractionPrompt => prompt;

    public bool CanInteract(PlayerInteractor interactor)
    {
        return !(singleUse && used);
    }

    public void Interact(PlayerInteractor interactor)
    {
        if (Time.time < nextAllowedTime)
            return;

        nextAllowedTime = Time.time + cooldown;
        used = true;
        onInteract?.Invoke();
    }

    public void SetPrompt(string newPrompt) => prompt = newPrompt;
}
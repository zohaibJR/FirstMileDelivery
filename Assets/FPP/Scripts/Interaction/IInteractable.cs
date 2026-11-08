/// <summary>
/// Implement this on anything the player can look at and interact with.
/// The PlayerInteractor finds it with a raycast.
/// </summary>
public interface IInteractable
{
    /// <summary>Text shown on screen while the player looks at this object.</summary>
    string InteractionPrompt { get; }

    /// <summary>Return false to hide the prompt and block interaction (locked, used up, etc).</summary>
    bool CanInteract(PlayerInteractor interactor);

    /// <summary>Called when the player presses the interact button.</summary>
    void Interact(PlayerInteractor interactor);
}

/// <summary>
/// An interactable that must be HELD down (a progress ring fills) before Interact() fires.
/// </summary>
public interface IHoldInteractable : IInteractable
{
    /// <summary>Seconds the player must hold the interact button.</summary>
    float HoldDuration { get; }
}
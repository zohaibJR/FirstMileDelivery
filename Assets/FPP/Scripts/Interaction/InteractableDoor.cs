using NUnit.Framework.Interfaces;
using UnityEngine;

/// <summary>
/// A door that swings open and closed. Put this on the door's pivot object (the hinge)
/// and give the door mesh a Collider.
/// Set "Locked" and assign a "Required Key" (an ItemData) for a key and lock puzzle.
/// </summary>
public class InteractableDoor : MonoBehaviour, IInteractable
{
    [Header("Motion")]
    [SerializeField] private float openAngle = 90f;
    [SerializeField, Min(0.1f)] private float speed = 4f;

    [Header("Lock")]
    [SerializeField] private bool locked = false;
    [Tooltip("Item the player needs in the inventory to unlock. Leave empty for a door that can't be unlocked by a key.")]
    [SerializeField] private ItemData requiredKey;
    [SerializeField] private bool consumeKey = false;

    private Quaternion closedRotation;
    private Quaternion openRotation;
    private bool isOpen;

    public string InteractionPrompt
    {
        get
        {
            if (locked)
                return requiredKey != null ? $"Locked - needs {requiredKey.DisplayName}" : "Locked";
            return isOpen ? "Close Door" : "Open Door";
        }
    }

    private void Awake()
    {
        closedRotation = transform.localRotation;
        openRotation = closedRotation * Quaternion.Euler(0f, openAngle, 0f);
    }

    private void Update()
    {
        Quaternion target = isOpen ? openRotation : closedRotation;
        transform.localRotation = Quaternion.Slerp(transform.localRotation, target, speed * Time.deltaTime);
    }

    public bool CanInteract(PlayerInteractor interactor) => true;

    public void Interact(PlayerInteractor interactor)
    {
        if (locked)
        {
            if (!TryUnlock(interactor))
            {
                Notifier.Show(requiredKey != null ? $"Locked. You need: {requiredKey.DisplayName}" : "It's locked.");
                GameAudio.Play(ProceduralSfx.Denied, transform.position, 0.7f);
                return;
            }
        }

        isOpen = !isOpen;
        GameAudio.Play(ProceduralSfx.Thud, transform.position, 0.5f, 0.1f);
    }

    private bool TryUnlock(PlayerInteractor interactor)
    {
        if (requiredKey == null)
            return false;

        Inventory inventory = interactor.GetComponent<Inventory>();
        if (inventory == null || !inventory.Has(requiredKey))
            return false;

        if (consumeKey)
            inventory.Remove(requiredKey, 1);

        locked = false;
        Notifier.Show("Unlocked");
        GameAudio.Play(ProceduralSfx.Unlock, transform.position, 0.8f);
        return true;
    }

    public void Unlock() => locked = false;
    public void Lock() => locked = true;
}
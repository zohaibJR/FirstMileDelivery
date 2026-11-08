using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Lets several systems (inspect mode, inventory...) freeze the player safely.
/// The FppPlayerController is only re-enabled when every system has released its lock.
/// Added automatically at runtime, you never need to add it by hand.
/// </summary>
public sealed class PlayerInputLock : MonoBehaviour
{
    private readonly HashSet<object> owners = new HashSet<object>();
    private FppPlayerController controller;

    public bool IsLocked => owners.Count > 0;

    /// <summary>Freezing while airborne would leave the player floating, so it is refused.</summary>
    public bool CanLock => controller == null || controller.IsGrounded;

    public static PlayerInputLock Get(GameObject player)
    {
        PlayerInputLock inputLock = player.GetComponent<PlayerInputLock>();
        if (inputLock == null)
            inputLock = player.AddComponent<PlayerInputLock>();
        return inputLock;
    }

    private void Awake()
    {
        controller = GetComponent<FppPlayerController>();
    }

    public bool Lock(object owner)
    {
        if (owners.Contains(owner))
            return true;

        if (owners.Count == 0 && !CanLock)
            return false;

        owners.Add(owner);
        Apply();
        return true;
    }

    public void Unlock(object owner)
    {
        if (owners.Remove(owner))
            Apply();
    }

    public bool IsLockedBy(object owner) => owners.Contains(owner);

    private void Apply()
    {
        if (controller != null)
            controller.enabled = owners.Count == 0;
    }

    private void OnDestroy()
    {
        owners.Clear();
    }
}
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Lets several systems (inspect mode, day transition...) freeze the player safely.
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

    /// <summary>
    /// Freezes the player for "owner" (pass "this"). Returns false if airborne.
    /// Call Unlock(this) when done.
    /// </summary>
    public bool Lock(object owner)
    {
        if (owner == null)
            return false;

        if (!CanLock)
            return false;

        if (owners.Add(owner) && owners.Count == 1)
            Apply(true);

        return true;
    }

    public void Unlock(object owner)
    {
        if (owner == null)
            return;

        if (owners.Remove(owner) && owners.Count == 0)
            Apply(false);
    }

    private void Apply(bool locked)
    {
        if (controller != null)
            controller.enabled = !locked;
    }
}

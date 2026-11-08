using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Put this on the FPP Player. Holds one PickupItem in front of the camera and lets the player
/// inspect it (freeze the view, rotate with the mouse, zoom with the wheel), drop it, throw it,
/// or store it in the inventory. Hints are drawn by InteractionHud.
/// </summary>
public sealed class ItemCarrier : MonoBehaviour
{
    [Header("References (auto-found if empty)")]
    [SerializeField] private Camera viewCamera;
    [SerializeField] private PlayerInteractor interactor;
    [SerializeField] private Inventory inventory;

    [Header("Carry Position (camera space)")]
    [SerializeField, Min(0.3f)] private float holdDistance = 1.1f;
    [SerializeField] private float holdHeight = -0.2f;
    [SerializeField] private float holdSide = 0.15f;
    [Tooltip("Stops the item from going through walls by pulling it closer.")]
    [SerializeField] private LayerMask obstacleMask = ~0;
    [SerializeField, Min(0.1f)] private float minHoldDistance = 0.5f;
    [SerializeField, Min(0.05f)] private float itemClearance = 0.35f;
    [SerializeField] private bool resetRotationOnPickup = true;

    [Header("Inspect")]
    [Tooltip("Off: press to toggle inspect. On: inspect only while the button is held (like many AAA games).")]
    [SerializeField] private bool holdToInspect = false;
    [SerializeField, Min(0.2f)] private float inspectDistance = 0.8f;
    [SerializeField, Min(0.2f)] private float inspectMinDistance = 0.5f;
    [SerializeField, Min(0.2f)] private float inspectMaxDistance = 1.5f;
    [SerializeField, Min(0.01f)] private float inspectRotateSpeed = 0.35f;
    [SerializeField, Min(1f)] private float gamepadRotateSpeed = 180f;
    [SerializeField, Min(0.0001f)] private float zoomSpeed = 0.002f;

    [Header("Smoothing")]
    [SerializeField, Min(1f)] private float positionFollow = 14f;
    [SerializeField, Min(1f)] private float rotationFollow = 22f;

    [Header("Input (Right mouse button also inspects)")]
    [SerializeField] private Key inspectKey = Key.F;
    [SerializeField] private Key dropKey = Key.Q;
    [SerializeField] private Key storeKey = Key.R;
    [SerializeField] private bool leftMouseThrows = true;
    [SerializeField, Min(0f)] private float throwSpeed = 9f;

    [Header("Audio (empty = generated placeholder sounds)")]
    [SerializeField] private AudioClip pickupClip;
    [SerializeField] private AudioClip throwClip;
    [SerializeField] private bool useProceduralSounds = true;
    [SerializeField, Range(0f, 1f)] private float sfxVolume = 0.8f;

    private PickupItem held;
    private bool inspecting;
    private Quaternion relativeRotation = Quaternion.identity;
    private float currentInspectDistance;
    private PlayerInputLock inputLock;

    public bool IsHolding => held != null;
    public bool IsInspecting => inspecting;
    public PickupItem HeldItem => held;

    public Key InspectKey => inspectKey;
    public Key DropKey => dropKey;
    public Key StoreKey => storeKey;
    public bool HoldToInspect => holdToInspect;
    public bool LeftMouseThrows => leftMouseThrows;

    public bool CanStoreHeld =>
        held != null && held.Data != null && held.Data.CanStoreWhenCarried && inventory != null;

    private void Awake()
    {
        if (viewCamera == null) viewCamera = GetComponentInChildren<Camera>(true);
        if (viewCamera == null) viewCamera = Camera.main;
        if (interactor == null) interactor = GetComponent<PlayerInteractor>();
        if (inventory == null) inventory = GetComponent<Inventory>();

        if (viewCamera == null)
            Debug.LogError("ItemCarrier: no camera found. Assign View Camera.", this);

        inputLock = PlayerInputLock.Get(gameObject);

        // Never let the obstacle check hit the player or the Ignore Raycast layer.
        obstacleMask &= ~(1 << gameObject.layer);
        obstacleMask &= ~(1 << 2);

        currentInspectDistance = inspectDistance;
    }

    private void OnDisable()
    {
        if (held != null)
            Release(Vector3.zero);
    }

    /// <summary>Called by PickupItem. Returns false if the hands are already full.</summary>
    public bool Pick(PickupItem item)
    {
        if (item == null || held != null || viewCamera == null)
            return false;

        held = item;
        held.OnPickedUp();

        relativeRotation = resetRotationOnPickup
            ? Quaternion.identity
            : Quaternion.Inverse(viewCamera.transform.rotation) * held.transform.rotation;

        currentInspectDistance = inspectDistance;

        PlaySound(pickupClip, ProceduralSfx.Pickup, 1f);
        return true;
    }

    private void Update()
    {
        if (held == null || viewCamera == null)
            return;

        // Another system (inventory) owns the player right now.
        if (inputLock.IsLocked && !inspecting)
            return;

        if (Cursor.lockState != CursorLockMode.Locked)
            return;

        Keyboard keyboard = Keyboard.current;
        Mouse mouse = Mouse.current;
        Gamepad gamepad = Gamepad.current;

        bool inspectPressed =
            (keyboard != null && keyboard[inspectKey].wasPressedThisFrame) ||
            (mouse != null && mouse.rightButton.wasPressedThisFrame) ||
            (gamepad != null && gamepad.buttonNorth.wasPressedThisFrame);

        bool inspectHeld =
            (keyboard != null && keyboard[inspectKey].isPressed) ||
            (mouse != null && mouse.rightButton.isPressed) ||
            (gamepad != null && gamepad.buttonNorth.isPressed);

        bool dropPressed =
            (keyboard != null && keyboard[dropKey].wasPressedThisFrame) ||
            (gamepad != null && gamepad.buttonEast.wasPressedThisFrame);

        bool storePressed =
            (keyboard != null && keyboard[storeKey].wasPressedThisFrame) ||
            (gamepad != null && gamepad.leftShoulder.wasPressedThisFrame);

        bool throwPressed =
            leftMouseThrows &&
            ((mouse != null && mouse.leftButton.wasPressedThisFrame) ||
             (gamepad != null && gamepad.rightShoulder.wasPressedThisFrame));

        bool escapePressed = keyboard != null && keyboard.escapeKey.wasPressedThisFrame;

        if (inspecting)
        {
            bool exit = escapePressed || (holdToInspect ? !inspectHeld : inspectPressed);
            if (exit)
            {
                SetInspecting(false);
                return;
            }

            if (dropPressed)
            {
                Release(Vector3.zero);
                return;
            }

            if (storePressed)
            {
                StoreHeld();
                return;
            }

            UpdateInspectRotation(mouse, gamepad);
            return;
        }

        if (holdToInspect ? inspectHeld : inspectPressed)
            SetInspecting(true);
        else if (storePressed)
            StoreHeld();
        else if (dropPressed)
            Release(Vector3.zero);
        else if (throwPressed)
            Release(viewCamera.transform.forward * throwSpeed);
    }

    private void UpdateInspectRotation(Mouse mouse, Gamepad gamepad)
    {
        Vector2 delta = Vector2.zero;

        if (mouse != null)
        {
            delta += mouse.delta.ReadValue() * inspectRotateSpeed;
            currentInspectDistance = Mathf.Clamp(
                currentInspectDistance - mouse.scroll.ReadValue().y * zoomSpeed,
                inspectMinDistance, inspectMaxDistance);
        }

        if (gamepad != null)
            delta += gamepad.rightStick.ReadValue() * gamepadRotateSpeed * Time.unscaledDeltaTime;

        if (delta.sqrMagnitude < 0.00001f)
            return;

        // Rotate around the camera's up and right axes so the drag always feels natural.
        Quaternion yaw = Quaternion.AngleAxis(-delta.x, Vector3.up);
        Quaternion pitch = Quaternion.AngleAxis(delta.y, Vector3.right);
        relativeRotation = yaw * pitch * relativeRotation;
    }

    private void LateUpdate()
    {
        if (held == null || viewCamera == null)
            return;

        Transform cam = viewCamera.transform;
        Vector3 target;

        if (inspecting)
        {
            target = cam.position + cam.forward * currentInspectDistance;
        }
        else
        {
            float distance = GetCarryDistance(cam);
            target = cam.position
                + cam.forward * distance
                + cam.up * holdHeight
                + cam.right * holdSide;
        }

        float posT = 1f - Mathf.Exp(-positionFollow * Time.unscaledDeltaTime);
        float rotT = 1f - Mathf.Exp(-rotationFollow * Time.unscaledDeltaTime);

        Transform item = held.transform;
        item.position = Vector3.Lerp(item.position, target, posT);
        item.rotation = Quaternion.Slerp(item.rotation, cam.rotation * relativeRotation, rotT);
    }

    private float GetCarryDistance(Transform cam)
    {
        if (Physics.Raycast(cam.position, cam.forward, out RaycastHit hit,
                holdDistance + itemClearance, obstacleMask, QueryTriggerInteraction.Ignore))
        {
            return Mathf.Clamp(hit.distance - itemClearance, minHoldDistance, holdDistance);
        }

        return holdDistance;
    }

    private void SetInspecting(bool value)
    {
        if (value == inspecting)
            return;

        if (value)
        {
            // Refused while airborne (would leave the player floating).
            if (!inputLock.Lock(this))
                return;
        }
        else
        {
            inputLock.Unlock(this);
        }

        inspecting = value;
    }

    private void StoreHeld()
    {
        if (held == null)
            return;

        if (!CanStoreHeld)
        {
            Notifier.Show("Can't store this item");
            return;
        }

        PickupItem item = held;
        ItemData data = item.Data;

        if (inventory.Add(data, 1) <= 0)
        {
            Notifier.Show("Inventory full");
            GameAudio.Play(ProceduralSfx.Denied, transform.position, 0.6f, 0f, 0f);
            return;
        }

        SetInspecting(false);
        held = null;

        Notifier.Show($"Stored {data.DisplayName}");
        PlaySound(null, ProceduralSfx.Pickup, 1f);
        Destroy(item.gameObject);
    }

    private void Release(Vector3 throwVelocity)
    {
        if (held == null)
            return;

        SetInspecting(false);

        held.OnReleased(throwVelocity);
        held = null;

        if (throwVelocity.sqrMagnitude > 0.01f)
            PlaySound(throwClip, ProceduralSfx.Whoosh, 1f);
    }

    /// <summary>
    /// Takes the held item out of the player's hands WITHOUT touching its physics.
    /// The caller decides what happens next (deliver, place, destroy...).
    /// </summary>
    public PickupItem DetachHeld()
    {
        if (held == null)
            return null;

        SetInspecting(false);

        PickupItem item = held;
        held = null;
        return item;
    }

    private void PlaySound(AudioClip custom, AudioClip fallback, float volumeScale)
    {
        AudioClip clip = GameAudio.Or(custom, useProceduralSounds ? fallback : null);
        Vector3 position = viewCamera != null ? viewCamera.transform.position : transform.position;
        GameAudio.Play(clip, position, sfxVolume * volumeScale, 0.06f, 0f);
    }
}
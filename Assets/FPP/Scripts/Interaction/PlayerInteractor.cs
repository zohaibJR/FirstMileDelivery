using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// First person raycast interaction. Put this on the FPP Player.
/// Every frame it casts a ray from the center of the camera, finds an IInteractable,
/// highlights it, and fires Interact() on key press (or after a hold for IHoldInteractable).
/// The on-screen prompt is drawn by InteractionHud.
/// </summary>
public sealed class PlayerInteractor : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Camera the ray is fired from. Auto-found in children if empty.")]
    [SerializeField] private Camera viewCamera;

    [Header("Raycast")]
    [SerializeField, Min(0.5f)] private float interactDistance = 3f;
    [Tooltip("Radius of the cast. 0 = thin ray, 0.05 - 0.15 is more forgiving on small objects.")]
    [SerializeField, Range(0f, 0.5f)] private float castRadius = 0.05f;
    [SerializeField] private LayerMask interactMask = ~0;
    [SerializeField] private QueryTriggerInteraction triggerInteraction = QueryTriggerInteraction.Ignore;

    [Header("Input")]
    [SerializeField] private Key interactKey = Key.E;
    [SerializeField] private bool allowGamepad = true;

    [Header("Highlight")]
    [SerializeField] private bool autoHighlight = true;

    [Header("Debug")]
    [SerializeField] private bool drawDebugRay = false;

    private IInteractable currentTarget;
    private Component currentTargetComponent;
    private InteractableHighlight currentHighlight;
    private PlayerInputLock inputLock;
    private float holdTimer;
    private bool waitForRelease;

    public event Action<IInteractable> TargetChanged;

    public IInteractable CurrentTarget => currentTarget;
    public bool HasTarget => currentTarget != null;
    public Key InteractKey => interactKey;

    /// <summary>Set to true by other systems (e.g. ItemCarrier) to pause interaction.</summary>
    public bool Blocked { get; set; }

    public bool TargetIsHold => currentTarget is IHoldInteractable hold && hold.HoldDuration > 0f;

    /// <summary>0 to 1 progress while holding the interact button on a hold-interactable.</summary>
    public float HoldProgress { get; private set; }

    private void Awake()
    {
        if (viewCamera == null)
            viewCamera = GetComponentInChildren<Camera>(true);

        if (viewCamera == null)
            viewCamera = Camera.main;

        if (viewCamera == null)
            Debug.LogError("PlayerInteractor: no camera found. Assign View Camera.", this);

        inputLock = PlayerInputLock.Get(gameObject);

        // Never hit ourselves, even if the player is not on the Ignore Raycast layer.
        interactMask &= ~(1 << gameObject.layer);
    }

    private void OnDisable()
    {
        SetTarget(null);
    }

    private void Update()
    {
        if (viewCamera == null)
            return;

        bool paused = Blocked
            || inputLock.IsLocked
            || Cursor.lockState != CursorLockMode.Locked;

        if (paused)
        {
            SetTarget(null);
            return;
        }

        FindTarget();
        HandleInput();
    }

    private void FindTarget()
    {
        Transform camT = viewCamera.transform;
        Ray ray = new Ray(camT.position, camT.forward);

        if (drawDebugRay)
            Debug.DrawRay(ray.origin, ray.direction * interactDistance, Color.green);

        bool hitSomething = castRadius > 0.001f
            ? Physics.SphereCast(ray, castRadius, out RaycastHit hit, interactDistance, interactMask, triggerInteraction)
            : Physics.Raycast(ray, out hit, interactDistance, interactMask, triggerInteraction);

        if (!hitSomething)
        {
            SetTarget(null);
            return;
        }

        // Collider may be on a child, so look upward.
        IInteractable found = hit.collider.GetComponentInParent<IInteractable>();
        SetTarget(found != null && found.CanInteract(this) ? found : null);
    }

    private void HandleInput()
    {
        if (currentTarget == null)
        {
            holdTimer = 0f;
            HoldProgress = 0f;
            return;
        }

        if (currentTarget is IHoldInteractable hold && hold.HoldDuration > 0f)
        {
            if (waitForRelease)
            {
                if (!IsInteractHeld())
                    waitForRelease = false;
                HoldProgress = 0f;
                return;
            }

            if (IsInteractHeld())
            {
                holdTimer += Time.deltaTime;
                HoldProgress = Mathf.Clamp01(holdTimer / hold.HoldDuration);

                if (HoldProgress >= 1f)
                {
                    IInteractable completed = currentTarget;
                    holdTimer = 0f;
                    HoldProgress = 0f;
                    waitForRelease = true;
                    completed.Interact(this);
                }
            }
            else
            {
                holdTimer = 0f;
                HoldProgress = 0f;
            }

            return;
        }

        if (WasInteractPressed())
            currentTarget.Interact(this);
    }

    private void SetTarget(IInteractable newTarget)
    {
        if (ReferenceEquals(newTarget, currentTarget))
            return;

        if (currentHighlight != null)
            currentHighlight.SetHighlighted(false);
        currentHighlight = null;

        currentTarget = newTarget;
        currentTargetComponent = newTarget as Component;
        holdTimer = 0f;
        HoldProgress = 0f;
        waitForRelease = newTarget != null && IsInteractHeld();

        if (newTarget != null && autoHighlight && currentTargetComponent != null)
        {
            InteractableHighlight highlight = currentTargetComponent.GetComponent<InteractableHighlight>();
            if (highlight == null)
                highlight = currentTargetComponent.gameObject.AddComponent<InteractableHighlight>();

            highlight.SetHighlighted(true);
            currentHighlight = highlight;
        }

        TargetChanged?.Invoke(newTarget);
    }

    private bool WasInteractPressed()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && keyboard[interactKey].wasPressedThisFrame)
            return true;

        if (allowGamepad)
        {
            Gamepad gamepad = Gamepad.current;
            if (gamepad != null && gamepad.buttonWest.wasPressedThisFrame)
                return true;
        }

        return false;
    }

    private bool IsInteractHeld()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && keyboard[interactKey].isPressed)
            return true;

        if (allowGamepad)
        {
            Gamepad gamepad = Gamepad.current;
            if (gamepad != null && gamepad.buttonWest.isPressed)
                return true;
        }

        return false;
    }
}
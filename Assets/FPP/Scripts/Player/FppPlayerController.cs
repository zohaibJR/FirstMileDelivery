using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public sealed class FppPlayerController : MonoBehaviour
{
    [Header("Feature Toggles")]
    [SerializeField] private bool enableSprinting = true;
    [SerializeField] private bool enableJump = true;
    [SerializeField] private bool enableCrouch = true;

    [Header("Movement")]
    [SerializeField, Min(0.1f)] private float walkSpeed = 5f;
    [SerializeField, Min(0.1f)] private float sprintSpeed = 8f;
    [SerializeField, Min(0.1f)] private float crouchSpeed = 2.5f;
    [SerializeField, Min(0.1f)] private float acceleration = 24f;
    [SerializeField, Min(0.1f)] private float deceleration = 30f;
    [SerializeField, Range(0f, 1f)] private float airControl = 0.45f;

    [Header("Mouse Look")]
    [SerializeField] private Transform viewPivot;
    [SerializeField, Range(0.01f, 1f)] private float mouseSensitivity = 0.09f;
    [SerializeField, Min(1f)] private float gamepadLookSpeed = 150f;
    [SerializeField, Range(45f, 89f)] private float verticalLookLimit = 88f;
    [SerializeField] private bool lockCursorOnStart = true;

    [Header("Jump And Gravity")]
    [SerializeField, Min(0.1f)] private float jumpHeight = 1.25f;
    [SerializeField, Min(1f)] private float gravity = 24f;
    [SerializeField, Min(0.1f)] private float groundedForce = 2f;

    [Header("Crouch")]
    [SerializeField, Min(0.5f)] private float crouchingHeight = 1.2f;
    [SerializeField] private float crouchingViewHeight = 0.1f;
    [SerializeField, Min(0.1f)] private float crouchTransitionSpeed = 10f;
    [SerializeField] private LayerMask standingCollisionMask = ~0;

    private CharacterController characterController;
    private Vector3 horizontalVelocity;
    private float verticalVelocity;
    private float pitch;
    private float standingHeight;
    private Vector3 standingCenter;
    private Vector3 standingViewPosition;
    private float capsuleBottom;
    private bool isCrouched;

    public bool IsGrounded => characterController != null && characterController.isGrounded;
    public bool IsCrouched => isCrouched;
    public float CurrentSpeed => horizontalVelocity.magnitude;
    public Transform ViewPivot => viewPivot;
    public bool EnableSprinting
    {
        get => enableSprinting;
        set => enableSprinting = value;
    }

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        standingHeight = characterController.height;
        standingCenter = characterController.center;
        capsuleBottom = standingCenter.y - standingHeight * 0.5f;

        if (viewPivot == null)
        {
            Camera childCamera = GetComponentInChildren<Camera>(true);
            if (childCamera != null)
                viewPivot = childCamera.transform.parent != null
                    ? childCamera.transform.parent
                    : childCamera.transform;
        }

        standingViewPosition = viewPivot != null
            ? viewPivot.localPosition
            : Vector3.zero;
    }

    private void Start()
    {
        if (lockCursorOnStart)
            SetCursorLocked(true);
    }

    private void OnDisable()
    {
        horizontalVelocity = Vector3.zero;
        verticalVelocity = 0f;
    }

    private void Update()
    {
        HandleCursorState();
        HandleLook();
        HandleCrouch();
        HandleMovement();
    }

    private void HandleCursorState()
    {
        Keyboard keyboard = Keyboard.current;
        Mouse mouse = Mouse.current;

        if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
            SetCursorLocked(false);
        else if (mouse != null && mouse.leftButton.wasPressedThisFrame &&
                 Cursor.lockState != CursorLockMode.Locked)
            SetCursorLocked(true);
    }

    private void HandleLook()
    {
        if (viewPivot == null || Cursor.lockState != CursorLockMode.Locked)
            return;

        Vector2 lookDelta = Vector2.zero;
        Mouse mouse = Mouse.current;
        if (mouse != null)
            lookDelta += mouse.delta.ReadValue() * mouseSensitivity;

        Gamepad gamepad = Gamepad.current;
        if (gamepad != null)
            lookDelta += gamepad.rightStick.ReadValue() *
                gamepadLookSpeed * Time.unscaledDeltaTime;

        transform.Rotate(0f, lookDelta.x, 0f, Space.Self);
        pitch = Mathf.Clamp(pitch - lookDelta.y,
            -verticalLookLimit, verticalLookLimit);
        viewPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }

    private void HandleMovement()
    {
        Vector2 input = ReadMovementInput();
        bool grounded = characterController.isGrounded;
        bool sprintHeld = IsSprintHeld();
        float targetSpeed = isCrouched
            ? crouchSpeed
            : enableSprinting && sprintHeld ? sprintSpeed : walkSpeed;

        Vector3 desiredVelocity = transform.right * input.x +
            transform.forward * input.y;
        desiredVelocity = Vector3.ClampMagnitude(desiredVelocity, 1f) * targetSpeed;

        float control = grounded ? 1f : airControl;
        float rate = desiredVelocity.sqrMagnitude > 0.001f
            ? acceleration
            : deceleration;
        horizontalVelocity = Vector3.MoveTowards(
            horizontalVelocity, desiredVelocity,
            rate * control * Time.deltaTime);

        if (grounded && verticalVelocity < 0f)
            verticalVelocity = -groundedForce;

        if (enableJump && !isCrouched && grounded && WasJumpPressed())
            verticalVelocity = Mathf.Sqrt(2f * gravity * jumpHeight);

        verticalVelocity -= gravity * Time.deltaTime;
        Vector3 motion = horizontalVelocity + Vector3.up * verticalVelocity;
        characterController.Move(motion * Time.deltaTime);
    }

    private void HandleCrouch()
    {
        bool wantsToCrouch = enableCrouch && IsCrouchHeld();
        if (!wantsToCrouch && isCrouched && !CanStand())
            wantsToCrouch = true;

        isCrouched = wantsToCrouch;
        float targetHeight = isCrouched
            ? Mathf.Clamp(crouchingHeight,
                characterController.radius * 2f, standingHeight)
            : standingHeight;

        characterController.height = Mathf.MoveTowards(
            characterController.height, targetHeight,
            crouchTransitionSpeed * Time.deltaTime);

        Vector3 center = standingCenter;
        center.y = capsuleBottom + characterController.height * 0.5f;
        characterController.center = center;

        if (viewPivot == null)
            return;

        Vector3 targetViewPosition = standingViewPosition;
        if (isCrouched)
            targetViewPosition.y = crouchingViewHeight;
        viewPivot.localPosition = Vector3.MoveTowards(
            viewPivot.localPosition, targetViewPosition,
            crouchTransitionSpeed * Time.deltaTime);
    }

    private bool CanStand()
    {
        float radius = Mathf.Max(0.01f,
            characterController.radius - characterController.skinWidth);
        Vector3 bottom = transform.TransformPoint(new Vector3(
            standingCenter.x,
            capsuleBottom + radius,
            standingCenter.z));
        Vector3 top = transform.TransformPoint(new Vector3(
            standingCenter.x,
            capsuleBottom + standingHeight - radius,
            standingCenter.z));
        int mask = standingCollisionMask & ~(1 << gameObject.layer);

        return !Physics.CheckCapsule(
            bottom, top, radius, mask, QueryTriggerInteraction.Ignore);
    }

    private static Vector2 ReadMovementInput()
    {
        Vector2 input = Vector2.zero;
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null)
        {
            input.x = (keyboard.dKey.isPressed ? 1f : 0f) -
                (keyboard.aKey.isPressed ? 1f : 0f);
            input.y = (keyboard.wKey.isPressed ? 1f : 0f) -
                (keyboard.sKey.isPressed ? 1f : 0f);
        }

        Gamepad gamepad = Gamepad.current;
        if (gamepad != null)
            input += gamepad.leftStick.ReadValue();

        return Vector2.ClampMagnitude(input, 1f);
    }

    private static bool IsSprintHeld()
    {
        Keyboard keyboard = Keyboard.current;
        Gamepad gamepad = Gamepad.current;
        return keyboard != null && (keyboard.leftShiftKey.isPressed ||
               keyboard.rightShiftKey.isPressed) ||
               gamepad != null && gamepad.leftStickButton.isPressed;
    }

    private static bool WasJumpPressed()
    {
        Keyboard keyboard = Keyboard.current;
        Gamepad gamepad = Gamepad.current;
        return keyboard != null && keyboard.spaceKey.wasPressedThisFrame ||
               gamepad != null && gamepad.buttonSouth.wasPressedThisFrame;
    }

    private static bool IsCrouchHeld()
    {
        Keyboard keyboard = Keyboard.current;
        Gamepad gamepad = Gamepad.current;
        return keyboard != null && (keyboard.leftCtrlKey.isPressed ||
               keyboard.rightCtrlKey.isPressed || keyboard.cKey.isPressed) ||
               gamepad != null && gamepad.rightStickButton.isPressed;
    }

    private static void SetCursorLocked(bool locked)
    {
        Cursor.lockState = locked
            ? CursorLockMode.Locked
            : CursorLockMode.None;
        Cursor.visible = !locked;
    }
}

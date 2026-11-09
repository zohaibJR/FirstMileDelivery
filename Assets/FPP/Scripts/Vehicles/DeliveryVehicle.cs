using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// The rental van. Put this on the root of the DeliveryVan prefab (needs a Rigidbody).
/// - Look at it and press E to get in (hands must be empty).
/// - While driving: W/S throttle and brake/reverse, A/D steer, Space handbrake, E to get out.
/// Simple arcade driving: no wheel colliders, so it is easy to tune.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public sealed class DeliveryVehicle : MonoBehaviour, IInteractable
{
    [Header("Seats (found by name if empty)")]
    [SerializeField] private Transform driverCamera;
    [SerializeField] private Transform exitPoint;

    [Header("Driving")]
    [Tooltip("Metres per second. 16 is about 58 km/h.")]
    [SerializeField, Min(1f)] private float maxSpeed = 16f;
    [SerializeField, Min(1f)] private float maxReverseSpeed = 5f;
    [SerializeField, Min(0.5f)] private float acceleration = 6f;
    [SerializeField, Min(0.5f)] private float brakeStrength = 14f;
    [SerializeField, Min(0.1f)] private float coastDeceleration = 2.5f;
    [Tooltip("Degrees per second at full steering.")]
    [SerializeField, Min(10f)] private float steerSpeed = 70f;
    [Tooltip("How quickly sideways sliding is removed. Higher = more grip.")]
    [SerializeField, Min(0f)] private float sideGrip = 8f;
    [Tooltip("Low centre of mass keeps the van from tipping over.")]
    [SerializeField] private Vector3 centerOfMass = new Vector3(0f, 0.35f, 0f);

    private Rigidbody body;
    private Transform driverTransform;
    private CharacterController driverController;
    private PlayerInputLock inputLock;

    private float throttle;
    private float steer;
    private bool handbrake;
    private float enteredAt;

    public bool IsDriven => driverTransform != null;
    public float SpeedKmh => body != null ? body.linearVelocity.magnitude * 3.6f : 0f;

    public string InteractionPrompt => "Drive Delivery Van";

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
        body.centerOfMass = centerOfMass;

        if (driverCamera == null)
            driverCamera = transform.Find("DriverCamera");
        if (exitPoint == null)
            exitPoint = transform.Find("ExitPoint");

        // No friction on the van's own colliders, so it never sticks to the ground.
        PhysicsMaterial slippery = new PhysicsMaterial("VanSlippery")
        {
            dynamicFriction = 0f,
            staticFriction = 0f,
            bounciness = 0f,
            frictionCombine = PhysicsMaterialCombine.Minimum
        };

        foreach (Collider c in GetComponentsInChildren<Collider>())
            c.sharedMaterial = slippery;
    }

    public bool CanInteract(PlayerInteractor interactor)
    {
        return !IsDriven;
    }

    public void Interact(PlayerInteractor interactor)
    {
        if (IsDriven)
            return;

        ItemCarrier carrier = interactor.GetComponent<ItemCarrier>();
        if (carrier != null && carrier.IsHolding)
        {
            Notifier.Show("Put the package down first");
            GameAudio.Play(ProceduralSfx.Denied, transform.position, 0.7f);
            return;
        }

        Enter(interactor.gameObject);
    }

    private void Enter(GameObject player)
    {
        driverTransform = player.transform;
        driverController = player.GetComponent<CharacterController>();
        inputLock = player.GetComponent<PlayerInputLock>();
        if (inputLock == null)
            inputLock = player.AddComponent<PlayerInputLock>();

        // Remember where the camera sits relative to the player, so the camera lands on the driver point.
        Camera cam = Camera.main;
        Vector3 localCameraOffset = cam != null
            ? driverTransform.InverseTransformVector(driverTransform.position - cam.transform.position)
            : Vector3.zero;

        inputLock.Lock(this);
        if (driverController != null)
            driverController.enabled = false;

        driverTransform.SetParent(transform, true);
        driverTransform.rotation = transform.rotation;

        Vector3 seat = driverCamera != null ? driverCamera.position : transform.position + Vector3.up * 1.8f;
        driverTransform.position = seat + driverTransform.TransformVector(localCameraOffset);

        enteredAt = Time.time;

        Debug.Log($"VAN DEBUG  kinematic={body.isKinematic}  constraints={body.constraints}  mass={body.mass}  gravity={body.useGravity}  timeScale={Time.timeScale}  rigidbodiesInVan={GetComponentsInChildren<Rigidbody>().Length}  vanScale={transform.lossyScale}", this);

        foreach (Collider c in GetComponentsInChildren<Collider>())
            Debug.Log($"VAN DEBUG  van collider: {c.name}  enabled={c.enabled}  trigger={c.isTrigger}", c);

        foreach (Collider c in driverTransform.GetComponentsInChildren<Collider>())
            Debug.Log($"VAN DEBUG  player collider: {c.name}  enabled={c.enabled}  trigger={c.isTrigger}", c);

        Rigidbody playerBody = driverTransform.GetComponent<Rigidbody>();
        if (playerBody != null)
            Debug.Log($"VAN DEBUG  player has a Rigidbody  kinematic={playerBody.isKinematic}", playerBody);

        Notifier.Show("W/S drive   A/D steer   Space handbrake   E get out");
        GameAudio.Play(ProceduralSfx.Click, transform.position, 0.8f);
    }

    private void Exit()
    {
        if (driverTransform == null)
            return;

        driverTransform.SetParent(null, true);

        Vector3 position = exitPoint != null ? exitPoint.position : transform.position - transform.right * 2.2f;
        Quaternion rotation = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);
        driverTransform.SetPositionAndRotation(position + Vector3.up * 0.1f, rotation);

        if (driverController != null)
            driverController.enabled = true;

        if (inputLock != null)
            inputLock.Unlock(this);

        driverTransform = null;
        driverController = null;
        inputLock = null;
        throttle = 0f;
        steer = 0f;
        handbrake = false;
    }

    private void Update()
    {
        if (!IsDriven)
            return;

        float t = 0f;
        float s = 0f;
        bool brake = false;
        bool exit = false;

        Keyboard kb = Keyboard.current;
        if (kb != null)
        {
            if (kb.wKey.isPressed || kb.upArrowKey.isPressed) t += 1f;
            if (kb.sKey.isPressed || kb.downArrowKey.isPressed) t -= 1f;
            if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) s += 1f;
            if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) s -= 1f;
            brake |= kb.spaceKey.isPressed;
            exit |= kb.eKey.wasPressedThisFrame;
        }

        Gamepad pad = Gamepad.current;
        if (pad != null)
        {
            t += pad.rightTrigger.ReadValue() - pad.leftTrigger.ReadValue();
            s += pad.leftStick.x.ReadValue();
            brake |= pad.buttonSouth.isPressed;
            exit |= pad.buttonWest.wasPressedThisFrame;
        }

        throttle = Mathf.Clamp(t, -1f, 1f);
        steer = Mathf.Clamp(s, -1f, 1f);
        handbrake = brake;

        if (throttle != 0f) Debug.Log($"Throttle {throttle}  Speed {body.linearVelocity.magnitude:0.0}", this);

        // Small delay so the same E press that got us in doesn't get us out.
        if (exit && Time.time > enteredAt + 0.4f)
            Exit();
    }

    private void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;
        Vector3 v = transform.InverseTransformDirection(body.linearVelocity);

        float input = IsDriven ? throttle : 0f;

        if (!IsDriven)
        {
            // Parked: stay put.
            v.z = Mathf.MoveTowards(v.z, 0f, brakeStrength * 2f * dt);
        }
        else if (handbrake)
        {
            v.z = Mathf.MoveTowards(v.z, 0f, brakeStrength * dt);
        }
        else if (Mathf.Abs(input) < 0.01f)
        {
            v.z = Mathf.MoveTowards(v.z, 0f, coastDeceleration * dt);
        }
        else if (input * v.z < -0.1f)
        {
            // Pressing the opposite direction: brake first.
            v.z = Mathf.MoveTowards(v.z, 0f, brakeStrength * dt);
        }
        else
        {
            float target = input > 0f ? maxSpeed : -maxReverseSpeed;
            v.z = Mathf.MoveTowards(v.z, target * Mathf.Abs(input), acceleration * dt);
        }

        // Grip: remove sideways sliding.
        v.x = Mathf.Lerp(v.x, 0f, Mathf.Clamp01(sideGrip * dt));

        body.linearVelocity = transform.TransformDirection(v);

        // Steering only works while moving, and flips when reversing.
        float speedFactor = Mathf.Clamp(v.z / 3f, -1f, 1f);
        float yaw = (IsDriven ? steer : 0f) * steerSpeed * speedFactor;
        Vector3 angular = body.angularVelocity;
        angular.y = yaw * Mathf.Deg2Rad;
        body.angularVelocity = angular;
    }
}
using System;
using UnityEngine;

/// <summary>
/// Camera head bob and landing dip. Put this on the FPP Player.
/// It moves the CAMERA (child of View Pivot), so it never fights with the crouch logic
/// in FppPlayerController. It also raises Stepped and Landed events for footstep sounds.
/// </summary>
[DefaultExecutionOrder(-100)]
public sealed class HeadBob : MonoBehaviour
{
    [Header("References (auto-found if empty)")]
    [SerializeField] private FppPlayerController controller;
    [Tooltip("The object that gets bobbed. Auto-set to the camera.")]
    [SerializeField] private Transform bobTarget;

    [Header("Bob")]
    [SerializeField] private bool enableBob = true;
    [Tooltip("Distance walked for one full left and right step cycle.")]
    [SerializeField, Min(0.5f)] private float strideLength = 2.2f;
    [SerializeField, Min(0f)] private float verticalAmplitude = 0.035f;
    [SerializeField, Min(0f)] private float horizontalAmplitude = 0.025f;
    [Tooltip("Walk speed of your controller (default 5). Faster movement bobs harder.")]
    [SerializeField, Min(0.1f)] private float referenceSpeed = 5f;
    [SerializeField, Range(0f, 1f)] private float crouchMultiplier = 0.5f;
    [SerializeField, Min(1f)] private float smoothing = 14f;

    [Header("Landing")]
    [SerializeField, Min(0f)] private float landDip = 0.12f;
    [SerializeField, Min(1f)] private float landRecovery = 8f;
    [Tooltip("Seconds in the air before a landing counts (ignores small bumps).")]
    [SerializeField, Min(0f)] private float minAirTime = 0.25f;

    /// <summary>Fired on every footstep. Parameter is intensity 0 to 1.</summary>
    public event Action<float> Stepped;

    /// <summary>Fired when landing after a fall. Parameter is intensity 0 to 1.</summary>
    public event Action<float> Landed;

    private Vector3 baseLocalPosition;
    private Vector3 offset;
    private float landOffset;
    private float phase;
    private int lastStepIndex;
    private bool wasGrounded = true;
    private float airTime;

    private void Awake()
    {
        if (controller == null)
            controller = GetComponent<FppPlayerController>();

        if (bobTarget == null)
        {
            Camera cam = GetComponentInChildren<Camera>(true);
            if (cam != null)
                bobTarget = cam.transform;
        }

        if (bobTarget != null)
            baseLocalPosition = bobTarget.localPosition;
    }

    private void OnDisable()
    {
        if (bobTarget != null)
            bobTarget.localPosition = baseLocalPosition;
    }

    private void LateUpdate()
    {
        if (controller == null || bobTarget == null)
            return;

        float dt = Time.deltaTime;
        bool grounded = controller.IsGrounded;

        // Landing detection
        if (!grounded)
        {
            airTime += dt;
        }
        else
        {
            if (!wasGrounded && airTime > minAirTime)
            {
                float k = Mathf.Clamp01(airTime / 0.8f);
                landOffset = -landDip * k;
                Landed?.Invoke(k);
            }
            airTime = 0f;
        }
        wasGrounded = grounded;

        // Walking bob
        float speed = grounded ? controller.CurrentSpeed : 0f;
        Vector3 target = Vector3.zero;

        if (enableBob && speed > 0.1f)
        {
            float speedFactor = Mathf.Clamp(speed / referenceSpeed, 0f, 1.6f);
            if (controller.IsCrouched)
                speedFactor *= crouchMultiplier;

            phase += speed * dt / strideLength * Mathf.PI * 2f;

            int stepIndex = Mathf.FloorToInt(phase / Mathf.PI);
            if (stepIndex != lastStepIndex)
            {
                lastStepIndex = stepIndex;
                float intensity = Mathf.Clamp01(speed / (referenceSpeed * 1.6f)) * (controller.IsCrouched ? 0.4f : 1f);
                Stepped?.Invoke(Mathf.Max(0.15f, intensity));
            }

            target = new Vector3(
                Mathf.Cos(phase) * horizontalAmplitude,
                Mathf.Sin(phase * 2f) * verticalAmplitude,
                0f) * speedFactor;
        }

        offset = Vector3.Lerp(offset, target, 1f - Mathf.Exp(-smoothing * dt));
        landOffset = Mathf.Lerp(landOffset, 0f, 1f - Mathf.Exp(-landRecovery * dt));

        bobTarget.localPosition = baseLocalPosition + offset + Vector3.up * landOffset;
    }
}
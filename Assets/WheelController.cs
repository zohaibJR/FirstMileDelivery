using UnityEngine;

public class WheelController : MonoBehaviour
{
    [Header("Wheel Colliders")]
    [SerializeField] WheelCollider frontRight;
    [SerializeField] WheelCollider frontLeft;
    [SerializeField] WheelCollider backRight;
    [SerializeField] WheelCollider backLeft;

    [Header("Wheel Meshes")]
    [SerializeField] Transform frontRightTransform;
    [SerializeField] Transform frontLeftTransform;
    [SerializeField] Transform backRightTransform;
    [SerializeField] Transform backLeftTransform;

    [Header("Car Settings")]
    public float acceleration = 500f;
    public float brakingForce = 30f;
    public float maxTurnAngle = 15f;

    private float currentAcceleration = 0f;
    private float currentBrakingForce = 0f;
    private float currentTurnAngle = 0f;

    private void FixedUpdate()
    {
        // -------- INPUT --------
        float v = Input.GetAxis("Vertical");
        float h = Input.GetAxis("Horizontal");
        bool brake = Input.GetKey(KeyCode.Space);

        // Debug logs
        Debug.Log("Vertical: " + v);
        Debug.Log("Horizontal: " + h);

        // -------- MOTOR & BRAKE --------
        currentAcceleration = acceleration * v;
        currentBrakingForce = brake ? brakingForce : 0f;

        // IMPORTANT:
        // Motor torque on BACK wheels (better and correct setup)
        backRight.motorTorque = currentAcceleration;
        backLeft.motorTorque = currentAcceleration;

        // Brake on all four wheels
        frontRight.brakeTorque = currentBrakingForce;
        frontLeft.brakeTorque = currentBrakingForce;
        backRight.brakeTorque = currentBrakingForce;
        backLeft.brakeTorque = currentBrakingForce;

        // -------- STEERING --------
        currentTurnAngle = maxTurnAngle * h;

        frontLeft.steerAngle = currentTurnAngle;
        frontRight.steerAngle = currentTurnAngle;

        // -------- UPDATE WHEEL MESHES --------
        UpdateWheel(frontRight, frontRightTransform);
        UpdateWheel(frontLeft, frontLeftTransform);
        UpdateWheel(backRight, backRightTransform);
        UpdateWheel(backLeft, backLeftTransform);
    }

    void UpdateWheel(WheelCollider col, Transform trans)
    {
        if (col == null || trans == null)
            return;

        Vector3 pos;
        Quaternion rot;

        col.GetWorldPose(out pos, out rot);

        // Apply collider position to mesh
        trans.position = pos;

        // Apply rotation
        trans.rotation = rot;
    }
}

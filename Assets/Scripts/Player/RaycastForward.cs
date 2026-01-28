using UnityEngine;

public class RaycastForward : MonoBehaviour
{
    [Header("Raycast Settings")]
    public Transform rayOrigin;       // Drag & Drop point from inspector
    public float rayDistance = 10f;   // How far the ray should go
    public Color gizmoColor = Color.red; // Color of the ray gizmo

    void Update()
    {
        if (rayOrigin == null) return;

        // Raycast forward in +Z direction
        Ray ray = new Ray(rayOrigin.position, rayOrigin.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, rayDistance))
        {
            Debug.Log("Ray hit: " + hit.collider.name);
        }
    }

    // Draw Gizmos even in PLAY MODE
    void OnDrawGizmos()
    {
        if (rayOrigin == null) return;

        Gizmos.color = gizmoColor;

        // Draw forward ray
        Gizmos.DrawLine(rayOrigin.position, rayOrigin.position + rayOrigin.forward * rayDistance);

        // Draw origin point
        Gizmos.DrawSphere(rayOrigin.position, 0.05f);
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InteractionController : MonoBehaviour
{
    [Header("Raycast Settings")]
    public Camera playerCamera;
    public float interactDistance = 2f;
    public LayerMask interactLayer;

    [Header("Interactable Tags")]
    public List<string> detectableTags = new List<string>() { "Package", "Door", "PlayerHouse" };

    [Header("Pickup Settings")]
    public Transform holdPosition;
    public float pickupSmoothness = 15f;

    private GameObject heldObject = null;
    private Rigidbody heldRb = null;

    [Header("UI")]
    public GameObject PickupText;
    public GameObject dropText;
    private PackageDetailUI packageUI;

    public PlayerPickupManager playerPickupManager;
    private bool isLookingAtPackage = false;

    [Header("Player House")]
    public Transform playerHouseEntryPoint;
    public Transform playerHouseExitPoint;
    public bool isPlayerInHouse = false;

    void Start()
    {
        packageUI = FindObjectOfType<PackageDetailUI>();
    }

    void Update()
    {
        if (heldObject == null)
            DetectObject();
        else
        {
            HoldObject();
            DropCheck();
        }
    }

    // --------------------------------------------------------------
    // RAYCAST + DETECTION
    // --------------------------------------------------------------
    void DetectObject()
    {
        PickupText.SetActive(false);

        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        RaycastHit hit;

        int layerMask = (interactLayer.value == 0) ? ~0 : interactLayer;

        if (Physics.Raycast(ray, out hit, interactDistance, layerMask))
        {
            string hitTag = hit.collider.tag;

            if (hitTag == "Package")
            {
                ShowPackageDetails(hit);
                HandlePickup(hit);
                return;
            }

            if (hitTag == "PlayerHouse")
            {
                PickupText.SetActive(true);
                PickupText.GetComponent<Text>().text =
                    isPlayerInHouse ? "Press E to Exit" : "Press E to Enter";

                if (Input.GetKeyDown(KeyCode.E))
                    StartCoroutine(TeleportRoutine());

                return;
            }

            if (detectableTags.Contains(hitTag))
            {
                PickupText.SetActive(true);
                isLookingAtPackage = false;
                packageUI.DectivatePanel();
                return;
            }
        }

        if (isLookingAtPackage)
        {
            packageUI.DectivatePanel();
            isLookingAtPackage = false;
        }
    }

    // --------------------------------------------------------------
    // SAFE TELEPORT
    // --------------------------------------------------------------
    IEnumerator TeleportRoutine()
    {
        CharacterController cc = GetComponent<CharacterController>();
        Rigidbody rb = GetComponent<Rigidbody>();

        if (cc != null) cc.enabled = false;
        if (rb != null) rb.isKinematic = true;

        yield return null;

        if (!isPlayerInHouse)
        {
            if (playerHouseEntryPoint == null) yield break;
            transform.position = playerHouseEntryPoint.position;
            transform.rotation = playerHouseEntryPoint.rotation;
            isPlayerInHouse = true;
            Debug.Log("Player entered house");
        }
        else
        {
            if (playerHouseExitPoint == null) yield break;
            transform.position = playerHouseExitPoint.position;
            transform.rotation = playerHouseExitPoint.rotation;
            isPlayerInHouse = false;
            Debug.Log("Player exited house");
        }

        yield return null;

        if (cc != null) cc.enabled = true;
        if (rb != null) rb.isKinematic = false;
    }

    // --------------------------------------------------------------
    // PACKAGE UI
    // --------------------------------------------------------------
    void ShowPackageDetails(RaycastHit hit)
    {
        PickupText.SetActive(true);

        PackageData data = hit.collider.GetComponent<PackageData>();
        if (data != null)
            packageUI.ActivatePanel(data);

        isLookingAtPackage = true;
    }

    // --------------------------------------------------------------
    // PICKUP WITH E
    // --------------------------------------------------------------
    void HandlePickup(RaycastHit hit)
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            PickupObject(hit.collider.gameObject);
            PickupText.SetActive(false);
            packageUI.DectivatePanel();
        }
    }

    // --------------------------------------------------------------
    // PICKUP LOGIC
    // --------------------------------------------------------------
    void PickupObject(GameObject obj)
    {
        dropText.SetActive(true);
        heldObject = obj;
        heldRb = obj.GetComponent<Rigidbody>();

        Collider col = obj.GetComponent<Collider>();
        if (col != null) col.enabled = false;

        PackageData data = obj.GetComponent<PackageData>();
        if (data != null) data.status = PackageStatus.Picked;

        if (heldRb != null)
        {
            heldRb.useGravity = false;
            heldRb.isKinematic = true;
        }

        obj.transform.SetParent(holdPosition);
        obj.transform.localPosition = Vector3.zero;
        obj.transform.localRotation = Quaternion.identity;

        playerPickupManager.ObjectPickedUp();
    }

    // --------------------------------------------------------------
    // HOLD OBJECT
    // --------------------------------------------------------------
    void HoldObject()
    {
        heldObject.transform.localPosition = Vector3.Lerp(
            heldObject.transform.localPosition,
            Vector3.zero,
            Time.deltaTime * pickupSmoothness
        );
    }

    // --------------------------------------------------------------
    // DROP SYSTEM
    // --------------------------------------------------------------
    void DropCheck()
    {
        if (Input.GetKeyDown(KeyCode.Q))
            DropObject();
    }

    void DropObject()
    {
        dropText.SetActive(false);
        if (heldObject == null) return;

        Collider col = heldObject.GetComponent<Collider>();
        if (col != null) col.enabled = true;

        PackageData data = heldObject.GetComponent<PackageData>();
        if (data != null) data.status = PackageStatus.Waiting;

        heldObject.transform.SetParent(null);

        if (heldRb != null)
        {
            heldRb.isKinematic = false;
            heldRb.useGravity = true;
            heldRb.linearVelocity = Vector3.zero;
            heldRb.angularVelocity = Vector3.zero;
        }

        heldObject = null;
        heldRb = null;

        playerPickupManager.ObjectDropped();
    }
}

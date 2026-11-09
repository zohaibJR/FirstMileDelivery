using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Draws markers on the phone's Map page. Put this on the FPP Player (next to SmartphoneUI).
/// North is always up. The map camera is only switched on while the Map page is visible.
/// Markers: player arrow, today's undelivered houses, fixed places, and the rented truck.
/// </summary>
public sealed class PhoneMapMarkers : MonoBehaviour
{
    [Serializable]
    public sealed class MapPlace
    {
        public string label = "Place";
        public Transform target;
        public Color color = Color.white;
    }

    [Header("Map")]
    [Tooltip("The MapCamera that renders the town from above.")]
    [SerializeField] private Camera mapCamera;
    [Tooltip("The MapImage (Raw Image) on Page_Map. Markers are created inside it.")]
    [SerializeField] private RectTransform mapImage;
    [Tooltip("Which page of the phone is the map (Deliveries 0, Map 1, Info 2).")]
    [SerializeField, Min(0)] private int mapPageIndex = 1;

    [Header("Fixed places (Warehouse, Home, Rental Office)")]
    [SerializeField] private List<MapPlace> places = new List<MapPlace>();

    [Header("Truck")]
    [Tooltip("The Counter object that has the RentalOffice component.")]
    [SerializeField] private RentalOffice rentalOffice;

    [Header("Look")]
    [Tooltip("Optional. Your font for the marker labels.")]
    [SerializeField] private TMP_FontAsset labelFont;
    [SerializeField] private Color houseColor = new Color(1f, 0.84f, 0.29f);
    [SerializeField] private Color truckColor = new Color(1f, 0.55f, 0.2f);
    [SerializeField] private Color playerColor = new Color(0.3f, 0.85f, 1f);
    [SerializeField, Min(6f)] private float markerSize = 22f;
    [SerializeField, Min(6f)] private float playerSize = 28f;
    [SerializeField, Min(8f)] private float labelSize = 18f;

    private SmartphoneUI phone;
    private DailyBatchManager batch;
    private bool active;

    private RectTransform placesRoot;
    private RectTransform housesRoot;
    private RectTransform truckMarker;
    private RectTransform playerMarker;

    private readonly List<RectTransform> placeMarkers = new List<RectTransform>();
    private readonly List<KeyValuePair<RectTransform, Transform>> houseMarkers = new List<KeyValuePair<RectTransform, Transform>>();

    private void Start()
    {
        phone = SmartphoneUI.Instance;
        batch = DailyBatchManager.Instance;

        if (mapCamera == null || mapImage == null)
        {
            Debug.LogWarning("PhoneMapMarkers: Map Camera or Map Image is not assigned.", this);
            enabled = false;
            return;
        }

        BuildStaticMarkers();
        mapCamera.enabled = false;

        if (phone != null)
        {
            phone.Opened += UpdateState;
            phone.Closed += UpdateState;
            phone.PageChanged += OnPageChanged;
        }

        if (batch != null)
            batch.BatchChanged += OnBatchChanged;
    }

    private void OnDestroy()
    {
        if (phone != null)
        {
            phone.Opened -= UpdateState;
            phone.Closed -= UpdateState;
            phone.PageChanged -= OnPageChanged;
        }

        if (batch != null)
            batch.BatchChanged -= OnBatchChanged;
    }

    private void OnPageChanged(int page)
    {
        UpdateState();
    }

    private void OnBatchChanged()
    {
        if (active)
            RebuildHouseMarkers();
    }

    private void UpdateState()
    {
        active = phone != null && phone.IsOpen && phone.CurrentPage == mapPageIndex;
        mapCamera.enabled = active;

        if (!active)
            return;

        RebuildHouseMarkers();
        UpdatePositions();
    }

    private void Update()
    {
        if (active)
            UpdatePositions();
    }

    // ---------------- Building markers ----------------

    private void BuildStaticMarkers()
    {
        // Created in this order so the player arrow is always drawn on top.
        placesRoot = CreateRoot("Places");
        housesRoot = CreateRoot("Houses");

        foreach (MapPlace place in places)
        {
            RectTransform marker = CreateMarker(place.label, placesRoot, place.color, markerSize + 4f);
            AddLabel(marker, place.label, markerSize + 4f);
            placeMarkers.Add(marker);
        }

        truckMarker = CreateMarker("Truck", mapImage, truckColor, markerSize + 2f);
        AddLabel(truckMarker, "Truck", markerSize + 2f);
        truckMarker.gameObject.SetActive(false);

        playerMarker = CreateMarker("Player", mapImage, playerColor, playerSize);
        RectTransform nose = CreateMarker("Nose", playerMarker, playerColor, playerSize * 0.5f);
        nose.anchoredPosition = new Vector2(0f, playerSize * 0.6f);
    }

    private RectTransform CreateRoot(string name)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.SetParent(mapImage, false);
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        return rt;
    }

    private static RectTransform CreateMarker(string name, Transform parent, Color color, float size)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(size, size);

        Image image = go.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return rt;
    }

    private void AddLabel(RectTransform marker, string text, float markerHeight)
    {
        if (string.IsNullOrEmpty(text))
            return;

        GameObject go = new GameObject("Label", typeof(RectTransform));
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.SetParent(marker, false);
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.sizeDelta = new Vector2(220f, labelSize + 8f);
        rt.anchoredPosition = new Vector2(0f, -markerHeight * 0.5f);

        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = labelSize;
        tmp.alignment = TextAlignmentOptions.Top;
        tmp.color = Color.white;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        tmp.raycastTarget = false;

        if (labelFont != null)
            tmp.font = labelFont;
    }

    private void RebuildHouseMarkers()
    {
        for (int i = housesRoot.childCount - 1; i >= 0; i--)
            Destroy(housesRoot.GetChild(i).gameObject);

        houseMarkers.Clear();

        if (batch == null || !batch.HasBatch)
            return;

        var assignments = batch.Assignments;
        for (int i = 0; i < assignments.Count; i++)
        {
            PackageAssignment a = assignments[i];
            if (a.delivered)
                continue;

            if (!HouseId.TryGet(a.houseId, out HouseId house))
                continue;

            RectTransform marker = CreateMarker(house.name, housesRoot, houseColor, markerSize);
            AddLabel(marker, a.address, markerSize);
            houseMarkers.Add(new KeyValuePair<RectTransform, Transform>(marker, house.transform));
        }
    }

    // ---------------- Positions ----------------

    private void UpdatePositions()
    {
        for (int i = 0; i < placeMarkers.Count && i < places.Count; i++)
        {
            Transform target = places[i].target;
            if (target != null)
                Place(placeMarkers[i], target.position);
        }

        foreach (KeyValuePair<RectTransform, Transform> pair in houseMarkers)
        {
            if (pair.Value != null)
                Place(pair.Key, pair.Value.position);
        }

        if (rentalOffice != null && rentalOffice.IsRented && rentalOffice.SpawnedVehicle != null)
            Place(truckMarker, rentalOffice.SpawnedVehicle.transform.position);
        else
            truckMarker.gameObject.SetActive(false);

        Place(playerMarker, transform.position);

        Camera view = Camera.main;
        if (view != null)
            playerMarker.localRotation = Quaternion.Euler(0f, 0f, -view.transform.eulerAngles.y);
    }

    // Puts a marker on the map where a world position is, and hides it if that spot is outside the map.
    private void Place(RectTransform marker, Vector3 worldPosition)
    {
        Vector3 viewport = mapCamera.WorldToViewportPoint(worldPosition);
        bool visible = viewport.z > 0f && viewport.x >= 0f && viewport.x <= 1f && viewport.y >= 0f && viewport.y <= 1f;

        if (marker.gameObject.activeSelf != visible)
            marker.gameObject.SetActive(visible);

        if (!visible)
            return;

        Vector2 size = mapImage.rect.size;
        marker.anchoredPosition = new Vector2((viewport.x - 0.5f) * size.x, (viewport.y - 0.5f) * size.y);
    }
}
using UnityEngine;
using TMPro;

public class DayNightCycle : MonoBehaviour
{
    [Header("Sun Settings")]
    public Light sun;

    [Header("Time Settings")]
    [Range(0, 24)] public float currentTime = 8f;
    public float dayDurationInMinutes = 5f;

    [Header("Morning / Night Control")]
    [Range(0, 24)] public float morningTime = 6f;
    [Range(0, 24)] public float nightTime = 18f;

    [Header("UI")]
    public TMP_Text timeText;

    private float timeSpeed;

    void Start()
    {
        timeSpeed = 24f / (dayDurationInMinutes * 60f);
    }

    void Update()
    {
        UpdateTime();
        RotateSun();
        UpdateUI();
    }

    void UpdateTime()
    {
        currentTime += Time.deltaTime * timeSpeed;
        if (currentTime >= 24f)
            currentTime = 0f;
    }

    void RotateSun()
    {
        float sunAngle = (currentTime / 24f) * 360f - 90f;
        sun.transform.rotation = Quaternion.Euler(sunAngle, 170f, 0f);

        // Light intensity control
        if (currentTime >= morningTime && currentTime <= nightTime)
            sun.intensity = 1f;
        else
            sun.intensity = 0.1f;
    }

    void UpdateUI()
    {
        int hours = Mathf.FloorToInt(currentTime);
        int minutes = Mathf.FloorToInt((currentTime - hours) * 60f);

        string amPm = hours >= 12 ? "PM" : "AM";
        int displayHour = hours % 12;
        if (displayHour == 0) displayHour = 12;

        timeText.text = $"{displayHour:00}:{minutes:00} {amPm}";
    }
}

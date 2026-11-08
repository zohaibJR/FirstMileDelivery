using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Makes the sun follow the clock: sunrise, noon, sunset, night.
/// Put it on the "GameSystems" object and drag your Directional Light into "Sun".
/// Works with the Built-in and URP pipelines. Every value can be tuned in the Inspector.
/// The curves use the time of day from 0 to 1 on the horizontal axis:
/// 0.25 = 6 AM, 0.5 = noon, 0.75 = 6 PM.
/// </summary>
[DefaultExecutionOrder(100)]
public sealed class DayNightCycle : MonoBehaviour
{
    [Header("Sun")]
    [Tooltip("Drag your Directional Light here.")]
    [SerializeField] private Light sun;
    [Tooltip("Compass direction the sun travels along. Changes which side shadows fall on.")]
    [SerializeField, Range(-180f, 180f)] private float sunDirection = -30f;
    [SerializeField] private Gradient sunColor = DefaultSunColor();

    [Header("Brightness over the day")]
    [SerializeField]
    private AnimationCurve sunIntensity = MakeCurve(
        new Vector2(0f, 0f), new Vector2(0.22f, 0f), new Vector2(0.28f, 0.5f), new Vector2(0.35f, 1f),
        new Vector2(0.5f, 1.15f), new Vector2(0.65f, 1f), new Vector2(0.72f, 0.5f),
        new Vector2(0.78f, 0f), new Vector2(1f, 0f));
    [SerializeField]
    private AnimationCurve ambientIntensity = MakeCurve(
        new Vector2(0f, 0.25f), new Vector2(0.22f, 0.25f), new Vector2(0.3f, 0.9f), new Vector2(0.5f, 1f),
        new Vector2(0.7f, 0.9f), new Vector2(0.78f, 0.25f), new Vector2(1f, 0.25f));

    [Header("Skybox")]
    [Tooltip("Darkens the sky at night. Needs a skybox material that has an Exposure setting.")]
    [SerializeField] private bool controlSkybox = true;
    [SerializeField]
    private AnimationCurve skyExposure = MakeCurve(
        new Vector2(0f, 0.12f), new Vector2(0.22f, 0.12f), new Vector2(0.3f, 1f), new Vector2(0.5f, 1.3f),
        new Vector2(0.7f, 1f), new Vector2(0.78f, 0.12f), new Vector2(1f, 0.12f));

    [Header("Performance")]
    [Tooltip("How often (real seconds) the ambient light is refreshed from the sky. 0 = never.")]
    [SerializeField, Min(0f)] private float lightingRefreshSeconds = 1.5f;

    private static readonly int ExposureId = Shader.PropertyToID("_Exposure");

    private Material originalSkybox;
    private Material skyboxInstance;
    private Color baseAmbientLight, baseAmbientSky, baseAmbientEquator, baseAmbientGround;
    private float refreshTimer;

    private void Awake()
    {
        if (sun == null)
            sun = RenderSettings.sun;

        if (sun == null)
            Debug.LogError("DayNightCycle: no sun found. Drag your Directional Light into the Sun field.", this);

        baseAmbientLight = RenderSettings.ambientLight;
        baseAmbientSky = RenderSettings.ambientSkyColor;
        baseAmbientEquator = RenderSettings.ambientEquatorColor;
        baseAmbientGround = RenderSettings.ambientGroundColor;

        // Work on a copy so the skybox asset on disk is never changed.
        if (controlSkybox && RenderSettings.skybox != null && RenderSettings.skybox.HasProperty(ExposureId))
        {
            originalSkybox = RenderSettings.skybox;
            skyboxInstance = new Material(originalSkybox);
            RenderSettings.skybox = skyboxInstance;
        }
    }

    private void OnDestroy()
    {
        if (skyboxInstance != null)
        {
            RenderSettings.skybox = originalSkybox;
            Destroy(skyboxInstance);
        }
    }

    private void Update()
    {
        TimeManager time = TimeManager.Instance;
        if (time == null || sun == null)
            return;

        Apply(time.NormalizedTime);

        if (lightingRefreshSeconds > 0f)
        {
            refreshTimer += Time.unscaledDeltaTime;
            if (refreshTimer >= lightingRefreshSeconds)
            {
                refreshTimer = 0f;
                DynamicGI.UpdateEnvironment();
            }
        }
    }

    private void Apply(float t)
    {
        // 6 AM (t = 0.25) is the horizon, noon is straight overhead, 6 PM is the horizon again.
        float angle = (t - 0.25f) * 360f;
        sun.transform.rotation = Quaternion.Euler(angle, sunDirection, 0f);

        sun.color = sunColor.Evaluate(t);
        float intensity = Mathf.Max(0f, sunIntensity.Evaluate(t));
        sun.intensity = intensity;

        // A sun below the horizon would light things from underneath, so switch it off.
        sun.enabled = intensity > 0.01f;

        float ambient = Mathf.Max(0f, ambientIntensity.Evaluate(t));
        switch (RenderSettings.ambientMode)
        {
            case AmbientMode.Skybox:
                RenderSettings.ambientIntensity = ambient;
                break;
            case AmbientMode.Trilight:
                RenderSettings.ambientSkyColor = baseAmbientSky * ambient;
                RenderSettings.ambientEquatorColor = baseAmbientEquator * ambient;
                RenderSettings.ambientGroundColor = baseAmbientGround * ambient;
                break;
            default:
                RenderSettings.ambientLight = baseAmbientLight * ambient;
                break;
        }

        if (skyboxInstance != null)
            skyboxInstance.SetFloat(ExposureId, Mathf.Max(0f, skyExposure.Evaluate(t)));
    }

    private static AnimationCurve MakeCurve(params Vector2[] points)
    {
        AnimationCurve curve = new AnimationCurve();
        foreach (Vector2 p in points)
            curve.AddKey(p.x, p.y);

        for (int i = 0; i < curve.length; i++)
            curve.SmoothTangents(i, 0f);

        return curve;
    }

    private static Gradient DefaultSunColor()
    {
        Color night = new Color(0.35f, 0.45f, 0.8f);
        Color sunrise = new Color(1f, 0.62f, 0.35f);
        Color day = new Color(1f, 0.96f, 0.88f);
        Color sunset = new Color(1f, 0.55f, 0.3f);

        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(night, 0f),
                new GradientColorKey(night, 0.22f),
                new GradientColorKey(sunrise, 0.27f),
                new GradientColorKey(day, 0.38f),
                new GradientColorKey(day, 0.62f),
                new GradientColorKey(sunset, 0.73f),
                new GradientColorKey(night, 0.79f),
                new GradientColorKey(night, 1f)
            },
            new[]
            {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(1f, 1f)
            });
        return gradient;
    }
}
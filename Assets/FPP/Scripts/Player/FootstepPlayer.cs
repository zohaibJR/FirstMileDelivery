using UnityEngine;

/// <summary>
/// Plays footstep and landing sounds. Put this on the FPP Player (HeadBob is added automatically).
/// Leave the clip lists empty to use generated placeholder sounds.
/// </summary>
[RequireComponent(typeof(HeadBob))]
public sealed class FootstepPlayer : MonoBehaviour
{
    [Header("Clips (empty = generated placeholder)")]
    [SerializeField] private AudioClip[] stepClips;
    [SerializeField] private AudioClip landClip;
    [SerializeField] private bool useProceduralFallback = true;

    [Header("Volume")]
    [SerializeField, Range(0f, 1f)] private float stepVolume = 0.5f;
    [SerializeField, Range(0f, 1f)] private float landVolume = 0.8f;
    [SerializeField, Range(0f, 0.5f)] private float pitchVariation = 0.12f;

    private HeadBob bob;

    private void Awake()
    {
        bob = GetComponent<HeadBob>();
    }

    private void OnEnable()
    {
        if (bob == null)
            bob = GetComponent<HeadBob>();

        bob.Stepped += OnStep;
        bob.Landed += OnLand;
    }

    private void OnDisable()
    {
        if (bob == null)
            return;

        bob.Stepped -= OnStep;
        bob.Landed -= OnLand;
    }

    private void OnStep(float intensity)
    {
        AudioClip clip = PickStepClip();
        float volume = stepVolume * Mathf.Lerp(0.5f, 1f, intensity);
        GameAudio.Play(clip, transform.position, volume, pitchVariation, 0f);
    }

    private void OnLand(float intensity)
    {
        AudioClip clip = GameAudio.Or(landClip, useProceduralFallback ? ProceduralSfx.Thud : null);
        float volume = landVolume * Mathf.Lerp(0.4f, 1f, intensity);
        GameAudio.Play(clip, transform.position, volume, pitchVariation, 0f);
    }

    private AudioClip PickStepClip()
    {
        if (stepClips != null && stepClips.Length > 0)
        {
            AudioClip clip = stepClips[Random.Range(0, stepClips.Length)];
            if (clip != null)
                return clip;
        }

        return useProceduralFallback ? ProceduralSfx.Step : null;
    }
}
/*
 * Script: WaveLightController
 *
 * Description:
 * Controls warning lights during transitions between enemy waves.
 * The assigned Unity Light components fade between off and red
 * repeatedly until the next wave begins.
 *
 * Optional emissive renderers can also be assigned so the visible
 * light fixture material flashes along with the actual light source.
 *
 * Responsibilities:
 * - Store all wave-warning Light components.
 * - Fade light intensity in and out.
 * - Temporarily change the lights to red.
 * - Optionally control emissive fixture materials.
 * - Restore the lights after the warning sequence.
 *
 * Interacts With:
 * - WaveManager
 */

using System.Collections;
using UnityEngine;

public class WaveLightController : MonoBehaviour
{
    public static WaveLightController instance;

    [Header("Unity Light Components")]
    [Tooltip("Drag the Point Light components here.")]
    [SerializeField] private Light[] warningLights;

    [Header("Optional Emissive Fixtures")]
    [Tooltip("Drag Mesh Renderer components here if the fixtures have emissive materials.")]
    [SerializeField] private Renderer[] emissiveRenderers;

    [Header("Flash Settings")]
    [Tooltip("colour the lights turn during a warning, red by default")]
    [SerializeField] private Color warningColor = Color.red;

    [Tooltip("brightness at the peak of each flash")]
    [SerializeField] private float maximumIntensity;

    [Tooltip("seconds for one fade up or down, a full flash is twice this")]
    [SerializeField] private float fadeDuration;

    [Tooltip("emission strength on the fixture materials, separate from light intensity")]
    [SerializeField] private float emissionIntensity;

    // the running flash, so a new warning cancels the old one instead of stacking
    private Coroutine flashCoroutine;

    // what the lights looked like before the warning, restored afterward
    private Color[] originalLightColors;
    private float[] originalLightIntensities;

    // instanced materials, so changing emission doesn't edit the shared asset
    private Material[] emissiveMaterials;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
    }

    private void Start()
    {
        saveOriginalLightSettings();
        prepareEmissionMaterials();

        // Debug.Log(
        //     "WaveLightController initialized with " +
        //     warningLights.Length +
        //     " Unity lights."
        // );
    }

    private void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }
    // caches colour and intensity so the warning can put them back exactly
    private void saveOriginalLightSettings()
    {
        originalLightColors = new Color[warningLights.Length];
        originalLightIntensities = new float[warningLights.Length];

        for (int i = 0; i < warningLights.Length; i++)
        {
            if (warningLights[i] == null)
                continue;

            originalLightColors[i] = warningLights[i].color;
            originalLightIntensities[i] = warningLights[i].intensity;
        }
    }

    // instances each fixture material and enables its emission keyword.
    // without instancing this would edit the shared material in the project.
    private void prepareEmissionMaterials()
    {
        emissiveMaterials = new Material[emissiveRenderers.Length];

        for (int i = 0; i < emissiveRenderers.Length; i++)
        {
            if (emissiveRenderers[i] == null)
                continue;

            // Renderer.material creates a separate material instance
            // so changing it does not modify every object using that material.
            emissiveMaterials[i] = emissiveRenderers[i].material;

            if (emissiveMaterials[i].HasProperty("_EmissionColor"))
            {
                emissiveMaterials[i].EnableKeyword("_EMISSION");
                emissiveMaterials[i].SetColor(
                    "_EmissionColor",
                    Color.black
                );
            }
        }
    }

    /// <summary>
    /// Flashes the warning lights for the supplied duration.
    /// Called by WaveManager during the wait between waves.
    /// </summary>
    public void FlashWarningLights(float duration)
    {
        if (duration <= 0f)
        {
            // Debug.LogWarning(
            //     "WaveLightController received a duration of zero or less."
            // );

            return;
        }

        if (warningLights == null || warningLights.Length == 0)
        {
            // Debug.LogWarning(
            //     "WaveLightController has no Light components assigned."
            // );

            return;
        }

        // Stop an existing warning sequence before starting another.
        if (flashCoroutine != null)
        {
            StopCoroutine(flashCoroutine);
        }

        //Debug.Log("Wave warning lights started for " + duration + " seconds.");

        flashCoroutine = StartCoroutine(flashRoutine(duration));
    }

    private IEnumerator flashRoutine(float totalDuration)
    {
        float elapsedTime = 0f;

        while (elapsedTime < totalDuration)
        {
            // Fade from off to bright red.
            yield return StartCoroutine(
                fadeLights(0f, maximumIntensity, fadeDuration)
            );

            elapsedTime += fadeDuration;

            if (elapsedTime >= totalDuration)
                break;

            // Fade from bright red back to off.
            yield return StartCoroutine(
                fadeLights(maximumIntensity, 0f, fadeDuration)
            );

            elapsedTime += fadeDuration;
        }

        setWarningIntensity(0f);

        flashCoroutine = null;

        //Debug.Log("Wave warning lights finished.");
    }

    private IEnumerator fadeLights(
        float startingIntensity,
        float targetIntensity,
        float duration)
    {
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.unscaledDeltaTime;

            float percent = duration > 0f
                ? timer / duration
                : 1f;

            float intensity = Mathf.Lerp(
                startingIntensity,
                targetIntensity,
                percent
            );

            setWarningIntensity(intensity);

            yield return null;
        }

        setWarningIntensity(targetIntensity);
    }

    private void setWarningIntensity(float intensity)
    {
        float intensityPercent = maximumIntensity > 0f
            ? intensity / maximumIntensity
            : 0f;

        // Change the actual Unity Point Lights.
        foreach (Light warningLight in warningLights)
        {
            if (warningLight == null)
                continue;

            warningLight.enabled = intensity > 0.01f;
            warningLight.color = warningColor;
            warningLight.intensity = intensity;
        }

        // Change optional emissive fixture materials.
        /*foreach (Material material in emissiveMaterials)
        {
            if (material == null ||
                !material.HasProperty("_EmissionColor"))
            {
                continue;
            }

            Color emissionColor =
                warningColor *
                emissionIntensity *
                intensityPercent;

            material.SetColor("_EmissionColor", emissionColor);
        }*/
    }

    // Stops the warning effect and restores the lights to
    // their original Inspector settings.
    public void RestoreOriginalLights()
    {
        if (flashCoroutine != null)
        {
            StopCoroutine(flashCoroutine);
            flashCoroutine = null;
        }

        for (int i = 0; i < warningLights.Length; i++)
        {
            if (warningLights[i] == null)
                continue;

            warningLights[i].enabled = true;
            warningLights[i].color = originalLightColors[i];
            warningLights[i].intensity =
                originalLightIntensities[i];
        }
    }
}

using System.Collections;
using UnityEngine;

public class GardenController : MonoBehaviour
{
    // Attach this to your "GardenRoot" object that controls environment visuals.
    // Expected future responsibilities:
    // - Animate lighting, weather, wind, particles.
    // - Shift season states and vegetation growth.
    // - Adjust color grading/material parameters based on affect.
    public TobyFredson.TobyGlobalShadersController ttfeController;
    public Light directionalLight;
    public ParticleSystem rainSystem;
    public ParticleSystem lightningSystem;
    public Material phase1Skybox;
    public Material phase2Skybox;
    public Material phase3Skybox;

    private ParticleSystem.EmissionModule rainEmission;
    private Coroutine lightningFlashCoroutine;

    private void Start()
    {
        if (rainSystem != null)
        {
            rainEmission = rainSystem.emission;
            rainSystem.gameObject.SetActive(false);
        }

        if (lightningSystem != null)
        {
            lightningSystem.gameObject.SetActive(false);
        }
    }

    public void RunResponsiveGarden(float valence, float intensity)
    {
        // Placeholder:
        // Use valence/intensity from participant state to drive environment adaptively.
        // Example: positive valence -> warmer light, richer bloom, more motion.
        Debug.Log($"Responsive garden update. Valence={valence}, Intensity={intensity}");
    }

    public void RunNonResponsiveGarden()
    {
        // Placeholder:
        // Keep environment static or follow a pre-scripted pattern not tied to participant state.
        Debug.Log("Non-responsive garden update.");
    }

    public void StartNonResponsiveSequence()
    {
        StartCoroutine(RunNonResponsiveSequence());
    }

    public void StartSeasonEscalation()
    {
        StartCoroutine(RunSeasonEscalationSequence());
    }

    // Runs a fixed non-responsive TTFE sequence over time for experiment control.
    public IEnumerator RunNonResponsiveSequence()
    {
        if (ttfeController == null)
        {
            Debug.LogWarning("GardenController: ttfeController is not assigned.");
            yield break;
        }

        ttfeController.SetSeason(-2f);
        ttfeController.SetWindSpeed(1f);
        ttfeController.SetWindStrength(0.1f);

        float elapsed = 0f;
        while (elapsed < 10f)
        {
            float t = elapsed / 10f;
            ttfeController.SetSeason(Mathf.Lerp(-2f, 2f, t));
            elapsed += Time.deltaTime;
            yield return null;
        }
        ttfeController.SetSeason(2f);

        elapsed = 0f;
        while (elapsed < 10f)
        {
            float t = elapsed / 10f;
            ttfeController.SetWindSpeed(Mathf.Lerp(1f, 3f, t));
            ttfeController.SetWindStrength(Mathf.Lerp(0.1f, 1f, t));
            elapsed += Time.deltaTime;
            yield return null;
        }

        ttfeController.SetWindSpeed(3f);
        ttfeController.SetWindStrength(1f);
    }

    public IEnumerator RunSeasonEscalationSequence()
    {
        Debug.Log("STARTING");
        if (ttfeController == null)
        {
            Debug.LogWarning("GardenController: ttfeController is not assigned.");
            yield break;
        }

        float totalElapsed = 0f;
        float nextLightningAt = Random.Range(10f, 20f);

        // Phase 1 (0-30s): calm baseline.
        if (phase1Skybox != null)
        {
            RenderSettings.skybox = phase1Skybox;
            DynamicGI.UpdateEnvironment();
        }

        if (rainSystem != null)
        {
            rainSystem.gameObject.SetActive(false);
        }

        if (lightningSystem != null)
        {
            lightningSystem.gameObject.SetActive(false);
        }

        ttfeController.SetSeason(-1f);
        ttfeController.SetWindSpeed(1f);
        ttfeController.SetWindStrength(0.1f);

        if (directionalLight != null)
        {
            directionalLight.intensity = 1f;
        }

        RenderSettings.fogDensity = 0f;

        if (rainSystem != null)
        {
            rainEmission.rateOverTime = 0f;
        }

        while (totalElapsed < 30f)
        {
            if (rainSystem != null && rainSystem.gameObject.activeSelf)
            {
                rainSystem.gameObject.SetActive(false);
            }

            if (lightningSystem != null && lightningSystem.gameObject.activeSelf)
            {
                lightningSystem.gameObject.SetActive(false);
            }

            totalElapsed += Time.deltaTime;
            yield return null;
        }

        // Phase 2 (30-60s): moderate escalation.
        if (phase2Skybox != null)
        {
            RenderSettings.skybox = phase2Skybox;
            DynamicGI.UpdateEnvironment();
        }

        if (rainSystem != null)
        {
            rainSystem.gameObject.SetActive(false);
        }

        if (lightningSystem != null)
        {
            lightningSystem.gameObject.SetActive(false);
        }

        float phase2Elapsed = 0f;
        while (phase2Elapsed < 30f)
        {
            if (rainSystem != null && rainSystem.gameObject.activeSelf)
            {
                rainSystem.gameObject.SetActive(false);
            }

            if (lightningSystem != null && lightningSystem.gameObject.activeSelf)
            {
                lightningSystem.gameObject.SetActive(false);
            }

            float t = phase2Elapsed / 30f;
            ttfeController.SetSeason(Mathf.Lerp(-1f, 1f, t));
            ttfeController.SetWindSpeed(Mathf.Lerp(1f, 2f, t));
            ttfeController.SetWindStrength(Mathf.Lerp(0.1f, 0.5f, t));

            if (directionalLight != null)
            {
                directionalLight.intensity = Mathf.Lerp(1f, 0.6f, t);
            }

            RenderSettings.fogDensity = Mathf.Lerp(0f, 0.01f, t);

            phase2Elapsed += Time.deltaTime;
            totalElapsed += Time.deltaTime;
            yield return null;
        }

        // Phase 3 (60-120s): full escalation.
        if (phase3Skybox != null)
        {
            RenderSettings.skybox = phase3Skybox;
            DynamicGI.UpdateEnvironment();
        }

        if (rainSystem != null)
        {
            rainSystem.gameObject.SetActive(true);
            rainSystem.Play();
            rainEmission = rainSystem.emission;
        }

        if (lightningSystem != null)
        {
            lightningSystem.gameObject.SetActive(true);
        }

        float phase3Elapsed = 0f;
        while (phase3Elapsed < 60f)
        {
            float t = phase3Elapsed / 60f;
            ttfeController.SetSeason(Mathf.Lerp(1f, 2f, t));
            ttfeController.SetWindSpeed(Mathf.Lerp(2f, 3f, t));
            ttfeController.SetWindStrength(Mathf.Lerp(0.5f, 1f, t));

            if (directionalLight != null)
            {
                directionalLight.intensity = Mathf.Lerp(0.6f, 0.3f, t);
            }

            RenderSettings.fogDensity = Mathf.Lerp(0.01f, 0.03f, t);

            if (rainSystem != null)
            {
                rainEmission.rateOverTime = new ParticleSystem.MinMaxCurve(Mathf.Lerp(0f, 3000f, t));
            }

            if (lightningSystem != null && lightningSystem.gameObject.activeInHierarchy && totalElapsed >= nextLightningAt)
            {
                lightningSystem.Play();
                nextLightningAt += Random.Range(10f, 20f);
            }

            phase3Elapsed += Time.deltaTime;
            totalElapsed += Time.deltaTime;
            yield return null;
        }

        // End state: max wind, full fall, heavy rain, dim light, thick fog.
        ttfeController.SetSeason(2f);
        ttfeController.SetWindSpeed(3f);
        ttfeController.SetWindStrength(1f);

        if (directionalLight != null)
        {
            directionalLight.intensity = 0.01f;
        }

        RenderSettings.fogDensity = 0.3f;

        if (rainSystem != null)
        {
            if (!rainSystem.gameObject.activeSelf)
            {
                rainSystem.gameObject.SetActive(true);
            }

            if (!rainSystem.isPlaying)
            {
                rainSystem.Play();
            }

            rainEmission = rainSystem.emission;
            rainEmission.rateOverTime = new ParticleSystem.MinMaxCurve(3000f);
        }

        if (lightningSystem != null)
        {
            if (!lightningSystem.gameObject.activeSelf)
            {
                lightningSystem.gameObject.SetActive(true);
            }

            if (lightningFlashCoroutine != null)
            {
                StopCoroutine(lightningFlashCoroutine);
            }

            lightningFlashCoroutine = StartCoroutine(FlashLightningIntermittently());
        }
    }

    private IEnumerator FlashLightningIntermittently()
    {
        while (lightningSystem != null && lightningSystem.gameObject.activeInHierarchy)
        {
            yield return new WaitForSeconds(Random.Range(10f, 20f));

            if (lightningSystem != null && lightningSystem.gameObject.activeInHierarchy)
            {
                lightningSystem.Play();
            }
        }
    }
}

using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class GardenController : MonoBehaviour
{
    // Attach this to your "GardenRoot" object that controls environment visuals.
    // Expected future responsibilities:
    // - Animate lighting, weather, wind, particles.
    // - Shift season states and vegetation growth.
    // - Adjust color grading/material parameters based on affect.
    public TobyFredson.TobyGlobalShadersController ttfeController;
    public GameObject rainGroup;
    public GameObject lightningGroup;
    public Material phase1Skybox;
    public Material phase2Skybox;
    public Material phase3Skybox;
    public Material neutralSkybox;

    [Header("Skybox Fade")]
    public CanvasGroup fadeCanvasGroup;
    public float skyboxFadeDuration = 0.35f;
    public float skyboxFadeHoldDuration = 0.05f;
    
    [Header("World Lighting")]
    public Light sunLight;
    public Light midDayLight;
    public Light duskLight;

    [Header("Audio")]
    public AudioSource ambienceSource;
    public AudioSource sfxSource;
    public AudioClip jungleClip;
    public AudioClip rainClip;
    public AudioClip lightningClip;
    public float ambienceVolume = 0.6f;
    public float lightningSfxVolume = 1.3f;
    public Vector2 lightningSfxInterval = new Vector2(10f, 20f);
    public float phase3RainSoundDelay = 2f;
    public float phase3LightningDelayAfterRain = 4f;
    public float rainStartLifetime = 8f;

    [Header("Haptics")]
    public HapticsController hapticsController;

    private ParticleSystem rainSystem;
    private ParticleSystem.EmissionModule rainEmission;
    private Coroutine activeGardenSequence;
    private Coroutine lightningFlashCoroutine;
    private Coroutine skyboxFadeCoroutine;
    private string recoveryHapticEventName;
    private const float RainLoopDuration = 20f;

    private void Start()
    {
        CachePrimaryRainSystem();
        ConfigureRainSystem();
        SetRainGroupActive(false);

        SetLightningGroupActive(false);
        InitializeFadeCanvas();

        // Ensure controlled audio startup behavior.
        if (ambienceSource != null)
        {
            ambienceSource.playOnAwake = false;
            ambienceSource.Stop();
        }

        if (sfxSource != null)
        {
            sfxSource.playOnAwake = false;
            sfxSource.Stop();
        }

        // Scene start ambience: low jungle loop.
        if (ambienceSource != null && jungleClip != null)
        {
            ambienceSource.clip = jungleClip;
            ambienceSource.loop = true;
            ambienceSource.volume = 0.05f;
            ambienceSource.Play();
        }
        else if (ambienceSource == null || jungleClip == null)
        {
            Debug.LogWarning("GardenController: ambienceSource or jungleClip missing for scene start ambience.");
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
        StartManagedGardenSequence(RunNonResponsiveSequence());
    }

    public void StartSeasonEscalation()
    {
        StartManagedGardenSequence(RunSeasonEscalationSequence());
    }

    public void StartResponsiveSequence()
    {
        StartManagedGardenSequence(RunResponsiveSequence());
    }

    public void ConfigureRecoveryHaptics(string eventName)
    {
        recoveryHapticEventName = eventName;
    }

    private void StartManagedGardenSequence(IEnumerator sequence)
    {
        StopActiveGardenSequence();
        activeGardenSequence = StartCoroutine(RunManagedGardenSequence(sequence));
    }

    private IEnumerator RunManagedGardenSequence(IEnumerator sequence)
    {
        yield return StartCoroutine(sequence);

        activeGardenSequence = null;
    }

    private void StopActiveGardenSequence()
    {
        if (activeGardenSequence != null)
        {
            StopCoroutine(activeGardenSequence);
            activeGardenSequence = null;
        }

        if (lightningFlashCoroutine != null)
        {
            StopCoroutine(lightningFlashCoroutine);
            lightningFlashCoroutine = null;
        }

        if (skyboxFadeCoroutine != null)
        {
            StopCoroutine(skyboxFadeCoroutine);
            skyboxFadeCoroutine = null;
        }

        InitializeFadeCanvas();
    }

    private IEnumerator RunResponsiveSequence()
    {
        if (ttfeController == null)
        {
            yield break;
        }

        // PHASE 2 TRANSITION (neutral → phase2)

        float t = 0f;
        while (t < 20f)
        {
            float p = t / 20f;

            ttfeController.SetSeason(Mathf.Lerp(0f, 1f, p));
            ttfeController.SetWindSpeed(Mathf.Lerp(2f, 2.5f, p));
            ttfeController.SetWindStrength(Mathf.Lerp(0.5f, 0.7f, p));

            t += Time.deltaTime;
            yield return null;
        }

        if (phase2Skybox != null)
        {
            yield return StartCoroutine(SwapSkyboxWithFade(phase2Skybox));
        }

        yield return new WaitForSeconds(10f);

        // PHASE 3 (phase2 → phase3): use the same escalation implementation as the season sequence.
        float totalElapsed = 0f;
        float nextLightningAt = float.MaxValue;


        if (phase3Skybox != null)
        {
            yield return StartCoroutine(SwapSkyboxWithFade(phase3Skybox, () => SetActiveLight(duskLight)));
        }

        SetRainGroupActive(true);
        ConfigureRainSystem();
        PlayRainSystems();

        SetLightningGroupActive(true);

        float rainDelayElapsed = 0f;
        float clampedRainDelay = Mathf.Max(0f, phase3RainSoundDelay);
        while (rainDelayElapsed < clampedRainDelay)
        {
            float dt = Time.deltaTime;
            rainDelayElapsed += dt;
            totalElapsed += dt;
            yield return null;
        }

        if (ambienceSource != null && rainClip != null)
        {
            ambienceSource.Stop();
            ambienceSource.clip = rainClip;
            ambienceSource.loop = true;
            ambienceSource.volume = 0f;
            ambienceSource.Play();
            StartCoroutine(FadeAmbienceVolume(ambienceVolume, 2f));
        }
        else
        {
            Debug.LogWarning("GardenController: ambienceSource or rainClip missing at Phase 3 start.");
        }

        nextLightningAt = totalElapsed + Mathf.Max(0f, phase3LightningDelayAfterRain);

        float phase3Elapsed = 0f;
        while (phase3Elapsed < 60f)
        {
            float p = phase3Elapsed / 60f;
            ttfeController.SetSeason(Mathf.Lerp(1f, 2f, p));
            ttfeController.SetWindSpeed(Mathf.Lerp(2f, 3f, p));
            ttfeController.SetWindStrength(Mathf.Lerp(0.5f, 1f, p));
            RenderSettings.fogDensity = Mathf.Lerp(0.01f, 0.03f, p);

            SetRainEmissionRate(Mathf.Lerp(0f, 3000f, p));

            if (totalElapsed >= nextLightningAt)
            {
                if (IsLightningGroupActive())
                {
                    PlayLightningSystems();
                }

                PlayLightningSfx();
                nextLightningAt += Random.Range(lightningSfxInterval.x, lightningSfxInterval.y);
            }

            phase3Elapsed += Time.deltaTime;
            totalElapsed += Time.deltaTime;
            yield return null;
        }

        ttfeController.SetSeason(2f);
        ttfeController.SetWindSpeed(3f);
        ttfeController.SetWindStrength(1f);
        RenderSettings.fogDensity = 0.1f;

        SetRainGroupActive(true);
        PlayRainSystems();
        SetRainEmissionRate(3000f);

        if (lightningFlashCoroutine != null)
        {
            StopCoroutine(lightningFlashCoroutine);
        }

        lightningFlashCoroutine = StartCoroutine(FlashLightningIntermittently());

        yield return new WaitForSeconds(10f);

        StopRainSystems();
        SetRainGroupActive(false);

        StopLightningSystems();
        SetLightningGroupActive(false);

        if (lightningFlashCoroutine != null)
        {
            StopCoroutine(lightningFlashCoroutine);
            lightningFlashCoroutine = null;
        }

        if (ambienceSource != null)
        {
            ambienceSource.Stop();
        }

        if (sfxSource != null)
        {
            sfxSource.Stop();
        }

        // RECOVERY (phase3 → phase1)
        PlayRecoveryHapticIfConfigured();

        t = 0f;
        while (t < 20f)
        {
            float p = t / 20f;

            ttfeController.SetSeason(Mathf.Lerp(2f, -1f, p));
            ttfeController.SetWindSpeed(Mathf.Lerp(3f, 2f, p));
            ttfeController.SetWindStrength(Mathf.Lerp(1f, 0.5f, p));
            RenderSettings.fogDensity = Mathf.Lerp(0.03f, 0f, p);

            t += Time.deltaTime;
            yield return null;
        }

        RenderSettings.fogDensity = 0f;

        if (phase1Skybox != null)
        {
            yield return StartCoroutine(SwapSkyboxWithFade(phase1Skybox, () => SetActiveLight(sunLight)));
        }
        else
        {
            SetActiveLight(sunLight);
        }
         
        if (ambienceSource != null && jungleClip != null)
        {
            ambienceSource.Stop();
            ambienceSource.clip = jungleClip;
            ambienceSource.loop = true;
            ambienceSource.volume = 0.05f;
            ambienceSource.Play();
        }

        recoveryHapticEventName = null;
    }

    private void SetActiveLight(Light activeLight)
    {
        if (sunLight != null) sunLight.gameObject.SetActive(false);
        if (midDayLight != null) midDayLight.gameObject.SetActive(false);
        if (duskLight != null) duskLight.gameObject.SetActive(false);

        if (activeLight != null)
            activeLight.gameObject.SetActive(true);
    }

    public void PlayLightningSfx()
    {
        if (sfxSource == null)
        {
            Debug.LogWarning("GardenController: sfxSource is not assigned.");
            return;
        }

        if (lightningClip == null)
        {
            Debug.LogWarning("GardenController: lightningClip is not assigned.");
            return;
        }

        sfxSource.PlayOneShot(lightningClip, lightningSfxVolume);
    }

    public void ResetGardenToNeutral()
    {
        StopActiveGardenSequence();
        recoveryHapticEventName = null;

        if (neutralSkybox != null)
        {
            SwapSkyboxWithFadeImmediate(neutralSkybox);
        }

        StopRainSystems();
        SetRainGroupActive(false);

        StopLightningSystems();
        SetLightningGroupActive(false);

        if (ttfeController != null)
        {
            ttfeController.SetSeason(0f);
            ttfeController.SetWindSpeed(2f);
            ttfeController.SetWindStrength(0.5f);
        }

        RenderSettings.fogDensity = 0f;

        SetActiveLight(sunLight);

        SetRainEmissionRate(0f);

        if (ambienceSource != null && jungleClip != null)
        {
            ambienceSource.Stop();
            ambienceSource.clip = jungleClip;
            ambienceSource.loop = true;
            ambienceSource.volume = 0.01f;
            ambienceSource.Play();
        }
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
        float nextLightningAt = float.MaxValue;

        // Phase 1 (0-30s): calm baseline.
        // Keep currently playing jungle ambience from transition setup.

        if (phase1Skybox != null)
        {
            yield return StartCoroutine(SwapSkyboxWithFade(phase1Skybox));
        }

        SetRainGroupActive(false);

        SetLightningGroupActive(false);

        ttfeController.SetSeason(-1f);
        ttfeController.SetWindSpeed(1f);
        ttfeController.SetWindStrength(0.1f);

        SetActiveLight(sunLight);

        RenderSettings.fogDensity = 0f;

        SetRainEmissionRate(0f);

        while (totalElapsed < 30f)
        {
            if (IsRainGroupActive())
            {
                SetRainGroupActive(false);
            }

            if (IsLightningGroupActive())
            {
                SetLightningGroupActive(false);
            }

            totalElapsed += Time.deltaTime;
            yield return null;
        }

        // Phase 2 (30-60s): moderate escalation.
        if (ambienceSource == null)
        {
            Debug.LogWarning("GardenController: ambienceSource missing at Phase 2 start.");
        }
        
        SetActiveLight(midDayLight);

        if (phase2Skybox != null)
        {
            yield return StartCoroutine(SwapSkyboxWithFade(phase2Skybox));
        }

        SetRainGroupActive(false);

        SetLightningGroupActive(false);

        float phase2Elapsed = 0f;
        while (phase2Elapsed < 30f)
        {
            if (IsRainGroupActive())
            {
                SetRainGroupActive(false);
            }

            if (IsLightningGroupActive())
            {
                SetLightningGroupActive(false);
            }

            float t = phase2Elapsed / 30f;
            ttfeController.SetSeason(Mathf.Lerp(-1f, 1f, t));
            ttfeController.SetWindSpeed(Mathf.Lerp(1f, 2f, t));
            ttfeController.SetWindStrength(Mathf.Lerp(0.1f, 0.5f, t));

            RenderSettings.fogDensity = Mathf.Lerp(0f, 0.01f, t);

            phase2Elapsed += Time.deltaTime;
            totalElapsed += Time.deltaTime;
            yield return null;
        }

        // Phase 3 (60-120s): full escalation.
        SetActiveLight(duskLight);

        // 1) Swap skybox first.
        if (phase3Skybox != null)
        {
            yield return StartCoroutine(SwapSkyboxWithFade(phase3Skybox));
        }

        SetRainGroupActive(true);
        ConfigureRainSystem();
        PlayRainSystems();

        SetLightningGroupActive(true);

        // 2) Wait a bit before starting rain ambience.
        float rainDelayElapsed = 0f;
        float clampedRainDelay = Mathf.Max(0f, phase3RainSoundDelay);
        while (rainDelayElapsed < clampedRainDelay)
        {
            float dt = Time.deltaTime;
            rainDelayElapsed += dt;
            totalElapsed += dt;
            yield return null;
        }

        if (ambienceSource != null && rainClip != null)
        {
            ambienceSource.Stop();
            ambienceSource.clip = rainClip;
            ambienceSource.loop = true;
            ambienceSource.volume = 0f;
            ambienceSource.Play();
            StartCoroutine(FadeAmbienceVolume(ambienceVolume, 2f));
        }
        else
        {
            Debug.LogWarning("GardenController: ambienceSource or rainClip missing at Phase 3 start.");
        }

        // 3) Delay first lightning after rain ambience starts.
        nextLightningAt = totalElapsed + Mathf.Max(0f, phase3LightningDelayAfterRain);

        float phase3Elapsed = 0f;
        while (phase3Elapsed < 60f)
        {
            float t = phase3Elapsed / 60f;
            ttfeController.SetSeason(Mathf.Lerp(1f, 2f, t));
            ttfeController.SetWindSpeed(Mathf.Lerp(2f, 3f, t));
            ttfeController.SetWindStrength(Mathf.Lerp(0.5f, 1f, t));

            RenderSettings.fogDensity = Mathf.Lerp(0.01f, 0.03f, t);

            SetRainEmissionRate(Mathf.Lerp(0f, 3000f, t));

            if (totalElapsed >= nextLightningAt)
            {
                if (IsLightningGroupActive())
                {
                    PlayLightningSystems();
                }

                // Play SFX with particle event when available, or independently if no lightning system exists.
                PlayLightningSfx();
                nextLightningAt += Random.Range(lightningSfxInterval.x, lightningSfxInterval.y);
            }

            phase3Elapsed += Time.deltaTime;
            totalElapsed += Time.deltaTime;
            yield return null;
        }

        // End state: max wind, full fall, heavy rain, dim light, thick fog.
        ttfeController.SetSeason(2f);
        ttfeController.SetWindSpeed(3f);
        ttfeController.SetWindStrength(1f);

        RenderSettings.fogDensity = 0.1f;

        SetRainGroupActive(true);
        PlayRainSystems();
        SetRainEmissionRate(3000f);

        if (lightningFlashCoroutine != null)
        {
            StopCoroutine(lightningFlashCoroutine);
        }

        lightningFlashCoroutine = StartCoroutine(FlashLightningIntermittently());
    }

    private IEnumerator FlashLightningIntermittently()
    {
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(lightningSfxInterval.x, lightningSfxInterval.y));

            bool playedVisual = false;
            if (IsLightningGroupActive())
            {
                PlayLightningSystems();
                playedVisual = true;
            }

            if (playedVisual || lightningGroup == null)
            {
                PlayLightningSfx();
            }
        }
    }

    private IEnumerator FadeAmbienceVolume(float targetVolume, float duration)
    {
        if (ambienceSource == null)
        {
            yield break;
        }

        float elapsed = 0f;
        float startVolume = ambienceSource.volume;
        float clampedDuration = Mathf.Max(0.01f, duration);

        while (elapsed < clampedDuration)
        {
            float t = elapsed / clampedDuration;
            ambienceSource.volume = Mathf.Lerp(startVolume, targetVolume, t);
            elapsed += Time.deltaTime;
            yield return null;
        }

        ambienceSource.volume = targetVolume;
    }

    private void InitializeFadeCanvas()
    {
        if (fadeCanvasGroup == null)
        {
            return;
        }

        fadeCanvasGroup.alpha = 0f;
        fadeCanvasGroup.gameObject.SetActive(false);
    }

    private IEnumerator SwapSkyboxWithFade(Material targetSkybox)
    {
        yield return StartCoroutine(SwapSkyboxWithFade(targetSkybox, null));
    }

    private IEnumerator SwapSkyboxWithFade(Material targetSkybox, System.Action onBlack)
    {
        if (targetSkybox == null)
        {
            yield break;
        }

        if (fadeCanvasGroup == null)
        {
            ApplySkybox(targetSkybox);
            yield break;
        }

        if (skyboxFadeCoroutine != null)
        {
            StopCoroutine(skyboxFadeCoroutine);
            skyboxFadeCoroutine = null;
        }

        yield return StartCoroutine(FadeCanvasAlpha(0f, 1f, skyboxFadeDuration));
        ApplySkybox(targetSkybox);
        onBlack?.Invoke();

        if (skyboxFadeHoldDuration > 0f)
        {
            yield return new WaitForSeconds(skyboxFadeHoldDuration);
        }

        yield return StartCoroutine(FadeCanvasAlpha(1f, 0f, skyboxFadeDuration));
        skyboxFadeCoroutine = null;
    }

    private void SwapSkyboxWithFadeImmediate(Material targetSkybox)
    {
        if (targetSkybox == null)
        {
            return;
        }

        if (fadeCanvasGroup == null)
        {
            ApplySkybox(targetSkybox);
            return;
        }

        if (skyboxFadeCoroutine != null)
        {
            StopCoroutine(skyboxFadeCoroutine);
        }

        skyboxFadeCoroutine = StartCoroutine(SwapSkyboxWithFade(targetSkybox));
    }

    private IEnumerator FadeCanvasAlpha(float startAlpha, float endAlpha, float duration)
    {
        if (fadeCanvasGroup == null)
        {
            yield break;
        }

        fadeCanvasGroup.gameObject.SetActive(true);
        fadeCanvasGroup.alpha = startAlpha;

        float elapsed = 0f;
        float clampedDuration = Mathf.Max(0.01f, duration);

        while (elapsed < clampedDuration)
        {
            float t = elapsed / clampedDuration;
            fadeCanvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, t);
            elapsed += Time.deltaTime;
            yield return null;
        }

        fadeCanvasGroup.alpha = endAlpha;

        if (Mathf.Approximately(endAlpha, 0f))
        {
            fadeCanvasGroup.gameObject.SetActive(false);
        }
    }

    private void ApplySkybox(Material targetSkybox)
    {
        RenderSettings.skybox = targetSkybox;
        DynamicGI.UpdateEnvironment();
    }

    private void PlayRecoveryHapticIfConfigured()
    {
        if (string.IsNullOrWhiteSpace(recoveryHapticEventName))
        {
            return;
        }

        if (hapticsController == null)
        {
            Debug.LogWarning("GardenController: HapticsController is not assigned for recovery haptics.");
            return;
        }

        hapticsController.PlayHaptic(recoveryHapticEventName);
    }

    private void CachePrimaryRainSystem()
    {
        ParticleSystem[] rainSystems = GetRainSystems();
        rainSystem = rainSystems.Length > 0 ? rainSystems[0] : null;
    }

    private void ConfigureRainSystem()
    {
        CachePrimaryRainSystem();

        if (rainSystem != null)
        {
            var rainMain = rainSystem.main;
            rainMain.duration = RainLoopDuration;
            rainMain.loop = true;
            rainMain.stopAction = ParticleSystemStopAction.None;
            rainMain.startLifetime = rainStartLifetime;
            rainEmission = rainSystem.emission;
        }
    }

    private void PlayRainSystems()
    {
        foreach (ParticleSystem rainSystem in GetRainSystems())
        {
            if (!rainSystem.isPlaying)
            {
                rainSystem.Play();
            }
        }
    }

    private void StopRainSystems()
    {
        foreach (ParticleSystem rainSystem in GetRainSystems())
        {
            rainSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }

    private void SetRainEmissionRate(float rate)
    {
        CachePrimaryRainSystem();

        if (rainSystem != null)
        {
            rainEmission = rainSystem.emission;
            rainEmission.rateOverTime = new ParticleSystem.MinMaxCurve(rate);
        }
    }

    private ParticleSystem[] GetRainSystems()
    {
        return rainGroup != null
            ? rainGroup.GetComponentsInChildren<ParticleSystem>(true)
            : System.Array.Empty<ParticleSystem>();
    }

    private void SetRainGroupActive(bool isActive)
    {
        if (rainGroup != null && rainGroup.activeSelf != isActive)
        {
            rainGroup.SetActive(isActive);
        }
    }

    private bool IsRainGroupActive()
    {
        return rainGroup != null && rainGroup.activeSelf;
    }

    private ParticleSystem[] GetLightningSystems()
    {
        return lightningGroup != null
            ? lightningGroup.GetComponentsInChildren<ParticleSystem>(true)
            : System.Array.Empty<ParticleSystem>();
    }

    private void PlayLightningSystems()
    {
        foreach (ParticleSystem lightningSystem in GetLightningSystems())
        {
            lightningSystem.Play();
        }
    }

    private void StopLightningSystems()
    {
        foreach (ParticleSystem lightningSystem in GetLightningSystems())
        {
            lightningSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }

    private void SetLightningGroupActive(bool isActive)
    {
        if (lightningGroup != null && lightningGroup.activeSelf != isActive)
        {
            lightningGroup.SetActive(isActive);
        }
    }

    private bool IsLightningGroupActive()
    {
        return lightningGroup != null && lightningGroup.activeSelf;
    }
}

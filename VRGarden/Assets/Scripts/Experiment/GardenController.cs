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
}

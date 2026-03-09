using UnityEngine;

public class HapticVestController : MonoBehaviour
{
    public void PlaySubduedPattern()
    {
        Debug.Log("Haptics: subdued vibration pattern started.");
    }

    public void PlayCheerfulPattern()
    {
        Debug.Log("Haptics: cheerful vibration pattern started.");
    }

    public void PlayRecoveryPattern()
    {
        Debug.Log("Haptics: recovery vibration pattern started.");
    }

    public void StopHaptics()
    {
        Debug.Log("Haptics: stopped.");
    }
}

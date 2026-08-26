using UnityEngine;

[RequireComponent(typeof(AudioLowPassFilter))]
public class PlayerAudioEffects : MonoBehaviour
{
    private AudioLowPassFilter lowPassFilter;
    private PlayerController playerController;

    private void Awake()
    {
        lowPassFilter = GetComponent<AudioLowPassFilter>();
        playerController = GetComponentInParent<PlayerController>();

        if (lowPassFilter != null)
        {
            lowPassFilter.enabled = false; 
            lowPassFilter.cutoffFrequency = 900f; 
        }
    }

    private void Update()
    {
        if (playerController == null || lowPassFilter == null) return;

        if (playerController.isHiding.Value)
        {
            if (!lowPassFilter.enabled) lowPassFilter.enabled = true;
        }
        else
        {
            if (lowPassFilter.enabled) lowPassFilter.enabled = false;
        }
    }
}
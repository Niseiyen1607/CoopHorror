using UnityEngine;

public class SceneMusic : MonoBehaviour
{
    public AudioClip ambienceClip;
    [Range(0f, 1f)] public float volume = 0.5f;

    void Start()
    {
        if (AudioManager.Instance != null && ambienceClip != null)
        {
            AudioManager.Instance.PlayAmbience(ambienceClip, volume);
        }
    }
}
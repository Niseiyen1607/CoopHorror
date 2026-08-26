using UnityEngine;
using UnityEngine.Audio;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Mixer")]
    public AudioMixer mainMixer;
    public AudioMixerGroup sfxGroup;
    public AudioMixerGroup ambienceGroup;
    public AudioMixerGroup uiGroup;

    [Header("Sources Audio d'Ambiance")]
    public AudioSource ambienceSource;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void PlaySound2D(AudioClip clip, float volume = 1f, float pitchRandomness = 0.1f)
    {
        if (clip == null) return;

        GameObject tempGO = new GameObject("Temp2DSound_" + clip.name);
        AudioSource source = tempGO.AddComponent<AudioSource>();
        
        source.outputAudioMixerGroup = uiGroup;
        source.clip = clip;
        source.volume = volume;
        source.pitch = 1f + Random.Range(-pitchRandomness, pitchRandomness); 
        source.spatialBlend = 0f; 

        source.Play();
        Destroy(tempGO, clip.length + 0.2f);
    }

    public void PlaySound3D(AudioClip clip, Vector3 position, float volume = 1f, float minDistance = 1.5f, float maxDistance = 25f, float pitchRandomness = 0.1f)
    {
        if (clip == null) return;

        GameObject tempGO = new GameObject("Temp3DSound_" + clip.name);
        tempGO.transform.position = position;

        AudioSource source = tempGO.AddComponent<AudioSource>();
        source.outputAudioMixerGroup = sfxGroup;
        source.clip = clip;
        source.volume = volume;
        source.pitch = 1f + Random.Range(-pitchRandomness, pitchRandomness);
        
        source.spatialBlend = 1f; 
        source.minDistance = minDistance;
        source.maxDistance = maxDistance;
        source.rolloffMode = AudioRolloffMode.Logarithmic;

        source.Play();
        Destroy(tempGO, (clip.length / Mathf.Max(0.1f, source.pitch)) + 0.2f);
    }

    public void PlayAmbience(AudioClip ambienceClip, float volume = 0.4f)
    {
        if (ambienceSource == null || ambienceClip == null) return;

        ambienceSource.outputAudioMixerGroup = ambienceGroup;
        ambienceSource.clip = ambienceClip;
        ambienceSource.volume = volume;
        ambienceSource.loop = true;
        ambienceSource.Play();
    }
}
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
    public AudioMixerGroup voiceGroup;

    [Header("Acoustique & Snapshots d'Ambiance")]
    public AudioMixerSnapshot defaultSnapshot;       
    public AudioMixerSnapshot metalHallwaySnapshot;  
    public AudioMixerSnapshot largeRoomSnapshot;     
    public AudioMixerSnapshot lockerMuffledSnapshot; 
    public AudioMixerSnapshot startRoomSnapshot;

    [Header("Sources Audio")]
    public AudioSource ambienceSource;

    private AudioSource activeVoiceSource;
    private AudioMixerSnapshot currentActiveSnapshot;

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

    private void Start()
    {
        if (defaultSnapshot != null)
        {
            defaultSnapshot.TransitionTo(0.1f);
            currentActiveSnapshot = defaultSnapshot;
        }
    }

    public void SetRoomAcoustics(AudioMixerSnapshot targetSnapshot, float transitionTime = 0.35f)
    {
        if (targetSnapshot == null || targetSnapshot == currentActiveSnapshot) return;

        targetSnapshot.TransitionTo(transitionTime);
        currentActiveSnapshot = targetSnapshot;
    }

    public void ResetToDefaultAcoustics(float transitionTime = 0.35f)
    {
        if (defaultSnapshot != null)
        {
            SetRoomAcoustics(defaultSnapshot, transitionTime);
        }
    }

    public void PlayVoiceOver2D(AudioClip clip, float volume = 1f)
    {
        if (clip == null) return;

        if (activeVoiceSource != null)
        {
            activeVoiceSource.Stop();
            Destroy(activeVoiceSource.gameObject);
        }

        GameObject tempGO = new GameObject("VoiceOver_" + clip.name);
        activeVoiceSource = tempGO.AddComponent<AudioSource>();
        
        activeVoiceSource.outputAudioMixerGroup = uiGroup;
        activeVoiceSource.clip = clip;
        activeVoiceSource.volume = volume;
        activeVoiceSource.pitch = 1f; 
        activeVoiceSource.spatialBlend = 0f;

        activeVoiceSource.Play();
        Destroy(tempGO, clip.length + 0.2f);
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

    public void PlayVoice3D(AudioClip clip, Vector3 position, float volume = 1f, float minDistance = 1.5f, float maxDistance = 25f)
    {
        if (clip == null) return;

        GameObject tempGO = new GameObject("Temp3DVoice_" + clip.name);
        tempGO.transform.position = position;

        AudioSource source = tempGO.AddComponent<AudioSource>();
        source.outputAudioMixerGroup = voiceGroup;
        source.clip = clip;
        source.volume = volume;
        source.spatialBlend = 1f; 
        source.minDistance = minDistance;
        source.maxDistance = maxDistance;
        source.rolloffMode = AudioRolloffMode.Logarithmic;

        source.Play();
        Destroy(tempGO, clip.length + 0.2f);
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

    public void StopAmbience()
    {
        if (ambienceSource != null)
        {
            ambienceSource.Stop();
        }
    }
}
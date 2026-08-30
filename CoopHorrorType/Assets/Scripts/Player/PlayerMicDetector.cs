using Unity.Netcode;
using UnityEngine;

public class PlayerMicDetector : NetworkBehaviour
{
    [Header("Sensibilité du Micro")]
    [Tooltip("Seuil de déclenchement de la parole")]
    [Range(0.005f, 0.2f)]
    public float speechThreshold = 0.035f;

    [Tooltip("Vitesse de lissage du volume sonore")]
    public float smoothness = 15f;

    [Header("Temps de maintien")]
    public float voiceRetentionTime = 0.35f; 

    [HideInInspector] public NetworkVariable<bool> isSpeaking = new NetworkVariable<bool>(
        false, 
        NetworkVariableReadPermission.Everyone, 
        NetworkVariableWritePermission.Server
    );

    public float CurrentLoudness { get; private set; } = 0f;

    private AudioClip micClip;
    private string micDevice;
    private float retentionTimer = 0f;
    private const int SAMPLE_WINDOW = 512;
    private float[] waveData = new float[SAMPLE_WINDOW];

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            InitMicrophone();
        }
    }

    public override void OnNetworkDespawn()
    {
        if (IsOwner && !string.IsNullOrEmpty(micDevice))
        {
            Microphone.End(micDevice);
        }
    }

    private void InitMicrophone()
    {
        if (Microphone.devices.Length > 0)
        {
            micDevice = Microphone.devices[0];
            micClip = Microphone.Start(micDevice, true, 10, 44100);
            Debug.Log($"<color=cyan>[MICRO] Micro activé : {micDevice}</color>");
        }
        else
        {
            Debug.LogWarning("[MICRO] Aucun microphone détecté sur cet appareil.");
        }
    }

    void Update()
    {
        if (!IsOwner) return;

        AnalyzeVoice();
    }

    private void AnalyzeVoice()
    {
        if (micClip == null || string.IsNullOrEmpty(micDevice)) return;

        int micPosition = Microphone.GetPosition(micDevice) - SAMPLE_WINDOW + 1;
        if (micPosition < 0) return;

        micClip.GetData(waveData, micPosition);

        float sum = 0f;
        for (int i = 0; i < SAMPLE_WINDOW; i++)
        {
            sum += waveData[i] * waveData[i];
        }
        float rawLoudness = Mathf.Sqrt(sum / SAMPLE_WINDOW);

        CurrentLoudness = Mathf.Lerp(CurrentLoudness, rawLoudness, Time.deltaTime * smoothness);

        if (CurrentLoudness > speechThreshold)
        {
            retentionTimer = voiceRetentionTime;

            if (!isSpeaking.Value)
            {
                SetSpeakingServerRpc(true);
            }
        }
        else
        {
            if (retentionTimer > 0f)
            {
                retentionTimer -= Time.deltaTime;
            }
            else if (isSpeaking.Value)
            {
                SetSpeakingServerRpc(false);
            }
        }

        if (VoiceHUD.Instance != null)
        {
            VoiceHUD.Instance.UpdateVoiceLevel(isSpeaking.Value, CurrentLoudness / (speechThreshold * 3f));
        }
    }

    [ServerRpc]
    private void SetSpeakingServerRpc(bool talking)
    {
        isSpeaking.Value = talking;
    }
}
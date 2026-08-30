using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(AudioLowPassFilter))]
public class PlayerMicDetector : NetworkBehaviour
{
    [Range(0.005f, 0.2f)]
    public float speechThreshold = 0.035f;

    public float smoothness = 15f;

    public float voiceRetentionTime = 0.35f;

    public float normalCutoffFrequency = 22000f;
    public float muffledCutoffFrequency = 750f;   

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

    private AudioLowPassFilter lowPassFilter;
    private PlayerController playerController;

    private void Awake()
    {
        lowPassFilter = GetComponent<AudioLowPassFilter>();
        playerController = GetComponent<PlayerController>();
        if (lowPassFilter != null) lowPassFilter.cutoffFrequency = normalCutoffFrequency;
    }

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            InitMicrophone();
        }

        if (playerController != null)
        {
            playerController.isDead.OnValueChanged += OnDeathStateChanged;
            playerController.isHiding.OnValueChanged += OnHidingStateChanged;
        }
    }

    public override void OnNetworkDespawn()
    {
        if (IsOwner && !string.IsNullOrEmpty(micDevice))
        {
            Microphone.End(micDevice);
        }

        if (playerController != null)
        {
            playerController.isDead.OnValueChanged -= OnDeathStateChanged;
            playerController.isHiding.OnValueChanged -= OnHidingStateChanged;
        }
    }

    private void OnDeathStateChanged(bool wasDead, bool isDeadNow)
    {
        if (isDeadNow)
        {
            CurrentLoudness = 0f;
            if (IsOwner && !string.IsNullOrEmpty(micDevice))
            {
                Microphone.End(micDevice); 
            }
            if (IsServer)
            {
                isSpeaking.Value = false;
            }
            else
            {
                SetSpeakingServerRpc(false);
            }
        }
        else if (IsOwner && !isDeadNow)
        {
            InitMicrophone();
        }
    }

    private void OnHidingStateChanged(bool wasHiding, bool isHidingNow)
    {
        if (lowPassFilter != null)
        {
            lowPassFilter.cutoffFrequency = isHidingNow ? muffledCutoffFrequency : normalCutoffFrequency;
        }
    }

    private void InitMicrophone()
    {
        if (Microphone.devices.Length > 0)
        {
            micDevice = Microphone.devices[0];
            micClip = Microphone.Start(micDevice, true, 10, 44100);
        }
    }

    void Update()
    {
        if (!IsOwner) return;
        if (playerController != null && playerController.isDead.Value) return;

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
            if (!isSpeaking.Value) SetSpeakingServerRpc(true);
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
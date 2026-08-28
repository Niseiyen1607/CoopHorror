using Unity.Netcode;
using UnityEngine;

public class PlayerMicDetector : NetworkBehaviour
{
    [Header("Réglages Micro / Voix")]
    public float micSensitivityThreshold = 0.04f; 
    
    [HideInInspector] public NetworkVariable<bool> isSpeaking = new NetworkVariable<bool>(false);

    private AudioClip micClip;
    private string micDevice;
    private float silenceTimer = 0f;

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            InitNativeMicrophone();
        }
    }

    private void InitNativeMicrophone()
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
        CheckMicrophoneVolume();
    }

    private void CheckMicrophoneVolume()
    {
        if (micClip == null || string.IsNullOrEmpty(micDevice)) return;

        int sampleWindow = 128;
        float[] waveData = new float[sampleWindow];
        int micPosition = Microphone.GetPosition(micDevice) - sampleWindow + 1;
        if (micPosition < 0) return;

        micClip.GetData(waveData, micPosition);

        float sum = 0f;
        for (int i = 0; i < sampleWindow; i++)
        {
            sum += waveData[i] * waveData[i];
        }
        float rmsLoudness = Mathf.Sqrt(sum / sampleWindow);

        if (rmsLoudness > micSensitivityThreshold)
        {
            silenceTimer = 0.3f; 
            if (!isSpeaking.Value)
            {
                SetSpeakingServerRpc(true);
            }
        }
        else
        {
            if (silenceTimer > 0f)
            {
                silenceTimer -= Time.deltaTime;
            }
            else if (isSpeaking.Value)
            {
                SetSpeakingServerRpc(false);
            }
        }
    }

    private void OnGUI()
    {
        if (!IsOwner) return;

        if (isSpeaking.Value)
        {
            GUIStyle style = new GUIStyle(GUI.skin.label);
            style.fontSize = 18;
            style.fontStyle = FontStyle.Bold;
            style.normal.textColor = Color.green;

            GUI.Label(new Rect(20, Screen.height - 40, 250, 35), " MICRO : ACTIF", style);
        }
    }

    [ServerRpc]
    private void SetSpeakingServerRpc(bool talking)
    {
        isSpeaking.Value = talking;
    }
}
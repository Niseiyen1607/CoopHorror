using System;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Vivox;
using UnityEngine;

public class VoiceChatManager : MonoBehaviour
{
    public static VoiceChatManager Instance { get; private set; }

    [Header("Réglages Proximité (Lethal Company)")]
    public int maxDistance = 22;
    public int minDistance = 1;  

    private string currentChannelName;

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

    private async void Start()
    {
        await InitializeVivoxAsync();
    }

    private async Task InitializeVivoxAsync()
    {
        try
        {
            if (UnityServices.State == ServicesInitializationState.Uninitialized)
            {
                await UnityServices.InitializeAsync();
            }

            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            }

            await VivoxService.Instance.InitializeAsync();
            await VivoxService.Instance.LoginAsync();

            Debug.Log("<color=green>[VOICE] Vivox initialisé et connecté !</color>");
        }
        catch (Exception e)
        {
            Debug.LogError($"[VOICE] Erreur Vivox : {e.Message}");
        }
    }

    public async void Join3DVoiceChannel(string relayCode)
    {
        currentChannelName = "Voice_" + relayCode;

        Channel3DProperties properties = new Channel3DProperties(
            maxDistance,
            minDistance,
            1.0f,
            AudioFadeModel.InverseByDistance
        );

        await VivoxService.Instance.JoinPositionalChannelAsync(
            currentChannelName,
            ChatCapability.AudioOnly,
            properties
        );

        Debug.Log($"<color=cyan>[VOICE] Rejoint le canal vocal 3D : {currentChannelName}</color>");
    }

    public void UpdateVoicePosition(Vector3 position, Vector3 forward, Vector3 up)
    {
        if (VivoxService.Instance != null && VivoxService.Instance.IsLoggedIn && !string.IsNullOrEmpty(currentChannelName))
        {
            VivoxService.Instance.Set3DPosition(position, forward, up, position, currentChannelName);
        }
    }

    public async void LeaveVoiceChannel()
    {
        if (!string.IsNullOrEmpty(currentChannelName))
        {
            await VivoxService.Instance.LeaveChannelAsync(currentChannelName);
        }
    }
}
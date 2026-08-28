using TMPro;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class PlayerNameplate : NetworkBehaviour
{
    [Header("Références Nameplate")]
    public TextMeshPro nameText;       
    public Transform nameplateHolder;  

    [Header("Indicateur Vocal")]
    public GameObject speakingIcon;   

    private PlayerController playerController;
    private PlayerMicDetector micDetector;

    private void Awake()
    {
        playerController = GetComponent<PlayerController>();
        micDetector = GetComponent<PlayerMicDetector>();
    }

    public override void OnNetworkSpawn()
    {
        if (playerController != null)
        {
            playerController.playerName.OnValueChanged += OnNameChanged;
        }

        if (micDetector != null)
        {
            micDetector.isSpeaking.OnValueChanged += OnSpeakingChanged;
            if (speakingIcon != null)
            {
                speakingIcon.SetActive(micDetector.isSpeaking.Value);
            }
        }

        if (IsOwner && nameplateHolder != null)
        {
            nameplateHolder.gameObject.SetActive(false);
        }
        else if (playerController != null)
        {
            UpdateNameText(playerController.playerName.Value.ToString());
        }
    }

    private void OnNameChanged(FixedString32Bytes oldName, FixedString32Bytes newName)
    {
        UpdateNameText(newName.ToString());
    }

    private void OnSpeakingChanged(bool previousValue, bool isTalking)
    {
        if (speakingIcon != null)
        {
            speakingIcon.SetActive(isTalking);
        }
    }

    private void UpdateNameText(string newName)
    {
        if (nameText != null)
        {
            nameText.text = newName;
        }
    }

    private void LateUpdate()
    {
        if (!IsOwner && nameplateHolder != null && Camera.main != null)
        {
            nameplateHolder.LookAt(nameplateHolder.position + Camera.main.transform.rotation * Vector3.forward,
                                 Camera.main.transform.rotation * Vector3.up);
        }
    }

    public override void OnNetworkDespawn()
    {
        if (playerController != null)
        {
            playerController.playerName.OnValueChanged -= OnNameChanged;
        }

        if (micDetector != null)
        {
            micDetector.isSpeaking.OnValueChanged -= OnSpeakingChanged;
        }
    }
}
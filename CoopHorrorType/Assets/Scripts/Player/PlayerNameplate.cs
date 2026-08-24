using TMPro;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class PlayerNameplate : NetworkBehaviour
{
    [Header("Références")]
    public TextMeshPro nameText;      
    public Transform nameplateHolder;  

    private PlayerController playerController;

    private void Awake()
    {
        playerController = GetComponent<PlayerController>();
    }

    public override void OnNetworkSpawn()
    {
        playerController.playerName.OnValueChanged += OnNameChanged;

        if (IsOwner && nameplateHolder != null)
        {
            nameplateHolder.gameObject.SetActive(false);
        }
        else
        {
            UpdateNameText(playerController.playerName.Value.ToString());
        }
    }

    private void OnNameChanged(FixedString32Bytes oldName, FixedString32Bytes newName)
    {
        UpdateNameText(newName.ToString());
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
}
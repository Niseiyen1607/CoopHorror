using Unity.Netcode;
using UnityEngine;

public class PlayerFlashlight : NetworkBehaviour
{
    public Light headLight;         
    public AudioSource clickSound;  

    public NetworkVariable<bool> isFlashlightOn = new NetworkVariable<bool>(false);

    public override void OnNetworkSpawn()
    {
        isFlashlightOn.OnValueChanged += OnFlashlightStateChanged;

        if (headLight != null)
        {
            headLight.enabled = isFlashlightOn.Value;
        }
    }

    private void Update()
    {
        if (!IsOwner) return;

        if (Input.GetKeyDown(KeyCode.F))
        {
            ToggleFlashlightServerRpc(!isFlashlightOn.Value);
        }
    }

    [ServerRpc]
    private void ToggleFlashlightServerRpc(bool state)
    {
        isFlashlightOn.Value = state;
    }

    private void OnFlashlightStateChanged(bool previousValue, bool newValue)
    {
        if (headLight != null)
        {
            headLight.enabled = newValue;
        }

        if (clickSound != null)
        {
            clickSound.Play();
        }
    }

    public override void OnDestroy()
    {
        base.OnDestroy();

        if (isFlashlightOn != null)
        {
            isFlashlightOn.OnValueChanged -= OnFlashlightStateChanged;
        }
    }
}
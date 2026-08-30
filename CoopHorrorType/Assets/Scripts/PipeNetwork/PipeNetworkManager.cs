using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PipeNetworkManager : NetworkBehaviour
{
    public string circuitName = "Circuit Sous-sol";
    public List<PipeSocket> socketsInCircuit = new List<PipeSocket>();

    public GameObject waterLeakParticles; 
    public AudioSource leakSound;         

    public NetworkVariable<bool> isCircuitCompleted = new NetworkVariable<bool>(false);

    public override void OnNetworkSpawn()
    {
        isCircuitCompleted.OnValueChanged += OnCircuitStateChanged;
    }

    public void CheckCircuitCompletion()
    {
        if (!IsServer) return;

        bool allConnected = true;

        foreach (PipeSocket socket in socketsInCircuit)
        {
            if (socket == null || !socket.IsFixedCorrectly())
            {
                allConnected = false;
                break;
            }
        }

        if (allConnected != isCircuitCompleted.Value)
        {
            isCircuitCompleted.Value = allConnected;

            if (allConnected)
            {
                Debug.Log($"<color=green> CIRCUIT '{circuitName}' reparer </color>");
            }
        }
    }

    private void OnCircuitStateChanged(bool previousValue, bool newValue)
    {
        if (newValue == true)
        {
            if (waterLeakParticles != null) waterLeakParticles.SetActive(false); 
            if (leakSound != null) leakSound.Stop();                            
        }
        else
        {
            if (waterLeakParticles != null) waterLeakParticles.SetActive(true);
            if (leakSound != null) if (!leakSound.isPlaying) leakSound.Play();
        }
    }
}
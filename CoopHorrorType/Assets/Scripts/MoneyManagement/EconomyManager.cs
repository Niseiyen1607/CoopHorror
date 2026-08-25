using Unity.Netcode;
using UnityEngine;

public class EconomyManager : NetworkBehaviour
{
    public static EconomyManager Instance { get; private set; }

    [Header("Objectif de Mission")]
    public int moneyGoal = 500;
    
    public NetworkVariable<int> currentMoney = new NetworkVariable<int>(0);

    [Header("Audio")]
    public AudioSource moneySound; 

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void AddMoney(int amount)
    {
        if (!IsServer) return;

        currentMoney.Value += amount;

        PlayMoneySoundClientRpc();
    }

    [ClientRpc]
    private void PlayMoneySoundClientRpc()
    {
        if (moneySound != null)
        {
            moneySound.Play();
        }
    }
}
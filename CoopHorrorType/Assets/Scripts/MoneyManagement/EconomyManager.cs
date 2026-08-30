using Unity.Netcode;
using UnityEngine;

public class EconomyManager : NetworkBehaviour
{
    public static EconomyManager Instance { get; private set; }

    public int moneyGoal = 250;
    
    public NetworkVariable<int> currentMoney = new NetworkVariable<int>(0);

    public AudioClip moneySoundClip; 

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

    public void RemoveMoney(int amount)
    {
        if (!IsServer) return;

        currentMoney.Value = Mathf.Max(0, currentMoney.Value - amount);
    }

    [ClientRpc]
    private void PlayMoneySoundClientRpc()
    {
        if (AudioManager.Instance != null && moneySoundClip != null)
        {
            AudioManager.Instance.PlaySound2D(moneySoundClip, volume: 0.85f, pitchRandomness: 0.12f);
        }
    }
}
using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class TutorialStalkerManager : NetworkBehaviour
{
    public static TutorialStalkerManager Instance { get; private set; }

    [Header("Configuration Stalker")]
    public GameObject stalkerPrefab;
    public Transform sideCorridorSpawnPoint; 
    public Light[] corridorLights;           
    public StalkerSpawner stalkerSpawner; 

    [Header("Conditions de Bannissement du Monstre")]
    public PipeNetworkManager pipeCircuit;  
    public int requiredMoneyToBanish = 200; 

    private GameObject activeStalkerInstance;
    private bool isTriggered = false;
    private bool isBanished = false;

    private void Awake()
    {
        Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            if (pipeCircuit != null)
            {
                pipeCircuit.isCircuitCompleted.OnValueChanged += CheckBanishConditionsCircuit;
            }

            if (EconomyManager.Instance != null)
            {
                EconomyManager.Instance.currentMoney.OnValueChanged += CheckBanishConditionsMoney;
            }
        }
    }

    public void TriggerStalkerIntro()
    {
        if (!IsServer || isTriggered || isBanished) return;

        isTriggered = true;
        StartCoroutine(StalkerIntroSequence());
    }

    private IEnumerator StalkerIntroSequence()
    {
        if (corridorLights != null && corridorLights.Length > 0)
        {
            for (int i = 0; i < 6; i++)
            {
                bool lightState = (i % 2 == 0);
                foreach (Light l in corridorLights) if (l != null) l.enabled = lightState;
                yield return new WaitForSeconds(0.12f);
            }
            foreach (Light l in corridorLights) if (l != null) l.enabled = true;
        }

        if (stalkerPrefab != null && sideCorridorSpawnPoint != null && FindObjectOfType<StalkerAI>() == null)
        {
            activeStalkerInstance = Instantiate(stalkerPrefab, sideCorridorSpawnPoint.position, sideCorridorSpawnPoint.rotation);
            activeStalkerInstance.GetComponent<NetworkObject>().Spawn();

            if (stalkerSpawner != null)
            {
                stalkerSpawner.RegisterCurrentStalker(activeStalkerInstance);
            }
        }

        if (stalkerSpawner != null)
        {
            stalkerSpawner.StartSpawningLoop();
        }
    }

    private void CheckBanishConditionsCircuit(bool previousValue, bool newValue) => VerifyBanishment();
    private void CheckBanishConditionsMoney(int previousValue, int newValue) => VerifyBanishment();

    private void VerifyBanishment()
    {
        if (!IsServer || isBanished) return;

        bool circuitDone = pipeCircuit == null || pipeCircuit.isCircuitCompleted.Value;
        bool moneyDone = (EconomyManager.Instance != null && EconomyManager.Instance.currentMoney.Value >= requiredMoneyToBanish);

        if (circuitDone && moneyDone)
        {
            BanishStalker();
        }
    }

    private void BanishStalker()
    {
        isBanished = true;
        if (stalkerSpawner != null)
        {
            stalkerSpawner.StopSpawningLoop();
        }

        StalkerAI[] allStalkers = FindObjectsOfType<StalkerAI>();
        foreach (var stalker in allStalkers)
        {
            if (stalker.TryGetComponent<NetworkObject>(out var netObj) && netObj.IsSpawned)
            {
                netObj.Despawn();
            }
        }
        activeStalkerInstance = null;
    }

    public override void OnNetworkDespawn()
    {
        if (pipeCircuit != null) pipeCircuit.isCircuitCompleted.OnValueChanged -= CheckBanishConditionsCircuit;
        if (EconomyManager.Instance != null) EconomyManager.Instance.currentMoney.OnValueChanged -= CheckBanishConditionsMoney;
    }
}
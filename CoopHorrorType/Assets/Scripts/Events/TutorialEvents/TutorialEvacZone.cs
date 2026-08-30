using Unity.Netcode;
using UnityEngine;

public class TutorialEvacZone : NetworkInteractable
{
    [Header("Conditions d'Évacuation")]
    public int requiredMoneyForEvac = 200; 
    public PipeNetworkManager pipeCircuit; 

    public override string GetInteractPrompt()
    {
        int currentMoney = EconomyManager.Instance != null ? EconomyManager.Instance.currentMoney.Value : 0;

        if (currentMoney < requiredMoneyForEvac)
        {
            return $"Objectif insuffisant ! (${currentMoney} / ${requiredMoneyForEvac})";
        }

        return "[E] Évacuer";
    }

    protected override void OnServerInteract(PlayerController player)
    {
        int currentMoney = EconomyManager.Instance != null ? EconomyManager.Instance.currentMoney.Value : 0;
        bool moneyMet = currentMoney >= requiredMoneyForEvac;
        bool circuitMet = pipeCircuit == null || pipeCircuit.isCircuitCompleted.Value;

        if (moneyMet && circuitMet)
        {
            if (GameOverManager.Instance != null)
            {
                GameOverManager.Instance.TriggerEndGame(true);
            }
        }
    }
}
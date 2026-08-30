using DG.Tweening;
using Unity.Netcode;
using UnityEngine;

public class TutorialDoor : NetworkBehaviour
{
    public PipeNetworkManager pipeCircuit; 
    public int requiredMoneyAmount = 200;  

    public Transform leftDoorPanel;
    public Transform rightDoorPanel;
    public float openDistance = 1.5f;
    public float openDuration = 1.2f;

    public AudioSource doorSound;

    private NetworkVariable<bool> isOpen = new NetworkVariable<bool>(false);

    public override void OnNetworkSpawn()
    {
        isOpen.OnValueChanged += OnDoorStateChanged;

        if (IsServer)
        {
            if (TutorialProgress.hasReachedCheckpoint)
            {
                ForceOpenDoor();
            }

            if (pipeCircuit != null)
            {
                pipeCircuit.isCircuitCompleted.OnValueChanged += CheckConditionsCircuit;
            }

            if (EconomyManager.Instance != null)
            {
                EconomyManager.Instance.currentMoney.OnValueChanged += CheckConditionsMoney;
            }
        }
    }

    private void CheckConditionsCircuit(bool previousValue, bool newValue) => VerifyDoorOpening();
    private void CheckConditionsMoney(int previousValue, int newValue) => VerifyDoorOpening();

    private void VerifyDoorOpening()
    {
        if (!IsServer || isOpen.Value) return;

        bool circuitDone = pipeCircuit == null || pipeCircuit.isCircuitCompleted.Value;
        bool moneyDone = (EconomyManager.Instance != null && EconomyManager.Instance.currentMoney.Value >= requiredMoneyAmount);

        if (circuitDone && moneyDone)
        {
            isOpen.Value = true;
        }
    }

    public void ForceOpenDoor()
    {
        if (!IsServer || isOpen.Value) return;
        isOpen.Value = true;
    }

    private void OnDoorStateChanged(bool previousValue, bool newValue)
    {
        if (newValue == true)
        {
            AnimateDoorOpen();
        }
    }

    private void AnimateDoorOpen()
    {
        if (doorSound != null) doorSound.Play();

        if (leftDoorPanel != null) leftDoorPanel.DOLocalMoveX(leftDoorPanel.localPosition.x - openDistance, openDuration).SetEase(Ease.OutBack);
        if (rightDoorPanel != null) rightDoorPanel.DOLocalMoveX(rightDoorPanel.localPosition.x + openDistance, openDuration).SetEase(Ease.OutBack);
    }

    public override void OnNetworkDespawn()
    {
        if (pipeCircuit != null) pipeCircuit.isCircuitCompleted.OnValueChanged -= CheckConditionsCircuit;
        if (EconomyManager.Instance != null) EconomyManager.Instance.currentMoney.OnValueChanged -= CheckConditionsMoney;
    }
}
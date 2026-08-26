using DG.Tweening; 
using Unity.Netcode;
using UnityEngine;

public class MechanicalDoor : NetworkBehaviour
{
    [Header("Panneaux Coulissants")]
    public Transform leftPanel;   
    public Transform rightPanel;  
    public float openDistance = 1.5f; 
    public float openDuration = 1.2f; 

    [Header("Audio")]
    public AudioSource doorSound;
    public AudioClip openSoundClip;
    public AudioClip closeSoundClip;

    public NetworkVariable<bool> isOpen = new NetworkVariable<bool>(false);

    private Vector3 leftInitialPos;
    private Vector3 rightInitialPos;

    private void Awake()
    {
        if (leftPanel != null) leftInitialPos = leftPanel.localPosition;
        if (rightPanel != null) rightInitialPos = rightPanel.localPosition;
    }

    public override void OnNetworkSpawn()
    {
        isOpen.OnValueChanged += OnDoorStateChanged;
    }

    public void ToggleDoor()
    {
        if (!IsServer) return;
        isOpen.Value = !isOpen.Value;
    }

    private void OnDoorStateChanged(bool previousValue, bool newValue)
    {
        AnimateDoor(newValue);
    }

    private void AnimateDoor(bool openState)
    {
        if (doorSound != null)
        {
            AudioClip clipToPlay = openState ? openSoundClip : closeSoundClip;
            if (clipToPlay != null) doorSound.PlayOneShot(clipToPlay);
        }

        if (openState)
        {
            if (leftPanel != null) leftPanel.DOLocalMoveX(leftInitialPos.x - openDistance, openDuration).SetEase(Ease.OutBack);
            if (rightPanel != null) rightPanel.DOLocalMoveX(rightInitialPos.x + openDistance, openDuration).SetEase(Ease.OutBack);
        }
        else
        {
            if (leftPanel != null) leftPanel.DOLocalMoveX(leftInitialPos.x, openDuration).SetEase(Ease.InBack);
            if (rightPanel != null) rightPanel.DOLocalMoveX(rightInitialPos.x, openDuration).SetEase(Ease.InBack);
        }
    }
}
using UnityEngine;
using UnityEngine.Audio;

[RequireComponent(typeof(BoxCollider))]
public class RoomAcousticZone : MonoBehaviour
{
    [Header("Acoustique de cette Pièce")]
    [Tooltip("Glisse le snapshot correspondant")]
    public AudioMixerSnapshot roomSnapshot;

    [Tooltip("Durée de la transition lors de l'entrée dans la pièce")]
    public float transitionDuration = 0.35f;

    private void Awake()
    {
        BoxCollider box = GetComponent<BoxCollider>();
        if (box != null) box.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        PlayerController player = other.GetComponentInParent<PlayerController>();
        if (player != null && player.IsOwner)
        {
            if (AudioManager.Instance != null && roomSnapshot != null)
            {
                AudioManager.Instance.SetRoomAcoustics(roomSnapshot, transitionDuration);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        PlayerController player = other.GetComponentInParent<PlayerController>();
        if (player != null && player.IsOwner)
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.ResetToDefaultAcoustics(transitionDuration);
            }
        }
    }
}
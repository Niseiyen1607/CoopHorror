using UnityEngine;
using UnityEngine.Audio;

[RequireComponent(typeof(BoxCollider))]
public class RoomAcousticZone : MonoBehaviour
{
    public AudioMixerSnapshot roomSnapshot;
    public float transitionDuration = 0.35f;

    private static int activeZonesCount = 0;
    private static AudioMixerSnapshot currentActiveZoneSnapshot = null;

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
            activeZonesCount++;
            currentActiveZoneSnapshot = roomSnapshot;

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
            activeZonesCount = Mathf.Max(0, activeZonesCount - 1);

            if (activeZonesCount == 0)
            {
                currentActiveZoneSnapshot = null;
                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.ResetToDefaultAcoustics(transitionDuration);
                }
            }
            else if (currentActiveZoneSnapshot != null && currentActiveZoneSnapshot != roomSnapshot)
            {
                AudioManager.Instance.SetRoomAcoustics(currentActiveZoneSnapshot, transitionDuration);
            }
        }
    }
}
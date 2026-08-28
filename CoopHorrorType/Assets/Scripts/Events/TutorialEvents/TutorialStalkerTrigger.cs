using Unity.Netcode;
using UnityEngine;

public class TutorialStalkerTrigger : NetworkBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer) return;

        PlayerController player = other.GetComponentInParent<PlayerController>();

        if (player != null)
        {
            if (TutorialStalkerManager.Instance != null)
            {
                TutorialStalkerManager.Instance.TriggerStalkerIntro();
            }
        }
    }
}
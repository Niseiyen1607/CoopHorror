using Unity.Netcode;
using UnityEngine;

public class RadioTrigger : NetworkBehaviour
{
    public DialogueData dialogue; 

    private bool hasTriggered = false;

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer || hasTriggered) return;

        if (TutorialProgress.hasReachedCheckpoint) return;

        PlayerController player = other.GetComponentInParent<PlayerController>();

        if (player != null)
        {
            hasTriggered = true;
            PlayTriggerDialogueClientRpc();
        }
    }

    [ClientRpc]
    private void PlayTriggerDialogueClientRpc()
    {
        if (dialogue.voiceClip != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayVoiceOver2D(dialogue.voiceClip, 1.0f);
        }

        if (RadioSubtitleUI.Instance != null && dialogue.phrases != null && dialogue.phrases.Count > 0)
        {
            RadioSubtitleUI.Instance.ShowPhrases(dialogue.speakerName, dialogue.phrases);
        }
    }
}
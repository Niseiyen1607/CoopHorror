using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class TutorialNarrator : NetworkBehaviour
{
    public static TutorialNarrator Instance { get; private set; }

    [Header("Dialogues de Serge (Style Shorts)")]
    public DialogueData vo1_Intro;
    public DialogueData vo3_UnscrewPipe;
    public DialogueData vo4_SnapPipe;

    [Header("Audio Radio")]
    public AudioClip staticStartSound; 

    [Header("Références Scène")]
    public TutorialDoor startDoor;

    private bool hasPlayedUnscrewVO = false;
    private bool hasPlayedSnapVO = false;

    private void Awake() => Instance = this;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            DefectivePipe.OnAnyPipeUnscrewed += PlayUnscrewDialogue;
            PipeSocket.OnAnyPipeSnapped += PlaySnapDialogue;

            StartCoroutine(PlayIntroRoutine());
        }
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer)
        {
            DefectivePipe.OnAnyPipeUnscrewed -= PlayUnscrewDialogue;
            PipeSocket.OnAnyPipeSnapped -= PlaySnapDialogue;
        }
    }

    private IEnumerator PlayIntroRoutine()
    {
        yield return new WaitForSeconds(2.5f);

        Debug.Log("<color=yellow>[TUTO NARRATEUR] Lancement du dialogue d'Intro (VO 1)...</color>");

        PlayDialogueClientRpc(1);

        float totalDuration = GetTotalPhrasesDuration(vo1_Intro.phrases);
        yield return new WaitForSeconds(totalDuration > 0 ? totalDuration : 5.0f);

        if (startDoor != null)
        {
            Debug.Log("<color=green>[TUTO NARRATEUR] Intro terminée. Ouverture de la Porte 1 !</color>");
            startDoor.ForceOpenDoor();
        }
    }

    private float GetTotalPhrasesDuration(List<SubtitlePhrase> phrases)
    {
        if (phrases == null) return 5f;
        float sum = 0f;
        foreach (var p in phrases) sum += p.duration;
        return sum;
    }

    private void PlayUnscrewDialogue()
    {
        if (hasPlayedUnscrewVO) return; 
        hasPlayedUnscrewVO = true;

        PlayDialogueClientRpc(3);
    }

    private void PlaySnapDialogue()
    {
        if (hasPlayedSnapVO) return; 
        hasPlayedSnapVO = true;

        PlayDialogueClientRpc(4);
    }

    [ClientRpc]
    private void PlayDialogueClientRpc(int dialogueNumber)
    {
        DialogueData dataToPlay = vo1_Intro;
        if (dialogueNumber == 3) dataToPlay = vo3_UnscrewPipe;
        if (dialogueNumber == 4) dataToPlay = vo4_SnapPipe;

        if (AudioManager.Instance != null && staticStartSound != null)
        {
            AudioManager.Instance.PlaySound2D(staticStartSound, 0.5f, pitchRandomness: 0.02f);
        }

        if (dataToPlay.voiceClip != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayVoiceOver2D(dataToPlay.voiceClip, 1.0f);
        }

        if (RadioSubtitleUI.Instance != null && dataToPlay.phrases != null)
        {
            RadioSubtitleUI.Instance.ShowPhrases(dataToPlay.speakerName, dataToPlay.phrases);
        }
    }
}
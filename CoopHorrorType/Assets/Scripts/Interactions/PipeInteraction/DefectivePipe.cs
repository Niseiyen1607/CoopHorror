using Unity.Netcode;
using UnityEngine;

public class DefectivePipe : NetworkInteractable
{
    public static event System.Action OnAnyPipeUnscrewed;

    public GameObject pipeSocket;        
    public GameObject brokenPipePrefab;  

    public AudioClip[] wrenchUnscrewSounds; 

    public override string GetInteractPrompt()
    {
        PlayerController localPlayer = NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<PlayerController>();

        if (localPlayer != null && localPlayer.currentlyHeldItem != null && localPlayer.currentlyHeldItem.itemType == ItemType.Wrench)
        {
            return "[E] Dévisser le tuyau (Clé à molette)";
        }

        return "Nécessite une Clé à molette !";
    }

    protected override void OnServerInteract(PlayerController player)
    {
        if (player.currentlyHeldItem != null && player.currentlyHeldItem.itemType == ItemType.Wrench)
        {
            PlayUnscrewSoundClientRpc(transform.position);

            OnAnyPipeUnscrewed?.Invoke();

            if (brokenPipePrefab != null)
            {
                GameObject brokenPipe = Instantiate(brokenPipePrefab, transform.position, transform.rotation);
                brokenPipe.GetComponent<NetworkObject>().Spawn();

                if (brokenPipe.TryGetComponent<Rigidbody>(out Rigidbody rb))
                {
                    rb.isKinematic = false;
                    Vector3 directionToPlayer = (player.transform.position - transform.position).normalized;
                    Vector3 dropImpulse = (directionToPlayer * 5.0f) + (Vector3.up * 1.0f); 
                    rb.AddForce(dropImpulse, ForceMode.Impulse);
                }
            }

            EnableSocketClientRpc();
            GetComponent<NetworkObject>().Despawn();
        }
    }

    [ClientRpc]
    private void PlayUnscrewSoundClientRpc(Vector3 pos)
    {
        if (AudioManager.Instance != null && wrenchUnscrewSounds != null && wrenchUnscrewSounds.Length > 0)
        {
            AudioClip clip = wrenchUnscrewSounds[Random.Range(0, wrenchUnscrewSounds.Length)];
            AudioManager.Instance.PlaySound3D(clip, pos, 1.5f, 20f);
        }
    }

    [ClientRpc]
    private void EnableSocketClientRpc()
    {
        if (pipeSocket != null)
        {
            pipeSocket.SetActive(true);
            PipeSocket socketScript = pipeSocket.GetComponent<PipeSocket>();
            if (socketScript != null) socketScript.enabled = true;
            Collider socketCollider = pipeSocket.GetComponent<Collider>();
            if (socketCollider != null) socketCollider.enabled = true;
        }
    }
}
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : NetworkBehaviour
{
    [Header("Réglages Déplacement")]
    public float baseMoveSpeed = 5f;
    public float sprintMultiplier = 1.5f; 
    public float crouchMultiplier = 0.5f; 
    public float gravity = -19.62f;

    [Header("Accroupi & Hauteur")]
    public float standingHeight = 2.0f;
    public float crouchingHeight = 1.3f; 

    [Header("Audio - Sons & Intervalles")]
    public AudioClip[] walkFootsteps;
    public AudioClip[] sprintFootsteps;
    public AudioClip[] crouchFootsteps;
    public AudioClip crouchTransitionSound;
    
    public float walkStepInterval = 0.5f;
    public float sprintStepInterval = 0.32f;
    public float crouchStepInterval = 0.7f;
    private float stepTimer = 0f;

    [Header("Système de Mort & Ragdoll")]
    public GameObject deadBodyRagdollPrefab;   
    [HideInInspector] public NetworkVariable<bool> isDead = new NetworkVariable<bool>(false);
    
    [HideInInspector] public NetworkVariable<float> speedMultiplier = new NetworkVariable<float>(1f);
    [HideInInspector] public NetworkVariable<bool> isHiding = new NetworkVariable<bool>(false); 
    [HideInInspector] public NetworkVariable<bool> isCrouching = new NetworkVariable<bool>(false);
    [HideInInspector] public NetworkVariable<bool> isSpeaking = new NetworkVariable<bool>(false);

    [HideInInspector] public NetworkVariable<float> cameraPitch = new NetworkVariable<float>(0f);
    [HideInInspector] public NetworkVariable<FixedString32Bytes> playerName = new NetworkVariable<FixedString32Bytes>();

    [HideInInspector] public NetworkVariable<NetworkObjectReference> currentlyHeldItemRef = new NetworkVariable<NetworkObjectReference>();

    [HideInInspector] public HidingSpot currentHidingSpot;

    [Header("Références")]
    public Transform cameraHolder;
    public Transform holdPoint;
    public Transform playerModel; 

    private CharacterController controller;
    private Vector3 playerVelocity;

    private Vector3 initialSpawnPosition;
    private Quaternion initialSpawnRotation;

    public CarriableItem currentlyHeldItem
    {
        get
        {
            if (currentlyHeldItemRef.Value.TryGet(out NetworkObject netObj))
            {
                CarriableItem item = netObj.GetComponent<CarriableItem>();
                if (item != null && item.enabled && item.IsHeld())
                {
                    return item;
                }
            }
            return null;
        }
    }

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
    }

    public override void OnNetworkSpawn()
    {
        initialSpawnPosition = transform.position;
        initialSpawnRotation = transform.rotation;

        isCrouching.OnValueChanged += OnCrouchStateChanged;
        isDead.OnValueChanged += OnDeadStateChanged;
    }

    public override void OnNetworkDespawn()
    {
        isCrouching.OnValueChanged -= OnCrouchStateChanged;
        isDead.OnValueChanged -= OnDeadStateChanged;
    }

    private void OnDeadStateChanged(bool previousValue, bool isDeadNow)
    {
        if (isDeadNow)
        {
            if (controller != null) controller.enabled = false;

            if (TryGetComponent<PlayerCameraLook>(out var pcl)) pcl.enabled = false;
            if (TryGetComponent<PlayerInteraction>(out var pi)) pi.enabled = false;
            if (TryGetComponent<PlayerThrowController>(out var pt)) pt.enabled = false;
            if (TryGetComponent<PlayerFlashlight>(out var pf)) 
            {
                pf.enabled = false;
                if (pf.headLight != null) pf.headLight.enabled = false;
            }

            if (IsOwner && SpectatorManager.Instance != null)
            {
                SpectatorManager.Instance.StartSpectating();
            }
        }
    }

    public void RespawnPlayerAtCheckpoint()
    {
        if (!IsServer) return;

        isDead.Value = false;

        Vector3 basePos = (TutorialProgress.hasReachedCheckpoint && TutorialProgress.checkpointPosition != Vector3.zero)
            ? TutorialProgress.checkpointPosition
            : initialSpawnPosition;

        float angle = OwnerClientId * (360f / 4f);
        Vector3 offset = Quaternion.Euler(0, angle, 0) * Vector3.forward * 1.5f;
        Vector3 finalSpawnPos = basePos + offset;

        Quaternion targetRot = initialSpawnRotation;

        if (controller != null) controller.enabled = false;
        transform.position = finalSpawnPos;
        transform.rotation = targetRot;
        if (controller != null) controller.enabled = true;

        RespawnClientRpc(OwnerClientId, finalSpawnPos, targetRot);
    }

    [ClientRpc]
    private void RespawnClientRpc(ulong clientId, Vector3 spawnPos, Quaternion spawnRot)
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (Renderer r in renderers) r.enabled = true;

        if (IsOwner)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            if (controller != null) controller.enabled = false;
            transform.position = spawnPos;
            transform.rotation = spawnRot;
            if (controller != null) controller.enabled = true;

            // CORRECTION : Recoller la caméra principale à la tête du joueur
            if (cameraHolder != null)
            {
                cameraHolder.localRotation = Quaternion.identity;
            }

            if (Camera.main != null)
            {
                Camera.main.transform.localPosition = Vector3.zero;
                Camera.main.transform.localRotation = Quaternion.identity;
            }

            if (TryGetComponent<PlayerCameraLook>(out var pcl)) pcl.enabled = true;
            if (TryGetComponent<PlayerInteraction>(out var pi)) pi.enabled = true;
            if (TryGetComponent<PlayerThrowController>(out var pt)) pt.enabled = true;
            if (TryGetComponent<PlayerFlashlight>(out var pf)) pf.enabled = true;

            if (SpectatorManager.Instance != null)
            {
                SpectatorManager.Instance.HideSpectatorHUD();
            }

            if (ScreenFader.Instance != null)
            {
                ScreenFader.Instance.FadeToClear(0.6f);
            }
        }
    }

    void Update()
    {
        if (isDead.Value) return;

    if (!IsOwner) return;

    if (isHiding.Value)
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            ExitHidingSpotServerRpc();
        }
        return; 
    }

    if (Input.GetKeyDown(KeyCode.LeftControl) || Input.GetKeyDown(KeyCode.C))
    {
        ToggleCrouchServerRpc(!isCrouching.Value);
    }

    bool isSprintKeyPressed = Input.GetKey(KeyCode.LeftShift);
    float x = Input.GetAxis("Horizontal");
    float z = Input.GetAxis("Vertical");
    bool isMoving = (x != 0 || z != 0);

        if (isCrouching.Value && isSprintKeyPressed)
        {
            ToggleCrouchServerRpc(false);
        }

        bool isSprinting = isSprintKeyPressed && isMoving && currentlyHeldItem == null && !isCrouching.Value;

        float activeSpeedBonus = 1f;
        if (isSprinting) activeSpeedBonus = sprintMultiplier;
        else if (isCrouching.Value) activeSpeedBonus = crouchMultiplier;

        float currentSpeed = baseMoveSpeed * speedMultiplier.Value * activeSpeedBonus;

        Vector3 move = transform.right * x + transform.forward * z;
        controller.Move(move * currentSpeed * Time.deltaTime);

        if (controller.isGrounded && playerVelocity.y < 0)
        {
            playerVelocity.y = -2f;
        }

        playerVelocity.y += gravity * Time.deltaTime;
        controller.Move(playerVelocity * Time.deltaTime);

        HandleFootsteps(isMoving, isSprinting);

        if (Input.GetKeyDown(KeyCode.G))
        {
            DropHeldItemServerRpc();
        }
    }

    private void HandleFootsteps(bool isMoving, bool isSprinting)
    {
        if (controller != null && controller.isGrounded && isMoving)
        {
            stepTimer += Time.deltaTime;

            float targetInterval = walkStepInterval;
            int stepType = 0; 

            if (isSprinting)
            {
                targetInterval = sprintStepInterval;
                stepType = 1; 
            }
            else if (isCrouching.Value)
            {
                targetInterval = crouchStepInterval;
                stepType = 2; 
            }

            if (stepTimer >= targetInterval)
            {
                stepTimer = 0f;
                PlayFootstepServerRpc(stepType);
            }
        }
        else
        {
            stepTimer = 0f;
        }
    }

    private void OnCrouchStateChanged(bool previousValue, bool newValue)
    {
        float targetHeight = newValue ? crouchingHeight : standingHeight;
        float lastHeight = controller != null ? controller.height : standingHeight;

        if (controller != null)
        {
            controller.height = targetHeight;
            controller.center = new Vector3(0, targetHeight / 2f, 0);

            float heightDifference = targetHeight - lastHeight;
            if (heightDifference < 0)
            {
                controller.Move(Vector3.up * 0.01f);
            }
        }

        if (cameraHolder != null)
        {
            float targetCamY = newValue ? (crouchingHeight * 0.85f) : (standingHeight * 0.85f);
            cameraHolder.localPosition = new Vector3(0, targetCamY, 0);
        }

        if (playerModel != null)
        {
            playerModel.localPosition = Vector3.zero;
        }

        if (AudioManager.Instance != null && crouchTransitionSound != null)
        {
            AudioManager.Instance.PlaySound3D(crouchTransitionSound, transform.position, 0.4f, 1f, 10f);
        }
    }

    public void Die()
    {
        if (!IsServer || isDead.Value) return;

        isDead.Value = true;

        Debug.Log($"<color=red>☠️ [MORT] Le joueur {playerName.Value} est mort ! Spawn du Ragdoll...</color>");

        if (currentlyHeldItem != null)
        {
            try { currentlyHeldItem.DropRequestedByPlayer(this); } catch { }
        }

        if (deadBodyRagdollPrefab != null)
        {
            GameObject ragdoll = Instantiate(deadBodyRagdollPrefab, transform.position, transform.rotation);
            if (ragdoll.TryGetComponent<NetworkObject>(out var netObj))
            {
                netObj.Spawn();
            }
        }

        OnPlayerDiedClientRpc(OwnerClientId);
    }

    [ClientRpc]
    private void OnPlayerDiedClientRpc(ulong deadClientId)
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.SpawnManager != null)
        {
            if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(NetworkObjectId, out var deadPlayerObj))
            {
                Renderer[] renderers = deadPlayerObj.GetComponentsInChildren<Renderer>();
                foreach (Renderer r in renderers)
                {
                    if (cameraHolder == null || !r.transform.IsChildOf(cameraHolder))
                    {
                        r.enabled = false;
                    }
                }
            }
        }

        if (NetworkManager.Singleton.LocalClientId == deadClientId)
        {
            if (SpectatorManager.Instance != null)
            {
                SpectatorManager.Instance.StartSpectating();
            }
        }
    }

    [ClientRpc]
    public void TeleportClientRpc(Vector3 targetPos, Quaternion targetRot)
    {
        if (IsOwner)
        {
            if (controller != null) controller.enabled = false;
            transform.position = targetPos;
            transform.rotation = targetRot;
            if (controller != null) controller.enabled = true;
        }
    }

    [ServerRpc]
    private void ToggleCrouchServerRpc(bool crouchState)
    {
        isCrouching.Value = crouchState;
    }

    [ServerRpc]
    private void PlayFootstepServerRpc(int stepType)
    {
        PlayFootstepClientRpc(stepType);
    }

    [ClientRpc]
    private void PlayFootstepClientRpc(int stepType)
    {
        if (AudioManager.Instance == null) return;

        AudioClip[] clipsToUse = null;
        float volume = 0.5f;
        float maxDist = 20f;

        switch (stepType)
        {
            case 0: clipsToUse = walkFootsteps; volume = 0.5f; maxDist = 18f; break;
            case 1: clipsToUse = sprintFootsteps; volume = 0.8f; maxDist = 30f; break;
            case 2: clipsToUse = crouchFootsteps; volume = 0.2f; maxDist = 8f; break;
        }

        if (clipsToUse != null && clipsToUse.Length > 0)
        {
            AudioClip clip = clipsToUse[Random.Range(0, clipsToUse.Length)];
            AudioManager.Instance.PlaySound3D(clip, transform.position, volume, 1f, maxDist);
        }
    }

    [ServerRpc]
    private void DropHeldItemServerRpc()
    {
        if (currentlyHeldItem != null)
        {
            currentlyHeldItem.DropRequestedByPlayer(this);
        }
    }

    [ServerRpc]
    private void ExitHidingSpotServerRpc()
    {
        if (currentHidingSpot != null)
        {
            currentHidingSpot.ExitHidingSpot(this);
        }
    }
}
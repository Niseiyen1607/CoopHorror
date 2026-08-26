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
    
    [HideInInspector] public NetworkVariable<float> speedMultiplier = new NetworkVariable<float>(1f);
    [HideInInspector] public NetworkVariable<bool> isHiding = new NetworkVariable<bool>(false); 
    [HideInInspector] public NetworkVariable<bool> isCrouching = new NetworkVariable<bool>(false);
    [HideInInspector] public NetworkVariable<bool> isSpeaking = new NetworkVariable<bool>(false);

    [HideInInspector] public NetworkVariable<float> cameraPitch = new NetworkVariable<float>(0f);
    [HideInInspector] public NetworkVariable<FixedString32Bytes> playerName = new NetworkVariable<FixedString32Bytes>();

    [HideInInspector] public NetworkVariable<NetworkObjectReference> currentlyHeldItemRef = new NetworkVariable<NetworkObjectReference>();

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

    [HideInInspector] public HidingSpot currentHidingSpot;

    [Header("Références")]
    public Transform cameraHolder;
    public Transform holdPoint;
    public Transform playerModel; 

    private CharacterController controller;
    private Vector3 playerVelocity;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
    }

    public override void OnNetworkSpawn()
    {
        isCrouching.OnValueChanged += OnCrouchStateChanged;
    }

    public override void OnNetworkDespawn()
    {
        isCrouching.OnValueChanged -= OnCrouchStateChanged;
    }

    void Update()
    {
        if (!IsOwner) return;

        if (isHiding.Value && currentHidingSpot != null) return;

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
    }

    private void HandleFootsteps(bool isMoving, bool isSprinting)
    {
        if (controller.isGrounded && isMoving)
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
        float lastHeight = controller.height;

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
            case 0: 
                clipsToUse = walkFootsteps;
                volume = 0.5f;
                maxDist = 18f;
                break;
            case 1: 
                clipsToUse = sprintFootsteps;
                volume = 0.8f;
                maxDist = 30f;
                break;
            case 2: 
                clipsToUse = crouchFootsteps;
                volume = 0.2f;
                maxDist = 8f;
                break;
        }

        if (clipsToUse != null && clipsToUse.Length > 0)
        {
            AudioClip clip = clipsToUse[Random.Range(0, clipsToUse.Length)];
            AudioManager.Instance.PlaySound3D(clip, transform.position, volume, 1f, maxDist);
        }
    }
}
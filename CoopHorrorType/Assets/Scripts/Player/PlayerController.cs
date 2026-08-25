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
    
    [HideInInspector] public NetworkVariable<float> speedMultiplier = new NetworkVariable<float>(1f);
    [HideInInspector] public NetworkVariable<bool> isHiding = new NetworkVariable<bool>(false); 
    [HideInInspector] public NetworkVariable<bool> isCrouching = new NetworkVariable<bool>(false);
    [HideInInspector] public NetworkVariable<bool> isSpeaking = new NetworkVariable<bool>(false);

    [HideInInspector] public NetworkVariable<float> cameraPitch = new NetworkVariable<float>(0f);
    [HideInInspector] public NetworkVariable<FixedString32Bytes> playerName = new NetworkVariable<FixedString32Bytes>();

    [HideInInspector] public NetworkVariable<NetworkObjectReference> currentlyHeldItemRef = new NetworkVariable<NetworkObjectReference>();

    // CORRECTION BLINDÉE : Vérifie que l'objet est ACTIF et TENU pour éviter les faux positifs !
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

    [Header("Références (Requis par les Objets et Armoires)")]
    public Transform cameraHolder;
    public Transform holdPoint;

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
    }

    private void OnCrouchStateChanged(bool previousValue, bool newValue)
    {
        if (controller != null)
        {
            float targetHeight = newValue ? crouchingHeight : standingHeight;
            controller.height = targetHeight;
            controller.center = new Vector3(0, targetHeight / 2f, 0);
        }

        if (cameraHolder != null)
        {
            float targetCamY = newValue ? (crouchingHeight * 0.85f) : (standingHeight * 0.85f);
            cameraHolder.localPosition = new Vector3(0, targetCamY, 0);
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
}
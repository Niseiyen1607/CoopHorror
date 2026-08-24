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
    public float mouseSensitivity = 2f;
    public float gravity = -19.62f;

    [Header("Réglages Lancer Chargé")]
    public float minThrowForce = 5f;     
    public float maxThrowForce = 22f;    
    public float maxChargeTime = 1.2f;   
    public Vector3 chargedHoldOffset = new Vector3(0f, 0f, -0.6f); 

    [Header("Accroupi & Hauteur")]
    public float standingHeight = 2.0f;
    public float crouchingHeight = 1.3f; 
    
    [HideInInspector] public NetworkVariable<float> speedMultiplier = new NetworkVariable<float>(1f);
    [HideInInspector] public NetworkVariable<bool> isHiding = new NetworkVariable<bool>(false); 
    [HideInInspector] public NetworkVariable<bool> isCrouching = new NetworkVariable<bool>(false);
    
    [HideInInspector] public NetworkVariable<float> cameraPitch = new NetworkVariable<float>(0f);
    [HideInInspector] public NetworkVariable<FixedString32Bytes> playerName = new NetworkVariable<FixedString32Bytes>();

    [HideInInspector] public CarriableItem currentlyHeldItem; 
    [HideInInspector] public HidingSpot currentHidingSpot;

    [Header("Références")]
    public Transform cameraHolder;
    public Transform holdPoint;
    
    private Camera playerCam;
    private AudioListener audioListener;
    private CharacterController controller;
    
    private float verticalRotation = 0f;
    private Vector3 playerVelocity;
    private bool isSprinting = false;

    private float currentChargeTime = 0f;
    private bool isChargingThrow = false;
    private Vector3 originalHoldPointLocalPos;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
    }

    private void Start()
    {
        if (holdPoint != null)
        {
            originalHoldPointLocalPos = holdPoint.localPosition;
        }
    }

    public override void OnNetworkSpawn()
    {
        playerCam = GetComponentInChildren<Camera>();
        audioListener = GetComponentInChildren<AudioListener>();

        isCrouching.OnValueChanged += OnCrouchStateChanged;

        if (IsOwner)
        {
            if (playerCam != null) playerCam.enabled = true;
            if (audioListener != null) audioListener.enabled = true;
            
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            HideLocalPlayerMesh();
        }
        else
        {
            if (playerCam != null) playerCam.enabled = false;
            if (audioListener != null) audioListener.enabled = false;
        }
    }

    private void HideLocalPlayerMesh()
    {
        SkinnedMeshRenderer[] skinnedRenderers = GetComponentsInChildren<SkinnedMeshRenderer>();
        foreach (SkinnedMeshRenderer r in skinnedRenderers)
        {
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
        }

        MeshRenderer[] renderers = GetComponentsInChildren<MeshRenderer>();
        foreach (MeshRenderer r in renderers)
        {
            if (holdPoint != null && r.transform.IsChildOf(holdPoint)) continue;
            if (cameraHolder != null && r.transform.IsChildOf(cameraHolder)) continue;

            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
        }
    }

    void Update()
    {
        if (cameraHolder != null)
        {
            float pitchToApply = IsOwner ? verticalRotation : cameraPitch.Value;
            cameraHolder.localRotation = Quaternion.Euler(pitchToApply, 0f, 0f);
        }

        if (!IsOwner) return;

        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        transform.Rotate(Vector3.up * mouseX);

        verticalRotation -= mouseY;
        verticalRotation = Mathf.Clamp(verticalRotation, -80f, 80f);

        if (Mathf.Abs(cameraPitch.Value - verticalRotation) > 0.1f)
        {
            UpdateCameraPitchServerRpc(verticalRotation);
        }

        if (isHiding.Value && currentHidingSpot != null)
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

        if (isSprintKeyPressed && currentlyHeldItem != null)
        {
            ResetHoldPointPos();
            DropHeldItemServerRpc();
        }

        if (isCrouching.Value && isSprintKeyPressed)
        {
            ToggleCrouchServerRpc(false);
        }

        isSprinting = isSprintKeyPressed && isMoving && currentlyHeldItem == null && !isCrouching.Value;

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

        if (currentlyHeldItem != null)
        {
            if (Input.GetMouseButtonDown(0))
            {
                isChargingThrow = true;
                currentChargeTime = 0f;
            }

            if (Input.GetMouseButton(0) && isChargingThrow)
            {
                currentChargeTime += Time.deltaTime;
                float chargeRatio = Mathf.Clamp01(currentChargeTime / maxChargeTime);

                if (holdPoint != null)
                {
                    holdPoint.localPosition = originalHoldPointLocalPos + (chargedHoldOffset * chargeRatio);
                }
            }

            if (Input.GetMouseButtonUp(0) && isChargingThrow)
            {
                float chargeRatio = Mathf.Clamp01(currentChargeTime / maxChargeTime);
                float finalForce = Mathf.Lerp(minThrowForce, maxThrowForce, chargeRatio);

                ResetHoldPointPos();

                ThrowHeldItemServerRpc(cameraHolder.forward, finalForce);
                isChargingThrow = false;
                currentChargeTime = 0f;
            }
        }
        else
        {
            if (isChargingThrow) ResetHoldPointPos();
        }

        if (Input.GetKeyDown(KeyCode.G))
        {
            ResetHoldPointPos();
            DropHeldItemServerRpc();
        }
    }

    private void ResetHoldPointPos()
    {
        isChargingThrow = false;
        currentChargeTime = 0f;
        if (holdPoint != null)
        {
            holdPoint.localPosition = originalHoldPointLocalPos;
        }
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

    [ServerRpc]
    private void UpdateCameraPitchServerRpc(float pitch)
    {
        cameraPitch.Value = pitch;
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
    private void ThrowHeldItemServerRpc(Vector3 throwDir, float force)
    {
        if (currentlyHeldItem != null)
        {
            currentlyHeldItem.ThrowRequestedByPlayer(this, throwDir, force);
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
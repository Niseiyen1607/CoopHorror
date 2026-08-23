using Unity.Netcode;
using UnityEngine;

public class PlayerController : NetworkBehaviour
{
    [Header("Réglages")]
    public float baseMoveSpeed = 5f;
    public float mouseSensitivity = 2f;
    
    [HideInInspector] public NetworkVariable<float> speedMultiplier = new NetworkVariable<float>(1f);
    [HideInInspector] public CarriableItem currentlyHeldItem; 

    [Header("Références")]
    public Transform cameraHolder;
    public Transform holdPoint;
    
    private Camera playerCam;
    private AudioListener audioListener;
    private float verticalRotation = 0f;

    public override void OnNetworkSpawn()
    {
        playerCam = GetComponentInChildren<Camera>();
        audioListener = GetComponentInChildren<AudioListener>();

        if (IsOwner)
        {
            if (playerCam != null) playerCam.enabled = true;
            if (audioListener != null) audioListener.enabled = true;
            
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else
        {
            if (playerCam != null) playerCam.enabled = false;
            if (audioListener != null) audioListener.enabled = false;
        }
    }

    void Update()
    {
        if (!IsOwner) return;

        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        transform.Rotate(Vector3.up * mouseX);

        verticalRotation -= mouseY;
        verticalRotation = Mathf.Clamp(verticalRotation, -80f, 80f);
        if (cameraHolder != null)
        {
            cameraHolder.localRotation = Quaternion.Euler(verticalRotation, 0f, 0f);
        }

        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        float currentSpeed = baseMoveSpeed * speedMultiplier.Value;
        Vector3 move = transform.right * x + transform.forward * z;
        transform.position += move * currentSpeed * Time.deltaTime;

        if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.G))
        {
            DropHeldItemServerRpc();
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
}
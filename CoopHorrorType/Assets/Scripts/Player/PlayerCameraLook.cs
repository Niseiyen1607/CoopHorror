using Unity.Netcode;
using UnityEngine;

public class PlayerCameraLook : NetworkBehaviour
{
    [Header("Réglages Caméra")]
    public float mouseSensitivity = 2f;

    [HideInInspector] public NetworkVariable<float> cameraPitch = new NetworkVariable<float>(0f);

    private Camera playerCam;
    private AudioListener audioListener;
    private PlayerController playerController;
    private float verticalRotation = 0f;

    private void Awake()
    {
        playerController = GetComponent<PlayerController>();
    }

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
            if (playerController.holdPoint != null && r.transform.IsChildOf(playerController.holdPoint)) continue;
            if (playerController.cameraHolder != null && r.transform.IsChildOf(playerController.cameraHolder)) continue;

            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
        }
    }

    void Update()
    {
        if (playerController == null || playerController.isDead.Value) return; 

        if (playerController.cameraHolder != null)
        {
            float pitchToApply = IsOwner ? verticalRotation : cameraPitch.Value;
            playerController.cameraHolder.localRotation = Quaternion.Euler(pitchToApply, 0f, 0f);
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
    }

    [ServerRpc]
    private void UpdateCameraPitchServerRpc(float pitch)
    {
        cameraPitch.Value = pitch;
    }
}
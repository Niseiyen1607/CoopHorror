using Unity.Netcode;
using UnityEngine;

public class PlayerThrowController : NetworkBehaviour
{
    [Header("Réglages Lancer Chargé")]
    public float minThrowForce = 5f;     
    public float maxThrowForce = 22f;    
    public float maxChargeTime = 1.2f;   
    public Vector3 chargedHoldOffset = new Vector3(0f, 0f, -0.6f); 

    private PlayerController playerController;
    
    private float currentChargeTime = 0f;
    private bool isChargingThrow = false;
    private Vector3 originalHoldPointLocalPos;

    private void Awake()
    {
        playerController = GetComponent<PlayerController>();
    }

    private void Start()
    {
        if (playerController != null && playerController.holdPoint != null)
        {
            originalHoldPointLocalPos = playerController.holdPoint.localPosition;
        }
    }

    void Update()
    {
        if (!IsOwner || playerController == null) return;

        if (Input.GetKey(KeyCode.LeftShift) && playerController.currentlyHeldItem != null)
        {
            ResetHoldPointPos();
            DropHeldItemServerRpc();
        }

        if (playerController.currentlyHeldItem != null)
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

                if (playerController.holdPoint != null)
                {
                    playerController.holdPoint.localPosition = originalHoldPointLocalPos + (chargedHoldOffset * chargeRatio);
                }
            }

            if (Input.GetMouseButtonUp(0) && isChargingThrow)
            {
                float chargeRatio = Mathf.Clamp01(currentChargeTime / maxChargeTime);
                float finalForce = Mathf.Lerp(minThrowForce, maxThrowForce, chargeRatio);

                ResetHoldPointPos();

                Vector3 throwDir = playerController.cameraHolder != null ? playerController.cameraHolder.forward : transform.forward;
                ThrowHeldItemServerRpc(throwDir, finalForce);
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
        if (playerController != null && playerController.holdPoint != null)
        {
            playerController.holdPoint.localPosition = originalHoldPointLocalPos;
        }
    }

    [ServerRpc]
    private void ThrowHeldItemServerRpc(Vector3 throwDir, float force)
    {
        if (playerController.currentlyHeldItem != null)
        {
            playerController.currentlyHeldItem.ThrowRequestedByPlayer(playerController, throwDir, force);
        }
    }

    [ServerRpc]
    private void DropHeldItemServerRpc()
    {
        if (playerController.currentlyHeldItem != null)
        {
            playerController.currentlyHeldItem.DropRequestedByPlayer(playerController);
        }
    }
}
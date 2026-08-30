using Unity.Netcode;
using UnityEngine;

public class PlayerAnimation : NetworkBehaviour
{
    public Animator animator;
    private PlayerController playerController;

    public float dampTime = 0.1f;

    private Vector3 lastPosition;

    private void Awake()
    {
        playerController = GetComponent<PlayerController>();
        
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }
    }

    private void Start()
    {
        lastPosition = transform.position;
    }

    private void Update()
    {
        if (animator == null || playerController == null) return;

        float targetSpeed = 0f;
        bool isCrouched = false;

        if (IsOwner)
        {
            float x = Input.GetAxis("Horizontal");
            float z = Input.GetAxis("Vertical");
            bool isMoving = (x != 0 || z != 0);

            if (isMoving && !playerController.isHiding.Value)
            {
                bool isSprinting = Input.GetKey(KeyCode.LeftShift) && 
                                   playerController.currentlyHeldItem == null && 
                                   !playerController.isCrouching.Value;

                targetSpeed = isSprinting ? 1.0f : 0.5f; 
            }

            isCrouched = playerController.isCrouching.Value;
        }
        else
        {
            float distanceMoved = Vector3.Distance(transform.position, lastPosition);
            float currentSpeed = distanceMoved / Time.deltaTime;
            lastPosition = transform.position;

            if (currentSpeed > 0.1f && !playerController.isHiding.Value)
            {
                targetSpeed = currentSpeed > (playerController.baseMoveSpeed * 1.1f) ? 1.0f : 0.5f;
            }

            isCrouched = playerController.isCrouching.Value;
        }

        animator.SetFloat("Speed", targetSpeed, dampTime, Time.deltaTime);
        animator.SetBool("IsCrouching", isCrouched);
    }
}
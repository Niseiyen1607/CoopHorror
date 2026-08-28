using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class StalkerAI : NetworkBehaviour
{
    public enum StalkerState
    {
        Hunting,     
        Stalking,   
        Rushing,     
        Staring,     
        Retreating   
    }

    [Header("Animations")]
    [SerializeField] private Animator animator;

    [Header("Vitesses")]
    public float huntSpeed = 3.0f;
    public float stalkSpeed = 2.2f;
    public float rushSpeed = 7.5f;
    public float retreatSpeed = 8.5f;

    [Header("Réglages de Traque & Duel")]
    public float rushDistance = 7.5f;
    public float attackDistance = 1.6f;
    public float playerVisionAngle = 70.0f;
    public float requiredStareDuration = 3.5f;

    [Header("Audio (Via AudioManager)")]
    public AudioClip footstepWalkSound;  
    public AudioClip footstepRushSound;  
    public AudioClip spawnScreamSound;   
    public float walkStepInterval = 0.55f;
    public float runStepInterval = 0.28f;

    private NavMeshAgent agent;
    private StalkerState currentState = StalkerState.Hunting;
    private PlayerController targetPlayer;

    private float currentStareTime = 0f;
    private float outOfSightTimer = 0f;
    private float repathTimer = 0f;
    private float retreatTimer = 0f;

    private bool isClientWalking = false;
    private bool isClientRunning = false;
    private float stepTimer = 0f;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
    }

    public override void OnNetworkSpawn()
    {
        if (!IsServer)
        {
            agent.enabled = false;
            return;
        }

        PlaySpawnScreamClientRpc();

        currentState = StalkerState.Hunting;
        agent.speed = huntSpeed;
        UpdateAnimationStateClientRpc(isWalking: true, isRunning: false);

        SelectBestTarget();
    }

    private void Update()
    {
        HandleClientFootsteps();

        if (!IsServer) return;

        switch (currentState)
        {
            case StalkerState.Hunting:
                HandleHunting();
                break;

            case StalkerState.Stalking:
                HandleStalking();
                break;

            case StalkerState.Rushing:
                HandleRushing();
                break;

            case StalkerState.Staring:
                HandleStaring();
                break;

            case StalkerState.Retreating:
                HandleRetreating();
                break;
        }
    }

    private void HandleClientFootsteps()
    {
        if (!isClientWalking && !isClientRunning) return;

        stepTimer -= Time.deltaTime;
        if (stepTimer <= 0f)
        {
            if (AudioManager.Instance != null)
            {
                AudioClip clipToPlay = isClientRunning ? footstepRushSound : footstepWalkSound;
                if (clipToPlay != null)
                {
                    AudioManager.Instance.PlaySound3D(clipToPlay, transform.position, volume: 0.8f, minDistance: 2f, maxDistance: 25f, pitchRandomness: 0.08f);
                }
            }

            stepTimer = isClientRunning ? runStepInterval : walkStepInterval;
        }
    }

    private void HandleHunting()
    {
        repathTimer += Time.deltaTime;
        if (repathTimer >= 2.0f || targetPlayer == null)
        {
            SelectBestTarget();
            repathTimer = 0f;
        }

        if (targetPlayer != null)
        {
            agent.SetDestination(targetPlayer.transform.position);

            float dist = Vector3.Distance(transform.position, targetPlayer.transform.position);
            if (dist <= 14f)
            {
                currentState = StalkerState.Stalking;
                agent.speed = stalkSpeed;
                UpdateAnimationStateClientRpc(isWalking: true, isRunning: false);
            }
        }

        if (IsVisibleToAnyPlayer())
        {
            EnterStaring();
        }
    }

    private void HandleStalking()
    {
        if (targetPlayer == null)
        {
            currentState = StalkerState.Hunting;
            return;
        }

        if (IsVisibleToAnyPlayer())
        {
            EnterStaring();
            return;
        }

        Vector3 behindPlayerPos = targetPlayer.transform.position - (targetPlayer.transform.forward * 2.5f);
        if (NavMesh.SamplePosition(behindPlayerPos, out NavMeshHit hit, 3f, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
        }
        else
        {
            agent.SetDestination(targetPlayer.transform.position);
        }

        float dist = Vector3.Distance(transform.position, targetPlayer.transform.position);
        Vector3 dirToMonster = (transform.position - targetPlayer.transform.position).normalized;
        float angleFacing = Vector3.Angle(targetPlayer.transform.forward, dirToMonster);

        if (dist <= rushDistance && angleFacing > 100f)
        {
            currentState = StalkerState.Rushing;
            agent.speed = rushSpeed;
            UpdateAnimationStateClientRpc(isWalking: false, isRunning: true);
        }
    }

    private void HandleRushing()
    {
        if (targetPlayer == null || targetPlayer.isDead.Value)
        {
            EnterRetreat();
            return;
        }

        if (IsVisibleToAnyPlayer())
        {
            EnterStaring();
            return;
        }

        agent.SetDestination(targetPlayer.transform.position);

        float dist = Vector3.Distance(transform.position, targetPlayer.transform.position);
        if (dist <= attackDistance)
        {
            AttackPlayer(targetPlayer);
            EnterRetreat(); 
        }
    }

    private void EnterStaring()
    {
        currentState = StalkerState.Staring;
        agent.isStopped = true;

        UpdateAnimationStateClientRpc(isWalking: false, isRunning: false);
        currentStareTime = 0f;
    }

    private void HandleStaring()
    {
        if (targetPlayer != null)
        {
            Vector3 lookDir = (targetPlayer.transform.position - transform.position).normalized;
            lookDir.y = 0;
            if (lookDir != Vector3.zero)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookDir), Time.deltaTime * 5f);
            }
        }

        bool isBeingWatched = IsVisibleToAnyPlayer();

        if (isBeingWatched)
        {
            currentStareTime += Time.deltaTime;
            if (currentStareTime >= requiredStareDuration)
            {
                EnterRetreat();
            }
        }
        else
        {
            agent.isStopped = false;
            currentState = StalkerState.Rushing;
            agent.speed = rushSpeed;
            UpdateAnimationStateClientRpc(isWalking: false, isRunning: true);
        }
    }

    private void EnterRetreat()
    {
        currentState = StalkerState.Retreating;
        agent.isStopped = false;
        agent.speed = retreatSpeed;

        UpdateAnimationStateClientRpc(isWalking: false, isRunning: true);
        FindFleePosition();

        outOfSightTimer = 0f;
        retreatTimer = 0f;
    }

    private void HandleRetreating()
    {
        retreatTimer += Time.deltaTime;

        if (retreatTimer >= 2.0f || (!agent.pathPending && agent.remainingDistance <= 1.5f))
        {
            FindFleePosition();
            retreatTimer = 0f;
        }

        if (!IsVisibleToAnyPlayer())
        {
            outOfSightTimer += Time.deltaTime;
            if (outOfSightTimer >= 1.5f)
            {
                GetComponent<NetworkObject>().Despawn();
            }
        }
        else
        {
            outOfSightTimer = 0f;
        }
    }

    private void SelectBestTarget()
    {
        PlayerController[] players = FindObjectsOfType<PlayerController>();
        if (players.Length == 0) return;

        PlayerController bestTarget = null;
        float maxScore = -999f;

        foreach (var p in players)
        {
            if (p == null || p.isDead.Value) continue;

            float allyDist = GetDistanceToNearestAlly(p, players);
            float monsterDist = Vector3.Distance(transform.position, p.transform.position);

            float score = (allyDist * 2f) - (monsterDist * 0.5f);
            if (score > maxScore)
            {
                maxScore = score;
                bestTarget = p;
            }
        }

        targetPlayer = bestTarget;
    }

    private float GetDistanceToNearestAlly(PlayerController player, PlayerController[] allPlayers)
    {
        float min = 50f;
        foreach (var other in allPlayers)
        {
            if (other == player) continue;
            float d = Vector3.Distance(player.transform.position, other.transform.position);
            if (d < min) min = d;
        }
        return min;
    }

    private bool IsVisibleToAnyPlayer()
    {
        PlayerController[] players = FindObjectsOfType<PlayerController>();

        foreach (var player in players)
        {
            if (player == null || player.isDead.Value) continue;

            Transform camTransform = player.GetComponentInChildren<Camera>()?.transform;
            if (camTransform == null) continue;

            Vector3 monsterHead = transform.position + Vector3.up * 1.3f;
            Vector3 dir = monsterHead - camTransform.position;
            float angle = Vector3.Angle(camTransform.forward, dir.normalized);

            if (angle < playerVisionAngle)
            {
                if (Physics.Raycast(camTransform.position, dir.normalized, out RaycastHit hit, 35f))
                {
                    if (hit.collider.transform.root == transform.root)
                    {
                        return true;
                    }
                }
            }
        }
        return false;
    }

    private void FindFleePosition()
    {
        PlayerController closest = null;
        float min = Mathf.Infinity;

        foreach (var p in FindObjectsOfType<PlayerController>())
        {
            float d = Vector3.Distance(transform.position, p.transform.position);
            if (d < min) { min = d; closest = p; }
        }

        Vector3 fleeDir = closest != null ? (transform.position - closest.transform.position).normalized : transform.forward;
        Vector3 targetPos = transform.position + (fleeDir * 22f) + (Random.insideUnitSphere * 5f);

        if (NavMesh.SamplePosition(targetPos, out NavMeshHit hit, 20f, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
        }
    }

    private void AttackPlayer(PlayerController player)
    {
        if (player != null && !player.isDead.Value)
        {
            Debug.Log($"<color=red> [STALKER] A DÉVORÉ {player.playerName.Value} !</color>");
            player.Die();
        }
    }
    
    [ClientRpc]
    private void UpdateAnimationStateClientRpc(bool isWalking, bool isRunning)
    {
        isClientWalking = isWalking;
        isClientRunning = isRunning;

        if (animator != null)
        {
            animator.SetBool("IsWalking", isWalking);
            animator.SetBool("IsRunning", isRunning);
        }
    }

    [ClientRpc]
    private void PlaySpawnScreamClientRpc()
    {
        if (animator != null) animator.SetTrigger("Scream");
        
        if (AudioManager.Instance != null && spawnScreamSound != null)
        {
            AudioManager.Instance.PlaySound3D(spawnScreamSound, transform.position, volume: 1.0f, minDistance: 3f, maxDistance: 35f);
        }
    }
}
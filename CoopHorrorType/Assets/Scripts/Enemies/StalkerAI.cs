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

    [Header("Vitesses (Plus Rapides & Agressives)")]
    public float huntSpeed = 4.0f;
    public float stalkSpeed = 3.2f;
    public float rushSpeed = 8.5f;     
    public float retreatSpeed = 9.0f;

    [Header("Réglages de Traque & Duel")]
    public float rushDistance = 8.0f;
    public float attackDistance = 1.6f;
    public float stareMaxDistance = 15.0f;       
    
    [Header("Délais du Duel (Style Lethal Company)")]
    public float timeToScareMonster = 1.2f;    
    public float timeToEnrageMonster = 3.0f;  
    
    public float playerVisionAngle = 65.0f;

    [Header("Réglages Anti-Spam Audio")]
    public float screamCooldown = 8.0f; 

    [Header("Audio (Via AudioManager)")]
    public AudioClip footstepWalkSound;  
    public AudioClip footstepRushSound;  
    public AudioClip attackScreamSound;  
    public float walkStepInterval = 0.45f;
    public float runStepInterval = 0.22f;

    private NavMeshAgent agent;
    private StalkerState currentState = StalkerState.Hunting;
    private PlayerController targetPlayer;

    private float currentStareTime = 0f;
    private float outOfSightTimer = 0f;
    private float repathTimer = 0f;
    private float retreatTimer = 0f;
    private float lastScreamTime = -999f;
    private float rushStateTimer = 0f;

    private bool isClientWalking = false;
    private bool isClientRunning = false;
    private float stepTimer = 0f;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
    }

    private bool IsAgentValid()
    {
        return agent != null && agent.enabled && agent.isOnNavMesh;
    }

    public override void OnNetworkSpawn()
    {
        if (!IsServer)
        {
            if (agent != null) agent.enabled = false;
            return;
        }

        if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 5f, NavMesh.AllAreas))
        {
            agent.Warp(hit.position);
        }

        currentState = StalkerState.Hunting;
        if (IsAgentValid()) agent.speed = huntSpeed;
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
                    AudioManager.Instance.PlaySound3D(clipToPlay, transform.position, volume: isClientRunning ? 0.9f : 0.4f, minDistance: 2f, maxDistance: 25f, pitchRandomness: 0.08f);
                }
            }

            stepTimer = isClientRunning ? runStepInterval : walkStepInterval;
        }
    }

    private void HandleHunting()
    {
        repathTimer += Time.deltaTime;

        if (repathTimer >= 1.5f || targetPlayer == null || targetPlayer.isDead.Value || targetPlayer.isHiding.Value)
        {
            SelectBestTarget();
            repathTimer = 0f;
        }

        if (targetPlayer != null && IsAgentValid())
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

        if (IsVisibleToAnyPlayer(stareMaxDistance))
        {
            EnterStaring();
        }
    }

    private void HandleStalking()
    {
        if (targetPlayer == null || targetPlayer.isDead.Value || targetPlayer.isHiding.Value)
        {
            currentState = StalkerState.Hunting;
            return;
        }

        if (IsVisibleToAnyPlayer(stareMaxDistance))
        {
            EnterStaring();
            return;
        }

        Vector3 behindPlayerPos = targetPlayer.transform.position - (targetPlayer.transform.forward * 2.5f);
        if (NavMesh.SamplePosition(behindPlayerPos, out NavMeshHit hit, 3f, NavMesh.AllAreas))
        {
            if (IsAgentValid()) agent.SetDestination(hit.position);
        }
        else if (IsAgentValid())
        {
            agent.SetDestination(targetPlayer.transform.position);
        }

        float dist = Vector3.Distance(transform.position, targetPlayer.transform.position);
        Vector3 dirToMonster = (transform.position - targetPlayer.transform.position).normalized;
        float angleFacing = Vector3.Angle(targetPlayer.transform.forward, dirToMonster);

        if (dist <= rushDistance && angleFacing > 90f)
        {
            EnterRush();
        }
    }

    private void EnterRush()
    {
        if (currentState == StalkerState.Rushing) return;

        currentState = StalkerState.Rushing;
        rushStateTimer = 0f;

        if (IsAgentValid())
        {
            agent.isStopped = false;
            agent.speed = rushSpeed;
        }
        UpdateAnimationStateClientRpc(isWalking: false, isRunning: true);

        if (Time.time - lastScreamTime >= screamCooldown)
        {
            lastScreamTime = Time.time;
            PlayAttackScreamClientRpc();
        }
    }

    private void HandleRushing()
    {
        if (targetPlayer == null || targetPlayer.isDead.Value || targetPlayer.isHiding.Value)
        {
            Debug.Log("<color=yellow>[STALKER] La cible s'est cachée ! Abandon et fuite !</color>");
            EnterRetreat();
            return;
        }

        rushStateTimer += Time.deltaTime;

        if (rushStateTimer >= 0.4f && IsVisibleToAnyPlayer(stareMaxDistance))
        {
            EnterStaring();
            return;
        }

        if (IsAgentValid()) agent.SetDestination(targetPlayer.transform.position);

        float dist = Vector3.Distance(transform.position, targetPlayer.transform.position);
        if (dist <= attackDistance)
        {
            AttackPlayer(targetPlayer);
            EnterRetreat();
        }
    }

    private void EnterStaring()
    {
        if (currentState == StalkerState.Staring) return;

        currentState = StalkerState.Staring;
        if (IsAgentValid()) agent.isStopped = true;

        UpdateAnimationStateClientRpc(isWalking: false, isRunning: false);
        currentStareTime = 0f;
    }

    private void HandleStaring()
    {
        if (targetPlayer != null && !targetPlayer.isHiding.Value)
        {
            Vector3 lookDir = (targetPlayer.transform.position - transform.position).normalized;
            lookDir.y = 0;
            if (lookDir != Vector3.zero)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookDir), Time.deltaTime * 6f);
            }
        }

        bool isBeingWatched = IsVisibleToAnyPlayer(stareMaxDistance);

        if (isBeingWatched)
        {
            currentStareTime += Time.deltaTime;

            if (currentStareTime >= timeToScareMonster && currentStareTime < timeToEnrageMonster)
            {
                Debug.Log("<color=green>[STALKER] Repéré ! Le monstre prend peur et s'enfuit !</color>");
                EnterRetreat();
                return;
            }

            if (currentStareTime >= timeToEnrageMonster)
            {
                Debug.Log("<color=red>[STALKER ENRAGÉ] Fixé trop longtemps ! CHARGE !</color>");
                EnterRush();
                return;
            }
        }
        else
        {
            EnterRush();
        }
    }

    private void EnterRetreat()
    {
        if (currentState == StalkerState.Retreating) return;

        currentState = StalkerState.Retreating;
        if (IsAgentValid())
        {
            agent.isStopped = false;
            agent.speed = retreatSpeed;
        }

        UpdateAnimationStateClientRpc(isWalking: false, isRunning: true);
        FindFleePosition();

        outOfSightTimer = 0f;
        retreatTimer = 0f;
    }

    private void HandleRetreating()
    {
        retreatTimer += Time.deltaTime;

        if (IsAgentValid() && (retreatTimer >= 2.0f || (!agent.pathPending && agent.remainingDistance <= 1.5f)))
        {
            FindFleePosition();
            retreatTimer = 0f;
        }

        if (!IsVisibleToAnyPlayer(35f))
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
            if (p == null || p.isDead.Value || p.isHiding.Value) continue;

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
            if (other == player || other == null || other.isDead.Value || other.isHiding.Value) continue;
            float d = Vector3.Distance(player.transform.position, other.transform.position);
            if (d < min) min = d;
        }
        return min;
    }

    private bool IsVisibleToAnyPlayer(float maxCheckDistance)
    {
        PlayerController[] players = FindObjectsOfType<PlayerController>();

        foreach (var player in players)
        {
            if (player == null || player.isDead.Value || player.isHiding.Value) continue;

            Transform camTransform = player.GetComponentInChildren<Camera>()?.transform;
            if (camTransform == null) continue;

            Vector3 monsterHead = transform.position + Vector3.up * 1.3f;
            Vector3 dir = monsterHead - camTransform.position;
            float distance = dir.magnitude;

            if (distance > maxCheckDistance) continue;

            float angle = Vector3.Angle(camTransform.forward, dir.normalized);

            if (angle < playerVisionAngle)
            {
                if (Physics.Raycast(camTransform.position, dir.normalized, out RaycastHit hit, distance))
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

        // Chercher le joueur vivant le plus proche qui N'EST PAS caché !
        foreach (var p in FindObjectsOfType<PlayerController>())
        {
            if (p == null || p.isDead.Value || p.isHiding.Value) continue;
            float d = Vector3.Distance(transform.position, p.transform.position);
            if (d < min) { min = d; closest = p; }
        }

        Vector3 fleeDir = Vector3.forward;

        if (closest != null)
        {
            fleeDir = (transform.position - closest.transform.position).normalized;
        }
        else
        {
            fleeDir = -transform.forward + (Random.insideUnitSphere * 0.5f);
            fleeDir.y = 0;
        }

        for (int i = 0; i < 10; i++)
        {
            Vector3 targetPos = transform.position + (fleeDir * Random.Range(15f, 25f)) + (Random.insideUnitSphere * 5f);

            if (NavMesh.SamplePosition(targetPos, out NavMeshHit hit, 15f, NavMesh.AllAreas))
            {
                if (IsAgentValid())
                {
                    agent.SetDestination(hit.position);
                    return;
                }
            }
        }
    }

    private void AttackPlayer(PlayerController player)
    {
        if (player != null && !player.isDead.Value && !player.isHiding.Value)
        {
            Debug.Log($"<color=red>☠️ [STALKER] A DÉVORÉ {player.playerName.Value} !</color>");
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
    private void PlayAttackScreamClientRpc()
    {
        if (animator != null) animator.SetTrigger("Scream");
        
        if (AudioManager.Instance != null && attackScreamSound != null)
        {
            AudioManager.Instance.PlaySound3D(attackScreamSound, transform.position, volume: 1.0f, minDistance: 3f, maxDistance: 35f);
        }
    }
}
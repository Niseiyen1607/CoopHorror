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
        Searching, 
        Retreating   
    }

    [Header("Animations")]
    [SerializeField] private Animator animator;

    [Header("Jumpscare & Caméra")]
    public Transform jumpscareCameraPoint;

    [Header("Vitesses du Monstre")]
    public float huntSpeed = 4.0f;
    public float stalkSpeed = 3.2f;
    public float rushSpeed = 8.5f;     
    public float searchSpeed = 2.6f;
    public float retreatSpeed = 9.0f;

    [Header("Distances de Détection")]
    public float rushDistance = 8.0f;
    public float attackDistance = 1.6f;
    public float stareMaxDistance = 15.0f;       
    public float playerVisionAngle = 65.0f;

    [Header("Paramètres de Recherche")]
    public float searchAreaRadius = 10.0f;
    public int maxSearchWaypoints = 3;     

    [Header("Audio Anti-Spam")]
    public float screamCooldown = 8.0f; 

    [Header("Audio")]
    public AudioClip footstepWalkSound;  
    public AudioClip footstepRushSound;  
    public AudioClip attackScreamSound;  
    public float walkStepInterval = 0.45f;
    public float runStepInterval = 0.22f;

    private NavMeshAgent agent;
    private StalkerState currentState = StalkerState.Hunting;
    private PlayerController targetPlayer;

    private Vector3 searchCenterPosition;
    private int remainingSearchWaypoints = 0;
    private float inspectPauseTimer = 0f;
    private bool isInspectingSpot = false;
    private Quaternion targetInspectRotation;
    private bool sawPlayerHide = false;

    private float outOfSightTimer = 0f;
    private float repathTimer = 0f;
    private float retreatRepathTimer = 0f;
    private float lastScreamTime = -999f;

    private bool isClientWalking = false;
    private bool isClientRunning = false;
    private float stepTimer = 0f;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        if (animator == null) animator = GetComponentInChildren<Animator>();

        if (agent != null)
        {
            agent.acceleration = 50f;
            agent.angularSpeed = 800f;
            agent.stoppingDistance = 0.8f;
            agent.autoBraking = true;
        }
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

        if (targetPlayer != null && !targetPlayer.isHiding.Value && !targetPlayer.isDead.Value)
        {
            searchCenterPosition = targetPlayer.transform.position;
        }

        CheckVoiceDetection();

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

            case StalkerState.Searching:
                HandleSearching();
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

        if (repathTimer >= 1.0f || targetPlayer == null || targetPlayer.isDead.Value || targetPlayer.isHiding.Value)
        {
            CheckTargetStatusOnLost();
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
            CheckTargetStatusOnLost();
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
            CheckTargetStatusOnLost();
            return;
        }

        if (IsVisibleToAnyPlayer(stareMaxDistance))
        {
            EnterStaring();
            return;
        }

        if (IsAgentValid()) agent.SetDestination(targetPlayer.transform.position);

        float dist = Vector3.Distance(transform.position, targetPlayer.transform.position);
        if (dist <= attackDistance)
        {
            AttackPlayer(targetPlayer);
        }
    }

    private void CheckTargetStatusOnLost()
    {
        if (targetPlayer != null && targetPlayer.isHiding.Value)
        {
            float dist = Vector3.Distance(transform.position, targetPlayer.transform.position);
            bool hadLineOfSight = HasDirectLineOfSight(targetPlayer.transform.position);

            if (currentState == StalkerState.Rushing || (dist <= 9f && hadLineOfSight))
            {
                sawPlayerHide = true;
                EnterSearching(targetPlayer.transform.position);
                return;
            }
        }

        SelectBestTarget();

        if (targetPlayer == null)
        {
            EnterSearching(searchCenterPosition);
        }
    }

    private void EnterSearching(Vector3 originPos)
    {
        currentState = StalkerState.Searching;
        searchCenterPosition = originPos != Vector3.zero ? originPos : transform.position;
        remainingSearchWaypoints = maxSearchWaypoints;
        isInspectingSpot = false;

        if (IsAgentValid())
        {
            agent.isStopped = false;
            agent.speed = searchSpeed;
            agent.SetDestination(searchCenterPosition);
        }

        UpdateAnimationStateClientRpc(isWalking: true, isRunning: false);
    }

    private void HandleSearching()
    {
        if (sawPlayerHide && targetPlayer != null && targetPlayer.isHiding.Value)
        {
            float distToLocker = Vector3.Distance(transform.position, targetPlayer.transform.position);
            if (distToLocker <= attackDistance + 0.8f)
            {
                if (targetPlayer.currentHidingSpot != null)
                {
                    targetPlayer.currentHidingSpot.ExitHidingSpot(targetPlayer);
                }
                AttackPlayer(targetPlayer);
                sawPlayerHide = false;
                return;
            }
        }

        SelectBestTarget();
        if (targetPlayer != null && !targetPlayer.isHiding.Value)
        {
            sawPlayerHide = false;
            currentState = StalkerState.Hunting;
            if (IsAgentValid()) agent.speed = huntSpeed;
            return;
        }

        if (isInspectingSpot)
        {
            inspectPauseTimer -= Time.deltaTime;

            transform.rotation = Quaternion.Slerp(transform.rotation, targetInspectRotation, Time.deltaTime * 2.5f);

            if (inspectPauseTimer <= 0f)
            {
                isInspectingSpot = false;
                remainingSearchWaypoints--;

                if (remainingSearchWaypoints <= 0)
                {
                    sawPlayerHide = false;
                    EnterRetreat();
                    return;
                }

                MoveToNextSearchWaypoint();
            }
            return;
        }

        if (IsAgentValid() && !agent.pathPending && agent.remainingDistance <= 1.2f)
        {
            isInspectingSpot = true;
            inspectPauseTimer = Random.Range(1.8f, 3.0f);
            
            float randomAngle = Random.Range(0, 2) == 0 ? Random.Range(-60f, -30f) : Random.Range(30f, 60f);
            targetInspectRotation = transform.rotation * Quaternion.Euler(0, randomAngle, 0);

            agent.isStopped = true;
            UpdateAnimationStateClientRpc(isWalking: false, isRunning: false);
        }
    }

    private void MoveToNextSearchWaypoint()
    {
        if (!IsAgentValid()) return;

        for (int i = 0; i < 8; i++)
        {
            Vector3 randomOffset = (Random.insideUnitSphere * Random.Range(4f, searchAreaRadius));
            randomOffset.y = 0;
            Vector3 candidatePos = searchCenterPosition + randomOffset;

            if (NavMesh.SamplePosition(candidatePos, out NavMeshHit hit, 6f, NavMesh.AllAreas))
            {
                agent.isStopped = false;
                agent.speed = searchSpeed;
                agent.SetDestination(hit.position);
                UpdateAnimationStateClientRpc(isWalking: true, isRunning: false);
                return;
            }
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
        retreatRepathTimer = 0f;
    }

    private void HandleRetreating()
    {
        retreatRepathTimer += Time.deltaTime;

        if (IsAgentValid() && (retreatRepathTimer >= 2.0f || (!agent.pathPending && agent.remainingDistance <= 2f)))
        {
            FindFleePosition();
            retreatRepathTimer = 0f;
        }

        if (IsFullyHiddenAndFarFromAllPlayers(30f))
        {
            outOfSightTimer += Time.deltaTime;
            if (outOfSightTimer >= 2.5f) 
            {
                GetComponent<NetworkObject>().Despawn();
            }
        }
        else
        {
            outOfSightTimer = 0f; 
        }
    }

    private bool IsFullyHiddenAndFarFromAllPlayers(float minDistanceToDespawn)
    {
        PlayerController[] players = FindObjectsOfType<PlayerController>();

        foreach (var player in players)
        {
            if (player == null || player.isDead.Value) continue;

            float dist = Vector3.Distance(transform.position, player.transform.position);
            if (dist < minDistanceToDespawn) return false;

            Camera cam = player.GetComponentInChildren<Camera>();
            if (cam == null) continue;

            Vector3[] checkPoints = new Vector3[]
            {
                transform.position + Vector3.up * 1.5f,
                transform.position + Vector3.up * 0.5f
            };

            foreach (var point in checkPoints)
            {
                Vector3 dir = point - cam.transform.position;
                if (Physics.Raycast(cam.transform.position, dir.normalized, out RaycastHit hit, dir.magnitude))
                {
                    if (hit.collider.transform.root == transform.root)
                    {
                        return false; 
                    }
                }
            }
        }

        return true;
    }

    private void EnterStaring()
    {
        if (currentState == StalkerState.Staring) return;

        currentState = StalkerState.Staring;
        if (IsAgentValid()) agent.isStopped = true; 

        UpdateAnimationStateClientRpc(isWalking: false, isRunning: false);
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
            if (IsAgentValid()) agent.isStopped = true;
        }
        else
        {
            EnterRush();
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

    private void CheckVoiceDetection()
    {
        if (currentState != StalkerState.Hunting && 
            currentState != StalkerState.Stalking && 
            currentState != StalkerState.Searching)
        {
            return;
        }

        PlayerController[] players = FindObjectsOfType<PlayerController>();

        foreach (var player in players)
        {
            if (player == null || player.isDead.Value) continue;

            if (player.TryGetComponent<PlayerMicDetector>(out var mic))
            {
                if (mic.isSpeaking.Value)
                {
                    float dist = Vector3.Distance(transform.position, player.transform.position);

                    if (player.isHiding.Value && dist <= 10f)
                    {
                        Debug.Log($"<color=red>[STALKER] A ENTENDU LE JOUEUR RESPIRER DANS LE CASIER ({dist:F1}m) !</color>");
                        
                        targetPlayer = player;
                        sawPlayerHide = true;

                        EnterSearching(player.transform.position);
                        return;
                    }

                    if (!player.isHiding.Value && dist <= 18f)
                    {
                        Debug.Log($"<color=red>[STALKER] A ENTENDU {player.playerName.Value} PARLER ({dist:F1}m) !</color>");

                        targetPlayer = player;
                        sawPlayerHide = false;

                        if (dist <= rushDistance)
                        {
                            EnterRush();
                        }
                        else
                        {
                            currentState = StalkerState.Hunting;
                            if (IsAgentValid())
                            {
                                agent.isStopped = false;
                                agent.speed = huntSpeed;
                                agent.SetDestination(player.transform.position);
                            }
                            UpdateAnimationStateClientRpc(isWalking: true, isRunning: false);
                        }
                        return;
                    }
                }
            }
        }
    }

    private bool HasDirectLineOfSight(Vector3 targetPos)
    {
        Vector3 eyePos = transform.position + Vector3.up * 1.5f;
        Vector3 targetCenter = targetPos + Vector3.up * 1.0f;
        Vector3 dir = (targetCenter - eyePos);

        if (Physics.Raycast(eyePos, dir.normalized, out RaycastHit hit, dir.magnitude))
        {
            if (hit.collider.CompareTag("Player") || hit.collider.GetComponentInParent<PlayerController>() != null || hit.collider.GetComponentInParent<HidingSpot>() != null)
            {
                return true;
            }
        }
        return false;
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
        PlayerController nearestPlayer = null;
        float min = Mathf.Infinity;

        foreach (var p in FindObjectsOfType<PlayerController>())
        {
            if (p == null || p.isDead.Value) continue;
            float d = Vector3.Distance(transform.position, p.transform.position);
            if (d < min) { min = d; nearestPlayer = p; }
        }

        Vector3 fleeDir = transform.forward;

        if (nearestPlayer != null)
        {
            fleeDir = (transform.position - nearestPlayer.transform.position).normalized;
            fleeDir.y = 0;
        }

        if (fleeDir == Vector3.zero) fleeDir = -transform.forward;

        for (int i = 0; i < 10; i++)
        {
            Vector3 targetPos = transform.position + (fleeDir * Random.Range(25f, 38f)) + (Random.insideUnitSphere * 8f);

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
        if (player != null && !player.isDead.Value)
        {
            StartCoroutine(AttackAndJumpscareRoutine(player));
        }
    }

    private IEnumerator AttackAndJumpscareRoutine(PlayerController player)
    {
        if (IsAgentValid())
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
        }

        Vector3 lookDir = (player.transform.position - transform.position).normalized;
        lookDir.y = 0;
        if (lookDir != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(lookDir);
        }

        if (animator != null) animator.SetTrigger("Jumpscare");
        PlayAttackScreamClientRpc();

        player.TriggerJumpscareClientRpc(NetworkObjectId);

        yield return new WaitForSeconds(1.5f);

        EnterRetreat();
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
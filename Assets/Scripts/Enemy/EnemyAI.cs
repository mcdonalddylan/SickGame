using UnityEngine;

// State machine for the enemy. All timings/ranges are editable in the inspector.
[RequireComponent(typeof(EnemyController))]
public class EnemyAI : MonoBehaviour
{
    public enum State { Moving, Standing, Turning, WalkingAway, Copy, Tumbling, MeleeAttack, RangedAttack, Dying }

    [Header("References")]
    public EnemyThreatRange threatRange;
    public Transform meleeHitPoint;
    public Transform projectileSpawnPoint;
    public EnemyProjectile projectilePrefab;

    [Header("Randomness weights")]
    [Tooltip("Chance (0-1) to pick Moving when choosing the next patrol behavior")]
    [Range(0f, 1f)] public float moveWeight = 0.5f;
    [Range(0f, 1f)] public float standWeight = 0.3f;
    [Range(0f, 1f)] public float turnWeight = 0.2f;
    [Range(0f, 1f)] public float copyWeight = 0.15f;

    [Header("Patrol Movement")]
    public float moveSpeed = 4f;
    public Vector2 moveDurationRange = new Vector2(1.5f, 4f);
    public Vector2 standDurationRange = new Vector2(1f, 3f);
    public float turnDuration = 0.3f;
    [Tooltip("After bumping into another enemy the enemy turns and walks away for this many seconds")]
    public float walkAwayDuration = 1.5f;

    [Header("Alert (player enters threat range)")]
    [Tooltip("Extra Y scale added at the peak of the alert pop (0.15 = +15%)")]
    public float alertScaleAmount = 0.15f;
    public float alertDuration = 0.15f;
    [Tooltip("Child object (model or head) that gets stretched. Must NOT be the root and must not hold the collider. Its feet stay planted")]
    public Transform alertScaleTarget;
    [Tooltip("Local Y (in the enemy root's space) that stays fixed while stretching. Leave on 'Use Capsule Bottom' for the feet")]
    public bool useCapsuleBottomAsAnchor = true;
    public float customAnchorLocalY = 0f;
    [Tooltip("Where the black lines come from. If empty, a point above the enemy's center is used")]
    public Transform foreheadPoint;
    public Vector3 defaultForeheadOffset = new Vector3(0.3f, 0.8f, 0f);

    [Header("Copy")]
    public Vector2 copyDurationRange = new Vector2(1f, 3f);
    [Tooltip("Optional. Animator bool set to true while in the Copy state (no animation exists yet)")]
    public Animator animator;
    public string copyAnimatorBool = "Copy";

    [Header("Tumble")]
    public float tumbleDuration = 1.2f;
    public Vector2 tumbleKnockback = new Vector2(8f, 10f);
    public float tumbleSpinSpeed = 540f;

    [Header("Death Flight")]
    public float deathLaunchSpeed = 22f;
    public float deathSpinSpeed = 1500f;
    [Range(0f, 1f)] public float deathBounciness = 0.8f;
    [Tooltip("How far past the camera edge (viewport units) the enemy must be before it explodes")]
    public float cameraExitMargin = 0.08f;
    [Tooltip("Safety: explode after this many seconds even if still on screen")]
    public float deathMaxTime = 8f;
    [Tooltip("Optional. If empty, a procedural explosion is used")]
    public GameObject deathExplosionPrefab;

    [Header("Melee Attack")]
    public float meleeRange = 2f;
    public float meleeWindup = 0.3f;
    public float meleeRecovery = 0.4f;
    public float meleeHitRadius = 1f;
    public float meleeDamage = 1f;
    public float meleeCooldown = 1f;

    [Header("Ranged Attack")]
    [Tooltip("Ranged attack is used once the player is beyond this fraction of the threat radius")]
    [Range(0.1f, 1f)] public float rangedMinDistanceFraction = 0.65f;
    public float rangedWindup = 0.5f;
    public float rangedRecovery = 0.6f;
    public float rangedCooldown = 2f;

    [Header("Debug")]
    public State currentState;

    private EnemyController controller;
    private float stateTimer;
    private float meleeCooldownTimer;
    private float rangedCooldownTimer;
    private bool attackActionDone;
    private float tumbleRotation;
    private Vector3 baseScale;
    private Vector3 targetBaseScale;
    private Vector3 targetBasePosition;
    private Coroutine alertRoutine;
    private bool playerWasOnHead;
    private PlayerMovement stompTarget;

    private void Awake()
    {
        controller = GetComponent<EnemyController>();
        baseScale = transform.localScale;
        if (alertScaleTarget != null && alertScaleTarget != transform)
        {
            targetBaseScale = alertScaleTarget.localScale;
            targetBasePosition = alertScaleTarget.localPosition;
        }
        else alertScaleTarget = null;
    }

    private void OnEnable()
    {
        if (threatRange != null) threatRange.PlayerEntered += OnPlayerSpotted;
    }

    private void OnDisable()
    {
        if (threatRange != null) threatRange.PlayerEntered -= OnPlayerSpotted;
    }

    private void OnPlayerSpotted()
    {
        if (currentState == State.Dying) return;

        Vector3 origin = foreheadPoint != null
            ? foreheadPoint.position
            : transform.position + new Vector3(defaultForeheadOffset.x * controller.FacingSign, defaultForeheadOffset.y, defaultForeheadOffset.z);
        EnemyEffects.SpawnAlertLines(origin, controller.FacingSign);

        if (alertRoutine != null) StopCoroutine(alertRoutine);
        alertRoutine = StartCoroutine(AlertScaleAnimation());
    }

    private System.Collections.IEnumerator AlertScaleAnimation()
    {
        if (alertScaleTarget == null)
        {
            alertRoutine = null;
            yield break;
        }

        float anchorY = customAnchorLocalY;
        CapsuleCollider capsule = GetComponent<CapsuleCollider>();
        if (useCapsuleBottomAsAnchor && capsule != null)
            anchorY = capsule.center.y - capsule.height * 0.5f;

        float duration = Mathf.Max(alertDuration, 0.01f);
        for (float t = 0f; t < 1f; t += Time.deltaTime / duration)
        {
            float s = 1f + alertScaleAmount * Mathf.Sin(t * Mathf.PI);
            ApplyAlertScale(s, anchorY);
            yield return null;
        }
        ApplyAlertScale(1f, anchorY);
        alertRoutine = null;
    }

    // Stretches the target on Y around the anchor height so the feet never move
    private void ApplyAlertScale(float s, float anchorY)
    {
        alertScaleTarget.localScale = new Vector3(targetBaseScale.x, targetBaseScale.y * s, targetBaseScale.z);
        Vector3 p = targetBasePosition;
        p.y += (anchorY - targetBasePosition.y) * (1f - s);
        alertScaleTarget.localPosition = p;
    }

    private void Start()
    {
        ChoosePatrolState();
    }

    private void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;
        meleeCooldownTimer -= dt;
        rangedCooldownTimer -= dt;
        stateTimer -= dt;

        CheckHeadStomp();

        switch (currentState)
        {
            case State.Moving: UpdateMoving(); break;
            case State.Standing: UpdateStanding(); break;
            case State.Turning: UpdateTurning(); break;
            case State.WalkingAway: UpdateWalkingAway(); break;
            case State.Copy: UpdateCopy(); break;
            case State.Tumbling: UpdateTumbling(dt); break;
            case State.MeleeAttack: UpdateMelee(); break;
            case State.RangedAttack: UpdateRanged(); break;
            case State.Dying: UpdateDying(dt); break;
        }

        controller.Tick(dt);
    }

    // ---------- Patrol ----------

    private void ChoosePatrolState()
    {
        float turnW = CanTurnAround() ? turnWeight : 0f;
        float total = moveWeight + standWeight + turnW + copyWeight;
        float roll = Random.value * (total <= 0f ? 1f : total);

        if (roll < moveWeight) EnterState(State.Moving, Random.Range(moveDurationRange.x, moveDurationRange.y));
        else if (roll < moveWeight + standWeight) EnterState(State.Standing, Random.Range(standDurationRange.x, standDurationRange.y));
        else if (roll < moveWeight + standWeight + copyWeight) EnterState(State.Copy, Random.Range(copyDurationRange.x, copyDurationRange.y));
        else if (turnW > 0f) EnterState(State.Turning, turnDuration);
        else EnterState(State.Standing, Random.Range(standDurationRange.x, standDurationRange.y));
    }

    // While the player is inside the threat range the enemy may not turn around,
    // unless the player is behind it (turning then means facing the player)
    private bool CanTurnAround()
    {
        if (threatRange == null || !threatRange.PlayerInRange) return true;
        float toPlayerX = threatRange.Player.transform.position.x - transform.position.x;
        return Mathf.Sign(toPlayerX) != controller.FacingSign;
    }

    // Behaves like Standing; only the animation differs
    private void UpdateCopy()
    {
        controller.velocity.x = 0f;
        if (TryStartAttack()) return;
        if (stateTimer <= 0f) ChoosePatrolState();
    }

    private void SetCopyAnimation(bool active)
    {
        if (animator == null || string.IsNullOrEmpty(copyAnimatorBool)) return;
        foreach (AnimatorControllerParameter p in animator.parameters)
        {
            if (p.name == copyAnimatorBool && p.type == AnimatorControllerParameterType.Bool)
            {
                animator.SetBool(copyAnimatorBool, active);
                return;
            }
        }
    }

    private void UpdateMoving()
    {
        if (TryStartAttack()) return;

        // Bumped into another enemy: turn around and walk the other way
        if (controller.EnemyAhead())
        {
            controller.velocity.x = 0f;
            if (CanTurnAround()) EnterState(State.WalkingAway, walkAwayDuration);
            return;
        }

        // Turn around instead of walking into a wall or off a ledge (hold position if turning isn't allowed)
        if (controller.WallAhead() || (controller.IsGrounded && !controller.HasGroundAhead()))
        {
            controller.velocity.x = 0f;
            if (CanTurnAround()) EnterState(State.Turning, turnDuration);
            return;
        }

        controller.velocity.x = moveSpeed * controller.FacingSign;
        if (stateTimer <= 0f) ChoosePatrolState();
    }

    // Walks away after bumping
    private void UpdateWalkingAway()
    {
        if (TryStartAttack()) return;

        if (controller.WallAhead() || (controller.IsGrounded && !controller.HasGroundAhead()))
        {
            controller.velocity.x = 0f;
            if (CanTurnAround()) EnterState(State.Turning, turnDuration);
            return;
        }

        controller.velocity.x = moveSpeed * controller.FacingSign;
        if (stateTimer <= 0f) ChoosePatrolState();
    }

    private void UpdateStanding()
    {
        controller.velocity.x = 0f;
        if (TryStartAttack()) return;
        if (stateTimer <= 0f) ChoosePatrolState();
    }

    private void UpdateTurning()
    {
        controller.velocity.x = 0f;
        if (TryStartAttack()) return;
        if (stateTimer <= 0f) ChoosePatrolState();
    }

    // ---------- Tumble ----------

    // Call this from the player's attack when it successfully hits the enemy.
    // hitDirection.x > 0 knocks the enemy to the right.
    public void TakeHit(Vector2 hitDirection)
    {
        float side = hitDirection.x >= 0f ? 1f : -1f;
        controller.velocity = new Vector2(tumbleKnockback.x * side, tumbleKnockback.y);
        tumbleRotation = 0f;
        controller.CancelTurnAnimation();
        EnterState(State.Tumbling, tumbleDuration);
    }

    // ---------- Death ----------

    // Launches the enemy opposite to where it faces (biased by the hit direction)
    public void Die(Vector2 hitDirection)
    {
        float side = hitDirection.x >= 0f ? 1f : -1f;
        float x = -controller.FacingSign * 0.5f + side;
        if (Mathf.Abs(x) < 0.1f) x = -controller.FacingSign;
        Vector2 dir = new Vector2(x, 0.7f).normalized;

        controller.CancelTurnAnimation();
        controller.bounceOffSurfaces = true;
        controller.bounciness = deathBounciness;
        controller.velocity = dir * deathLaunchSpeed;
        tumbleRotation = 0f;
        EnterState(State.Dying, deathMaxTime);
    }

    private void UpdateDying(float dt)
    {
        tumbleRotation += deathSpinSpeed * dt * -Mathf.Sign(controller.velocity.x == 0f ? 1f : controller.velocity.x);
        transform.rotation = Quaternion.Euler(0f, 0f, tumbleRotation);

        if (stateTimer <= 0f || OutsideCamera())
            Explode();
    }

    private bool OutsideCamera()
    {
        Camera cam = Camera.main;
        if (cam == null) return false;
        Vector3 v = cam.WorldToViewportPoint(transform.position);
        float lo = -cameraExitMargin, hi = 1f + cameraExitMargin;
        return v.x < lo || v.x > hi || v.y < lo || v.y > hi;
    }

    private void Explode()
    {
        Vector3 dir = new Vector3(controller.velocity.x, controller.velocity.y, 0f);
        if (deathExplosionPrefab != null)
            Instantiate(deathExplosionPrefab, transform.position, Quaternion.LookRotation(dir.sqrMagnitude > 0.0001f ? dir : Vector3.up, Vector3.up));
        else
            EnemyEffects.SpawnExplosion(transform.position, dir);
        Destroy(gameObject);
    }

    private void UpdateTumbling(float dt)
    {
        // Spin on Z visually; gravity and wall collisions still apply via the controller
        tumbleRotation += tumbleSpinSpeed * dt * -Mathf.Sign(controller.velocity.x == 0f ? 1f : controller.velocity.x);
        transform.rotation = Quaternion.Euler(0f, controller.IsFacingRight ? 0f : 180f, tumbleRotation);

        if (controller.IsGrounded) controller.velocity.x = Mathf.MoveTowards(controller.velocity.x, 0f, 30f * dt);

        if (stateTimer <= 0f && controller.IsGrounded)
        {
            controller.SetFacing(controller.IsFacingRight);
            ChoosePatrolState();
        }
    }

    // ---------- Head stomp ----------

    // Player landing on this enemy's head triggers the melee attack, which always hits the player
    private void CheckHeadStomp()
    {
        PlayerMovement player = FindPlayerOnHead();
        bool onHead = player != null;

        if (onHead && !playerWasOnHead &&
            currentState != State.Dying && currentState != State.Tumbling && currentState != State.MeleeAttack)
        {
            stompTarget = player;
            controller.velocity.x = 0f;
            EnterState(State.MeleeAttack, meleeWindup + meleeRecovery);
        }

        playerWasOnHead = onHead;
    }

    private PlayerMovement FindPlayerOnHead()
    {
        if (threatRange == null || !threatRange.PlayerInRange) return null;
        PlayerMovement player = threatRange.Player;
        if (player == null || player.Controller == null) return null;

        Collider ground = player.Controller.GroundCollider;
        if (ground == null || ground.GetComponentInParent<EnemyAI>() != this) return null;
        return player;
    }

    // ---------- Attacks ----------

    private bool TryStartAttack()
    {
        if (threatRange == null || !threatRange.PlayerInRange) return false;

        Vector3 toPlayer = threatRange.Player.transform.position - transform.position;
        float distance = Mathf.Abs(toPlayer.x);
        float threatRadius = threatRange.Radius;

        if (distance <= meleeRange && meleeCooldownTimer <= 0f)
        {
            FacePlayer(toPlayer);
            EnterState(State.MeleeAttack, meleeWindup + meleeRecovery);
            return true;
        }

        if (distance >= threatRadius * rangedMinDistanceFraction && distance > meleeRange && rangedCooldownTimer <= 0f)
        {
            FacePlayer(toPlayer);
            EnterState(State.RangedAttack, rangedWindup + rangedRecovery);
            return true;
        }

        // In range but both attacks on cooldown / in-between distance: hold position facing the player
        if (currentState != State.Standing || stateTimer <= 0f)
        {
            FacePlayer(toPlayer);
        }
        return false;
    }

    private void FacePlayer(Vector3 toPlayer)
    {
        bool playerIsRight = toPlayer.x >= 0f;
        controller.TurnTo(playerIsRight);
    }

    private void UpdateMelee()
    {
        controller.velocity.x = 0f;
        float elapsed = meleeWindup + meleeRecovery - stateTimer;

        if (!attackActionDone && elapsed >= meleeWindup)
        {
            attackActionDone = true;
            DoMeleeHit();
        }

        if (stateTimer <= 0f)
        {
            meleeCooldownTimer = meleeCooldown;
            AfterAttack();
        }
    }

    private void DoMeleeHit()
    {
        if (stompTarget != null)
        {
            stompTarget.SendMessage("TakeDamage", meleeDamage, SendMessageOptions.DontRequireReceiver);
            stompTarget = null;
            return;
        }

        Vector3 point = meleeHitPoint != null
            ? meleeHitPoint.position
            : transform.position + Vector3.right * controller.FacingSign * meleeRange * 0.5f;

        foreach (Collider col in Physics.OverlapSphere(point, meleeHitRadius))
        {
            PlayerMovement player = col.GetComponentInParent<PlayerMovement>();
            if (player != null)
            {
                player.SendMessage("TakeDamage", meleeDamage, SendMessageOptions.DontRequireReceiver);
                break;
            }
        }
    }

    private void UpdateRanged()
    {
        controller.velocity.x = 0f;
        float elapsed = rangedWindup + rangedRecovery - stateTimer;

        if (!attackActionDone && elapsed >= rangedWindup)
        {
            attackActionDone = true;
            FireProjectile();
        }

        if (stateTimer <= 0f)
        {
            rangedCooldownTimer = rangedCooldown;
            AfterAttack();
        }
    }

    private void FireProjectile()
    {
        if (projectilePrefab == null) return;

        Vector3 spawn = projectileSpawnPoint != null ? projectileSpawnPoint.position : transform.position;
        Vector3 dir = Vector3.right * controller.FacingSign;
        if (threatRange != null && threatRange.PlayerInRange)
            dir = threatRange.Player.transform.position - spawn;

        EnemyProjectile projectile = Instantiate(projectilePrefab, spawn, Quaternion.identity);
        projectile.Launch(dir);
    }

    // Keep attacking while the player is still in range, otherwise return to patrolling
    private void AfterAttack()
    {
        if (threatRange != null && threatRange.PlayerInRange && TryStartAttack()) return;
        ChoosePatrolState();
    }

    // ---------- Helpers ----------

    private void EnterState(State state, float duration)
    {
        if (currentState == State.Copy && state != State.Copy) SetCopyAnimation(false);
        if (state == State.Copy) SetCopyAnimation(true);
        currentState = state;
        stateTimer = duration;
        attackActionDone = false;
        if (state == State.Turning || state == State.WalkingAway) controller.Turn();
        if (state != State.Moving) controller.velocity.x = (state == State.Tumbling || state == State.Dying) ? controller.velocity.x : 0f;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Vector3 point = meleeHitPoint != null ? meleeHitPoint.position : transform.position + transform.right * meleeRange * 0.5f;
        Gizmos.DrawWireSphere(point, meleeHitRadius);

        if (threatRange != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(threatRange.transform.position, threatRange.Radius * rangedMinDistanceFraction);
        }
    }
}

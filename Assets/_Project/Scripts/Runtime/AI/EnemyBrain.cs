using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;

namespace Ruinas
{
    public enum EnemyState { Spawning, Idle, Alert, Chase, Position, Windup, Attack, Recover, React, Stunned, Returning, Dead }

    /// <summary>
    /// Base da IA inimiga. Decisões (Think) acontecem em intervalos com defasagem aleatória por inimigo;
    /// a execução (Tick) roda todo quadro. Movimento pela NavMesh (não atravessa abismos), empurrão via
    /// agent.Move, espaço de ataque reservado ao redor do jogador e recuperação de travamento sem
    /// teleporte visível.
    /// </summary>
    [RequireComponent(typeof(Actor))]
    public abstract class EnemyBrain : MonoBehaviour, IKnockbackTarget
    {
        public EnemyDefinition Def { get; private set; }
        public Actor Actor { get; private set; }
        public EnemyState State { get; private set; } = EnemyState.Idle;
        public ArenaDirector OwnerEncounter { get; set; }
        public bool IsAlive => Actor != null && !Actor.IsDead && State != EnemyState.Dead;
        public Actor Target => target;
        public event Action<EnemyBrain> Despawned;

        protected NavMeshAgent agent;
        protected ProceduralAnimator anim;
        protected HitboxController hitbox;
        protected LevelContext ctx;
        protected Actor target;
        protected Vector3 home;
        protected float stateTime;
        protected float attackCooldown;
        protected float speedFactor = 1f;
        protected int slot = -1;
        protected bool slotRanged;
        protected bool frozenUntilRelease;

        AttackDefinition currentAttack;
        float attackT;
        bool attackOpened, attackClosed;
        int attackEventId;
        float attackDamage;
        Vector3 attackDir;
        Telegraph telegraph;

        Vector3 knock;
        float nextThink;
        float stuckTimer;
        Vector3 stuckRef;
        float hiddenStuck;
        bool despawning;

        public virtual void Init(EnemyDefinition def, LevelContext context, float healthMultiplier, float damageMultiplier, bool playSpawn)
        {
            Def = def;
            ctx = context;
            Actor = GetComponent<Actor>();
            agent = GetComponent<NavMeshAgent>();
            anim = GetComponentInChildren<ProceduralAnimator>();
            hitbox = GetComponent<HitboxController>();

            Actor.team = Team.Enemy;
            Actor.EnemyDef = def;
            Actor.displayName = def.displayName;
            Actor.Knockback = this;
            Actor.Stats.baseMoveSpeed = def.moveSpeed;
            Actor.Stats.globalDamage = damageMultiplier;
            Actor.Receiver.Init(def.maxHealth * healthMultiplier);
            Actor.Damaged += OnDamaged;
            Actor.Died += OnDied;

            if (agent != null)
            {
                agent.speed = def.moveSpeed;
                agent.angularSpeed = 0f;
                agent.updateRotation = false;
                agent.acceleration = 28f;
                agent.stoppingDistance = 0.1f;
                agent.autoBraking = true;
                agent.radius = Mathf.Max(0.25f, Actor.radius * 0.9f);
                agent.avoidancePriority = ctx.Random.Range(35, 65);
                agent.obstacleAvoidanceType = ObstacleAvoidanceType.MedQualityObstacleAvoidance;
            }
            home = transform.position;
            nextThink = Time.time + ctx.Random.Range(0f, def.decisionInterval);
            SetState(playSpawn ? EnemyState.Spawning : EnemyState.Idle);
            if (playSpawn) anim?.Play("spawn", 1f);
        }

        /// <summary>Mantém parado (modo de referência) até ser liberado.</summary>
        public void Freeze(bool frozen) => frozenUntilRelease = frozen;

        protected void SetState(EnemyState s)
        {
            State = s;
            stateTime = 0f;
            OnEnterState(s);
        }

        protected virtual void OnEnterState(EnemyState s) { }

        void OnDestroy()
        {
            if (Actor != null)
            {
                Actor.Damaged -= OnDamaged;
                Actor.Died -= OnDied;
            }
            ReleaseSlot();
            if (telegraph != null) telegraph.Hide();
        }

        void Update()
        {
            if (ctx == null || State == EnemyState.Dead) return;
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            stateTime += dt;
            if (attackCooldown > 0f) attackCooldown -= dt;

            if (knock.sqrMagnitude > 0.01f)
            {
                if (agent != null && agent.enabled && agent.isOnNavMesh) agent.Move(knock * dt);
                knock = Vector3.MoveTowards(knock, Vector3.zero, 20f * dt);
            }

            if (frozenUntilRelease)
            {
                StopAgent();
                UpdateAnim();
                return;
            }

            if (Actor.Hitstop > 0f)
            {
                StopAgent();
                return;
            }

            if (Actor.Stats.Stunned)
            {
                if (State != EnemyState.Stunned)
                {
                    CancelAttack();
                    SetState(EnemyState.Stunned);
                }
                StopAgent();
                UpdateAnim();
                return;
            }
            if (State == EnemyState.Stunned) SetState(EnemyState.Chase);

            if (Time.time >= nextThink)
            {
                nextThink = Time.time + Def.decisionInterval;
                target = FindTarget();
                Think();
            }

            switch (State)
            {
                case EnemyState.Spawning:
                    StopAgent();
                    if (stateTime >= 0.7f) SetState(EnemyState.Alert);
                    break;
                case EnemyState.Alert:
                    StopAgent();
                    if (stateTime >= 0.35f) SetState(EnemyState.Chase);
                    break;
                case EnemyState.React:
                    StopAgent();
                    if (stateTime >= 0.38f) SetState(EnemyState.Chase);
                    break;
                case EnemyState.Windup:
                case EnemyState.Attack:
                case EnemyState.Recover:
                    StopAgent();
                    UpdateAttack(dt);
                    break;
            }

            Tick(dt);
            if (agent != null && agent.enabled) agent.speed = Actor.Stats.MoveSpeed * speedFactor;
            UpdateFacing(dt);
            UpdateAnim();
            StuckCheck(dt);
        }

        protected abstract void Think();
        protected virtual void Tick(float dt) { }

        protected virtual Actor FindTarget()
        {
            var p = ctx.Player;
            if (p == null || p.IsDefeated || p.Actor.IsDead) return null;
            float d = Vector3.Distance(p.transform.position, transform.position);
            bool engaged = State != EnemyState.Idle && State != EnemyState.Spawning;
            float radius = engaged ? Def.leashRadius : Def.perceptionRadius;
            if (d > radius) return null;
            if (!engaged && d > 5f && !HasLineOfSight(p.Actor.Center)) return null;
            return p.Actor;
        }

        protected bool HasLineOfSight(Vector3 point)
        {
            return !Physics.Linecast(Actor.Center, point, Layers.EnvironmentMask, QueryTriggerInteraction.Ignore);
        }

        protected float FlatDistance(Vector3 p)
        {
            Vector3 d = p - transform.position;
            d.y = 0f;
            return d.magnitude;
        }

        protected void MoveTo(Vector3 p)
        {
            if (agent == null || !agent.enabled || !agent.isOnNavMesh) return;
            agent.isStopped = false;
            if ((agent.destination - p).sqrMagnitude > 0.2f || !agent.hasPath) agent.SetDestination(p);
        }

        protected void StopAgent()
        {
            if (agent == null || !agent.enabled || !agent.isOnNavMesh) return;
            if (!agent.isStopped) agent.isStopped = true;
            agent.velocity = Vector3.MoveTowards(agent.velocity, Vector3.zero, 30f * Time.deltaTime);
        }

        protected Vector3 RequestSlotPosition(bool ranged)
        {
            var p = ctx.Player;
            if (p == null || p.Slots == null) return target != null ? target.transform.position : transform.position;
            if (slot < 0 || slotRanged != ranged)
            {
                ReleaseSlot();
                slot = p.Slots.Request(this, ranged, transform.position);
                slotRanged = ranged;
            }
            if (slot < 0)
            {
                // Sem espaço livre: aguarda circulando a uma distância segura.
                Vector3 away = transform.position - p.transform.position;
                away.y = 0f;
                if (away.sqrMagnitude < 0.01f) away = Vector3.forward;
                return p.transform.position + away.normalized * (ranged ? 9f : 3.4f);
            }
            return p.Slots.SlotPosition(slot, ranged);
        }

        protected void ReleaseSlot()
        {
            if (slot >= 0 && ctx != null && ctx.Player != null && ctx.Player.Slots != null) ctx.Player.Slots.Release(this);
            slot = -1;
        }

        public void ApplyKnockback(Vector3 v)
        {
            float resist = Def != null && Def.isElite ? 0.35f : 1f;
            knock += v * resist;
            if (knock.magnitude > 10f) knock = knock.normalized * 10f;
        }

        // ------------------------------------------------------------ Ataques (preparação → janela → recuperação)

        protected void BeginAttack(AttackDefinition atk, float damage, Vector3 aimAt)
        {
            if (atk == null) return;
            currentAttack = atk;
            attackT = 0f;
            attackOpened = attackClosed = false;
            attackDamage = damage;
            attackEventId = ctx.Combat.NewEventId();
            Vector3 d = aimAt - transform.position;
            d.y = 0f;
            attackDir = d.sqrMagnitude > 1e-4f ? d.normalized : transform.forward;
            SetState(EnemyState.Windup);
            anim?.Play(atk.poseClip, 1f);
            Services.Audio?.Play(Def.alertSfx, transform.position, 0.6f);
            if (atk.telegraph)
            {
                Vector3 center = atk.areaAroundSelf ? transform.position : transform.position + attackDir * Mathf.Min(atk.range * 0.6f, 1.4f);
                float radius = atk.areaAroundSelf ? atk.areaRadius : atk.telegraphRadius;
                telegraph = ctx.Vfx.ShowTelegraph(center + Vector3.up * 0.04f, radius, atk.activeStart, new Color(1f, 0.28f, 0.2f, 0.85f), atk.areaAroundSelf, true);
            }
        }

        void UpdateAttack(float dt)
        {
            if (currentAttack == null) { if (State != EnemyState.Recover) SetState(EnemyState.Chase); return; }
            attackT += dt;
            if (!attackOpened)
            {
                // Continua girando para o alvo durante a preparação (esquivar exige sair do alcance, não só do ângulo).
                if (target != null && attackT < currentAttack.activeStart * 0.7f)
                {
                    Vector3 d = target.transform.position - transform.position;
                    d.y = 0f;
                    if (d.sqrMagnitude > 1e-4f) attackDir = Vector3.RotateTowards(attackDir, d.normalized, 3f * dt, 0f);
                }
                if (attackT >= currentAttack.activeStart)
                {
                    attackOpened = true;
                    SetStateKeepTime(EnemyState.Attack);
                    Services.Audio?.Play(currentAttack.swingSfx, transform.position);
                    OnAttackActive(currentAttack, attackDir);
                    if (hitbox != null) hitbox.Begin(currentAttack, attackDamage, attackEventId, attackDir, null, DamageFlags.None, DamageKind.Melee);
                    if (currentAttack.areaAroundSelf)
                    {
                        ctx.Vfx.Spawn("shockwave_ring", transform.position + Vector3.up * 0.05f, Quaternion.identity, 0f,
                            new VfxParams { radius = currentAttack.areaRadius, color = new Color(1f, 0.55f, 0.35f), hasColor = true });
                        ctx.CameraRig?.AddShake(0.15f);
                    }
                }
            }
            else if (!attackClosed && attackT >= currentAttack.activeEnd)
            {
                attackClosed = true;
                hitbox?.End();
                SetStateKeepTime(EnemyState.Recover);
            }
            else if (attackClosed && attackT >= currentAttack.duration)
            {
                currentAttack = null;
                attackCooldown = Def.attackCooldown * ctx.Random.Range(0.85f, 1.2f);
                SetState(EnemyState.Chase);
            }
        }

        protected virtual void OnAttackActive(AttackDefinition atk, Vector3 dir) { }

        void SetStateKeepTime(EnemyState s)
        {
            State = s;
        }

        protected void CancelAttack()
        {
            currentAttack = null;
            hitbox?.End();
            if (telegraph != null) telegraph.Hide();
            telegraph = null;
        }

        protected Vector3 AttackDirection => attackDir;

        // ------------------------------------------------------------ Reações e morte

        protected virtual void OnDamaged(Actor a, DamageResult r)
        {
            if (State == EnemyState.Dead) return;
            if (target == null && r.request.attacker != null) target = r.request.attacker;
            if (State == EnemyState.Idle || State == EnemyState.Spawning) SetState(EnemyState.Alert);
            Services.Audio?.Play(Def.hurtSfx, transform.position, 0.8f);
            bool canInterrupt = State == EnemyState.Windup || State == EnemyState.Chase || State == EnemyState.Position || State == EnemyState.Idle || State == EnemyState.Alert;
            if (r.request.stagger >= Def.poise && canInterrupt && (r.request.flags & DamageFlags.NoReaction) == 0)
            {
                CancelAttack();
                SetState(EnemyState.React);
                anim?.Play("flinch", 1f);
            }
        }

        protected virtual void OnDied(Actor a, DamageResult r)
        {
            CancelAttack();
            ReleaseSlot();
            SetState(EnemyState.Dead);
            if (agent != null) agent.enabled = false;
            foreach (var c in GetComponentsInChildren<Collider>()) c.enabled = false;
            anim?.Play("death", 1f, true);
            Services.Audio?.Play(Def.deathSfx, transform.position);
            if (Def.loot != null && !(OwnerEncounter != null && OwnerEncounter.SuppressLoot)) ctx.Loot.Roll(Def.loot, transform.position + Vector3.up * 0.5f, 1);
            StartCoroutine(DeathRoutine());
        }

        IEnumerator DeathRoutine()
        {
            yield return new WaitForSeconds(0.55f);
            ctx.Vfx.Spawn("death_cubes", transform.position + Vector3.up * 0.9f * Def.scale, Quaternion.identity, 0f,
                new VfxParams { color = Def.debrisColorA, hasColor = true, radius = Def.scale });
            ctx.Vfx.Spawn("death_puff", transform.position + Vector3.up * 0.6f, Quaternion.identity);
            var rig = GetComponentInChildren<CharacterRig>();
            rig?.SetVisible(false);
            yield return new WaitForSeconds(0.3f);
            Despawn();
        }

        public void Despawn()
        {
            if (despawning) return;
            despawning = true;
            CancelAttack();
            ReleaseSlot();
            Despawned?.Invoke(this);
            Destroy(gameObject);
        }

        // ------------------------------------------------------------ Movimento e apresentação

        void UpdateFacing(float dt)
        {
            Vector3 look;
            if (State == EnemyState.Windup || State == EnemyState.Attack || State == EnemyState.Recover) look = attackDir;
            else if (agent != null && agent.enabled && agent.velocity.sqrMagnitude > 0.05f) look = agent.velocity;
            else if (target != null && State != EnemyState.Idle) look = target.transform.position - transform.position;
            else return;
            look.y = 0f;
            if (look.sqrMagnitude < 1e-4f) return;
            transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(look.normalized), 540f * dt);
        }

        void UpdateAnim()
        {
            if (anim == null) return;
            Vector3 v = agent != null && agent.enabled ? agent.velocity : Vector3.zero;
            anim.SetLocomotion(v, Mathf.Max(0.5f, Def.moveSpeed));
        }

        void StuckCheck(float dt)
        {
            if (agent == null || !agent.enabled || !agent.isOnNavMesh) return;
            bool moving = (State == EnemyState.Chase || State == EnemyState.Position || State == EnemyState.Returning) && !agent.isStopped;
            if (!moving || agent.remainingDistance < 1.2f)
            {
                stuckTimer = 0f;
                stuckRef = transform.position;
                return;
            }
            stuckTimer += dt;
            if (stuckTimer < 1.4f) return;
            float moved = (transform.position - stuckRef).magnitude;
            stuckTimer = 0f;
            stuckRef = transform.position;
            if (moved > 0.25f) { hiddenStuck = 0f; return; }

            // Travado: tenta um desvio lateral; só reposiciona fora da visão da câmera.
            Vector2 r = ctx.Random.InsideUnitCircle() * 2.5f;
            Vector3 side = transform.position + new Vector3(r.x, 0f, r.y);
            if (NavMesh.SamplePosition(side, out var hit, 2f, NavMesh.AllAreas)) agent.SetDestination(hit.position);
            hiddenStuck += 1.4f;
            if (hiddenStuck > 5f && !VisibleToCamera() && target != null)
            {
                if (NavMesh.SamplePosition(RequestSlotPosition(slotRanged), out hit, 3f, NavMesh.AllAreas))
                    agent.Warp(hit.position);
                hiddenStuck = 0f;
            }
        }

        bool VisibleToCamera()
        {
            var cam = ctx.CameraRig != null ? ctx.CameraRig.cam : Camera.main;
            if (cam == null) return true;
            Vector3 v = cam.WorldToViewportPoint(transform.position);
            return v.z > 0f && v.x > -0.05f && v.x < 1.05f && v.y > -0.05f && v.y < 1.05f;
        }
    }
}

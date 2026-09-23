using System;
using UnityEngine;
using UnityEngine.AI;

namespace Ruinas
{
    /// <summary>
    /// Interpreta comandos (entrada ao vivo, roteiro de referência ou piloto automático) e os converte em
    /// intenções para o motor e para a máquina de ações. Resolve o contexto do clique (alvo, chão,
    /// interação) no perfil "clique para mover"; movimento direto cancela o caminho de forma previsível.
    /// </summary>
    [RequireComponent(typeof(PlayerMotor))]
    public class PlayerController : MonoBehaviour
    {
        public Actor Actor { get; private set; }
        public PlayerMotor Motor { get; private set; }
        public PlayerActionStateMachine Actions { get; private set; }
        public ArtifactSystem Artifacts { get; private set; }
        public EquipmentSystem Equipment { get; private set; }
        public HitboxController Hitbox { get; private set; }
        public AttackSlotManager Slots { get; private set; }
        public CharacterRig Rig { get; private set; }
        public ProceduralAnimator Anim { get; private set; }
        public PlayerDefinition Def { get; private set; }
        public InventorySystem Inventory { get; private set; }
        public ProgressionSystem Progression { get; private set; }
        public AttackDefinition[] FistCombo;

        public IPlayerCommandSource OverrideSource { get; set; }
        public PlayerCommands LastCommands => commands;
        /// <summary>Pedido de interação neste quadro (tecla de interagir ou botão de ataque contextual no controle).</summary>
        public bool InteractRequested { get; private set; }
        public Interactable NearestInteractable { get; private set; }
        public bool IsDefeated { get; private set; }
        public Camera ViewCamera => ctx != null && ctx.CameraRig != null ? ctx.CameraRig.cam : Camera.main;
        public Vector3 AimPoint { get; private set; }
        public Vector3 AimDirection { get; private set; }

        public event Action<PlayerController> Defeated;

        readonly LiveCommandSource live = new LiveCommandSource();
        PlayerCommands commands;
        LevelContext ctx;

        // Caminho do clique para mover
        NavMeshPath path;
        readonly Vector3[] corners = new Vector3[64];
        int cornerCount, cornerIndex;
        bool hasPath;
        Actor pathTarget;
        Interactable pathInteractable;
        float repathTimer;
        float clickRepeatTimer;

        public void Init(LevelContext context, PlayerDefinition def, InventorySystem inventory, ProgressionSystem progression, float maxHealthOverride = 0f)
        {
            ctx = context;
            Def = def;
            Inventory = inventory;
            Progression = progression;
            Actor = GetComponent<Actor>();
            Motor = GetComponent<PlayerMotor>();
            Actions = GetComponent<PlayerActionStateMachine>();
            Artifacts = GetComponent<ArtifactSystem>();
            Equipment = GetComponent<EquipmentSystem>();
            Hitbox = GetComponent<HitboxController>();
            Slots = GetComponent<AttackSlotManager>();
            Rig = GetComponentInChildren<CharacterRig>();
            Anim = GetComponentInChildren<ProceduralAnimator>();
            path = new NavMeshPath();

            Actor.team = Team.Player;
            Actor.displayName = "Heroína";
            Actor.Stats.baseMoveSpeed = def.moveSpeed;
            Actor.Stats.critChance = def.critChance;
            Actor.Stats.critMultiplier = def.critMultiplier;
            Actor.Stats.globalDamage = progression != null ? progression.DamageBonus : 1f;

            Motor.Init(def);
            Actions.Init(this, def);
            Artifacts.Init(this);
            Equipment.Init(this, inventory, maxHealthOverride);
            Actor.Receiver.Init(Equipment.ComputeMaxHealth());
            Actor.Died += OnDied;
            if (progression != null) progression.Changed += OnProgressionChanged;
        }

        void OnDestroy()
        {
            if (Actor != null) Actor.Died -= OnDied;
            if (Progression != null) Progression.Changed -= OnProgressionChanged;
        }

        void OnProgressionChanged()
        {
            Actor.Stats.globalDamage = Progression.DamageBonus * DemoDamageMultiplier;
            Equipment.Refresh();
        }

        /// <summary>Multiplicador da configuração de demonstração (1 na campanha).</summary>
        public float DemoDamageMultiplier { get; set; } = 1f;

        void OnDied(Actor a, DamageResult r)
        {
            if (IsDefeated) return;
            IsDefeated = true;
            ClearPath();
            Actions.SetDefeated();
            Artifacts.CancelAll();
            Motor.Stop();
            Services.Audio?.Play("player_death", transform.position);
            Defeated?.Invoke(this);
            ctx?.Events.RaisePlayerDefeated();
        }

        public void Revive(Vector3 position, Quaternion rotation, float healthFraction = 1f)
        {
            IsDefeated = false;
            ClearPath();
            Motor.Teleport(position, rotation);
            Actor.Status?.ClearAll();
            Actor.ClearHitstop();
            Actor.Receiver.Revive(healthFraction);
            Actions.ResetState(true);
            Artifacts.CancelAll();
            Artifacts.ResetCooldowns();
            ctx?.CameraRig?.Snap();
            ctx?.Events.RaisePlayerRespawned();
        }

        void Update()
        {
            if (Actor == null || ctx == null) return;
            commands = default;
            var src = OverrideSource != null && OverrideSource.Active ? OverrideSource : live;
            src.Fill(ref commands, this);

            if (IsDefeated || Actor.IsDead)
            {
                Motor.SetMoveIntent(Vector3.zero);
                return;
            }

            Handle(commands);
            ScanInteractables();
            Footsteps();
            if (Anim != null)
            {
                Anim.SetLocomotion(Motor.Velocity, Motor.MaxSpeed);
                // Altura do salto só no modelo: o colisor e a câmera não sobem.
                Anim.ExtraLift = Motor.VisualLift;
            }
        }

        public Vector3 ScreenToWorld(Vector2 v)
        {
            var cam = ViewCamera;
            Vector3 fwd = cam != null ? cam.transform.forward : Vector3.forward;
            Vector3 right = cam != null ? cam.transform.right : Vector3.right;
            fwd.y = 0f;
            right.y = 0f;
            fwd.Normalize();
            right.Normalize();
            Vector3 w = right * v.x + fwd * v.y;
            return w.sqrMagnitude > 1f ? w.normalized : w;
        }

        void Handle(in PlayerCommands c)
        {
            Vector3 moveWorld = c.moveIsWorld ? c.moveWorld : ScreenToWorld(c.move);
            moveWorld.y = 0f;
            bool clickProfile = !c.gamepad && Services.Settings != null &&
                                Services.Settings.Data.controlProfile == (int)ControlProfile.ClickToMove && OverrideSource == null;

            if (moveWorld.sqrMagnitude > 0.04f && hasPath) ClearPath();

            Vector3 aimDir = ComputeAimDirection(c, moveWorld);
            AimDirection = aimDir;

            if (c.dodgePressed) Actions.TryDodge(moveWorld.sqrMagnitude > 0.01f ? moveWorld : (hasPath ? PathDirection() : transform.forward));
            if (c.potionPressed) Actions.TryPotion();
            if (c.artifact1) Artifacts.TryUse(0, AimPoint, aimDir);
            if (c.artifact2) Artifacts.TryUse(1, AimPoint, aimDir);
            if (c.artifact3) Artifacts.TryUse(2, AimPoint, aimDir);
            // Interação contextual: no controle, o botão de ataque pega itens e abre baús quando há algo ao alcance.
            bool free = Actions.State == PlayerActionState.Free;
            bool contextual = c.gamepad && c.meleePressed && free &&
                              (NearestInteractable != null || Pickup.NearestInteractive(transform.position, Pickup.InteractRadius) != null);
            InteractRequested = c.interactPressed || contextual;
            if (InteractRequested && NearestInteractable != null && free)
                NearestInteractable.Interact(this);

            if (clickProfile) HandleClickProfile(c, aimDir);
            else if (!contextual && (c.meleePressed || c.meleeHeld)) Actions.RequestMelee(aimDir);

            if (c.rangedPressed || c.rangedHeld) Actions.RequestRanged(aimDir);

            Vector3 intent = Vector3.zero;
            if (Actions.AllowsMovement)
            {
                if (hasPath) intent = FollowPath();
                else intent = moveWorld;
            }
            Motor.SetMoveIntent(intent, 1f);
        }

        Vector3 ComputeAimDirection(in PlayerCommands c, Vector3 moveWorld)
        {
            Vector3 origin = transform.position;
            Vector3 dir;
            if (c.hasAimPoint)
            {
                AimPoint = c.aimPoint;
                dir = c.aimPoint - origin;
            }
            else if (c.aimStick.sqrMagnitude > 0.04f) dir = ScreenToWorld(c.aimStick);
            else if (moveWorld.sqrMagnitude > 0.01f) dir = moveWorld;
            else dir = transform.forward;
            dir.y = 0f;
            if (dir.sqrMagnitude < 1e-4f) dir = transform.forward;
            dir.Normalize();
            if (!c.hasAimPoint) AimPoint = origin + dir * 6f;

            // Assistência discreta: gira para um inimigo próximo dentro de um cone, sem perseguir.
            float range = CurrentMeleeRange() + 1.2f;
            var assist = FindAssistTarget(dir, range, c.gamepad ? 70f : 40f);
            if (c.hoverTarget != null) assist = c.hoverTarget;
            if (assist != null)
            {
                Vector3 to = assist.transform.position - origin;
                to.y = 0f;
                if (to.sqrMagnitude > 1e-4f && (c.hoverTarget != null || to.magnitude <= range)) dir = to.normalized;
            }
            return dir;
        }

        public float CurrentMeleeRange()
        {
            var w = Equipment != null ? Equipment.Melee : null;
            if (w != null && w.def.combo != null && w.def.combo.Length > 0 && w.def.combo[0] != null) return w.def.combo[0].range;
            return 1.6f;
        }

        Actor FindAssistTarget(Vector3 dir, float range, float cone)
        {
            Actor best = null;
            float bestScore = float.MaxValue;
            foreach (var a in ctx.Actors.All)
            {
                if (a == null || a.IsDead || !Actor.IsHostileTo(a)) continue;
                Vector3 to = a.transform.position - transform.position;
                if (Mathf.Abs(to.y) > 1.8f) continue;
                to.y = 0f;
                float d = to.magnitude - a.radius;
                if (d > range) continue;
                float ang = Vector3.Angle(dir, to);
                if (ang > cone * 0.5f && d > 0.8f) continue;
                if (Physics.Linecast(Actor.Center, a.Center, Layers.EnvironmentMask, QueryTriggerInteraction.Ignore)) continue;
                float score = d + ang * 0.03f;
                if (score < bestScore) { bestScore = score; best = a; }
            }
            return best;
        }

        // ------------------------------------------------------------ Clique para mover

        void HandleClickProfile(in PlayerCommands c, Vector3 aimDir)
        {
            clickRepeatTimer -= Time.deltaTime;
            bool click = c.clickPressed || (c.clickHeld && clickRepeatTimer <= 0f);
            if (click)
            {
                clickRepeatTimer = 0.2f;
                if (c.attackInPlace)
                {
                    ClearPath();
                    Actions.RequestMelee(aimDir);
                }
                else if (c.clickTarget != null && Actor.IsHostileTo(c.clickTarget))
                {
                    pathTarget = c.clickTarget;
                    pathInteractable = null;
                    if (DistanceTo(pathTarget) <= CurrentMeleeRange() + pathTarget.radius) { ClearPath(); Actions.RequestMelee(aimDir); }
                    else SetPath(pathTarget.transform.position);
                }
                else if (c.clickInteractable != null)
                {
                    pathInteractable = c.clickInteractable;
                    pathTarget = null;
                    SetPath(pathInteractable.transform.position);
                }
                else if (c.clickHasPoint)
                {
                    pathTarget = null;
                    pathInteractable = null;
                    SetPath(c.clickPoint);
                }
            }

            // Alvo em alcance durante o caminho: para e ataca.
            if (pathTarget != null)
            {
                if (pathTarget.IsDead) { pathTarget = null; ClearPath(); }
                else if (DistanceTo(pathTarget) <= CurrentMeleeRange() + pathTarget.radius)
                {
                    Vector3 to = pathTarget.transform.position - transform.position;
                    ClearPath();
                    Actions.RequestMelee(to);
                    if (!c.clickHeld) pathTarget = null;
                }
                else
                {
                    repathTimer -= Time.deltaTime;
                    if (repathTimer <= 0f) SetPath(pathTarget.transform.position);
                }
            }
            if (pathInteractable != null && Vector3.Distance(transform.position, pathInteractable.transform.position) <= pathInteractable.radius)
            {
                var it = pathInteractable;
                pathInteractable = null;
                ClearPath();
                if (it.CanInteract(this)) it.Interact(this);
            }
        }

        float DistanceTo(Actor a)
        {
            Vector3 d = a.transform.position - transform.position;
            d.y = 0f;
            return d.magnitude;
        }

        public bool SetPath(Vector3 destination)
        {
            repathTimer = 0.3f;
            if (!NavMesh.SamplePosition(destination, out var hit, 2.5f, NavMesh.AllAreas)) return false;
            if (!NavMesh.SamplePosition(transform.position, out var start, 1.5f, NavMesh.AllAreas)) return false;
            if (!NavMesh.CalculatePath(start.position, hit.position, NavMesh.AllAreas, path) || path.status == NavMeshPathStatus.PathInvalid) return false;
            cornerCount = path.GetCornersNonAlloc(corners);
            cornerIndex = cornerCount > 1 ? 1 : 0;
            hasPath = cornerCount > 0;
            return hasPath;
        }

        public void ClearPath()
        {
            hasPath = false;
            cornerCount = 0;
        }

        public bool HasPath => hasPath;

        Vector3 PathDirection()
        {
            if (!hasPath || cornerIndex >= cornerCount) return transform.forward;
            Vector3 d = corners[cornerIndex] - transform.position;
            d.y = 0f;
            return d.sqrMagnitude > 1e-4f ? d.normalized : transform.forward;
        }

        Vector3 FollowPath()
        {
            while (cornerIndex < cornerCount)
            {
                Vector3 d = corners[cornerIndex] - transform.position;
                d.y = 0f;
                bool last = cornerIndex == cornerCount - 1;
                if (d.magnitude > (last ? 0.2f : 0.45f)) return d.magnitude < 1f && last ? d : d.normalized;
                cornerIndex++;
            }
            ClearPath();
            return Vector3.zero;
        }

        float stepDistance;

        void Footsteps()
        {
            if (!Motor.Grounded || Motor.IsForced) return;
            float v = Motor.Velocity.magnitude;
            if (v < 0.5f) return;
            stepDistance += v * Time.deltaTime;
            if (stepDistance >= 0.78f)
            {
                stepDistance = 0f;
                Services.Audio?.Play("footstep", transform.position);
            }
        }

        // ------------------------------------------------------------ Interações

        void ScanInteractables()
        {
            var best = Interactable.FindNear(transform.position, 2.4f, this);
            if (best != NearestInteractable)
            {
                if (NearestInteractable != null) NearestInteractable.SetHighlighted(false);
                NearestInteractable = best;
                if (best != null) best.SetHighlighted(true);
            }
        }
    }
}

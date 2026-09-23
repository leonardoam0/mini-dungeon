using UnityEngine;
using UnityEngine.AI;

namespace Ruinas
{
    /// <summary>
    /// Acompanhante aliado (criatura de manta vermelha — PROPOSTA: função não confirmada no vídeo).
    /// Segue o jogador por caminhos válidos, mantém-se fora da rota dele, cospe em alvos com linha de visão
    /// e reaparece atrás do jogador (com efeito) quando fica para trás ou sem caminho.
    /// </summary>
    [RequireComponent(typeof(Actor))]
    public class CompanionBrain : MonoBehaviour
    {
        public ProjectileDefinition spit;
        public float spitDamage = 9f;
        public float spitCooldown = 1.6f;
        public float attackRange = 8f;

        Actor actor;
        NavMeshAgent agent;
        ProceduralAnimator anim;
        PlayerController owner;
        LevelContext ctx;
        float remaining;
        float damageScale = 1f;
        float cooldown;
        float thinkTimer;
        float lostTimer;
        Actor target;
        bool dismissed;
        bool persistent;

        public bool IsAlive => !dismissed && actor != null && (persistent || remaining > 0f);
        public float Remaining => remaining;

        public void Init(PlayerController player, float lifetime, float damageMultiplier, bool isPersistent = false)
        {
            ctx = LevelContext.Current;
            owner = player;
            remaining = lifetime;
            persistent = isPersistent;
            damageScale = damageMultiplier;
            actor = GetComponent<Actor>();
            agent = GetComponent<NavMeshAgent>();
            anim = GetComponentInChildren<ProceduralAnimator>();
            actor.team = Team.Player;
            actor.displayName = "Acompanhante";
            actor.Receiver.Init(100f);
            actor.Receiver.Invulnerable = true;
            if (agent != null)
            {
                agent.speed = 5.2f;
                agent.acceleration = 24f;
                agent.angularSpeed = 0f;
                agent.updateRotation = false;
                agent.stoppingDistance = 0.3f;
                agent.avoidancePriority = 90;
            }
        }

        public void Dismiss()
        {
            if (dismissed) return;
            dismissed = true;
            ctx?.Vfx.Spawn("summon_puff", transform.position, Quaternion.identity);
            Destroy(gameObject);
        }

        void Update()
        {
            if (dismissed || owner == null || ctx == null) return;
            float dt = Time.deltaTime;
            if (!persistent)
            {
                remaining -= dt;
                if (remaining <= 0f || owner.IsDefeated) { Dismiss(); return; }
            }
            if (cooldown > 0f) cooldown -= dt;
            thinkTimer -= dt;
            if (thinkTimer <= 0f)
            {
                thinkTimer = 0.3f;
                Think();
            }

            Vector3 look = target != null ? target.transform.position - transform.position : (agent != null ? agent.velocity : Vector3.zero);
            look.y = 0f;
            if (look.sqrMagnitude > 0.02f) transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(look), 480f * dt);
            anim?.SetLocomotion(agent != null && agent.enabled ? agent.velocity : Vector3.zero, 5.2f);
        }

        void Think()
        {
            if (agent == null || !agent.enabled || !agent.isOnNavMesh) return;
            Vector3 ownerPos = owner.transform.position;
            float toOwner = Vector3.Distance(transform.position, ownerPos);

            // Ficou para trás (ou sem caminho): reaparece de forma coerente atrás do jogador.
            bool pathBad = agent.pathStatus == NavMeshPathStatus.PathInvalid || agent.pathStatus == NavMeshPathStatus.PathPartial;
            lostTimer = toOwner > 15f || pathBad ? lostTimer + 0.3f : 0f;
            if (lostTimer > 1.5f)
            {
                lostTimer = 0f;
                Vector3 back = ownerPos - owner.transform.forward * 2f;
                if (NavMesh.SamplePosition(back, out var hit, 2.5f, NavMesh.AllAreas))
                {
                    ctx.Vfx.Spawn("summon_puff", transform.position, Quaternion.identity);
                    agent.Warp(hit.position);
                    ctx.Vfx.Spawn("summon_puff", hit.position, Quaternion.identity);
                }
                return;
            }

            target = ctx.Actors.NearestHostile(actor, transform.position, attackRange, a => !Physics.Linecast(actor.Center, a.Center, Layers.EnvironmentMask));
            if (target != null && cooldown <= 0f && spit != null)
            {
                agent.isStopped = true;
                cooldown = spitCooldown;
                Vector3 origin = transform.position + Vector3.up * 1.5f + transform.forward * 0.5f;
                ctx.Projectiles.Spawn(spit, actor, Team.Player, origin, target.Center - origin, spitDamage * damageScale);
                anim?.Play("spit", 1f);
                Services.Audio?.Play("llama_spit", transform.position);
                return;
            }

            // Posição de escolta: atrás e ao lado do jogador, fora da linha de movimento.
            Vector3 ownerVel = owner.Motor != null ? owner.Motor.Velocity : Vector3.zero;
            Vector3 fwd = ownerVel.sqrMagnitude > 0.5f ? ownerVel.normalized : owner.transform.forward;
            Vector3 side = Vector3.Cross(Vector3.up, fwd);
            Vector3 desired = ownerPos - fwd * 2.2f + side * 1.6f;
            if (toOwner > 3.6f || Vector3.Distance(transform.position, desired) > 2.5f)
            {
                if (NavMesh.SamplePosition(desired, out var h, 2f, NavMesh.AllAreas))
                {
                    agent.isStopped = false;
                    agent.SetDestination(h.position);
                }
            }
            else agent.isStopped = true;
        }
    }
}

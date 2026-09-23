using UnityEngine;

namespace Ruinas
{
    /// <summary>
    /// Ameaça de área verde: planta enraizada que emerge quando o jogador se aproxima, marca o chão com um
    /// anel e lança um esporo em arco que cria uma nuvem venenosa exatamente na área marcada.
    /// </summary>
    public class SporeVineBrain : EnemyBrain
    {
        const float FlightTime = 0.85f;
        const float TelegraphTime = 1.0f;

        bool emerged;
        float aimT = -1f;
        Vector3 aimPoint;
        Telegraph ring;

        public override void Init(EnemyDefinition def, LevelContext context, float healthMultiplier, float damageMultiplier, bool playSpawn)
        {
            base.Init(def, context, healthMultiplier, damageMultiplier, false);
            emerged = false;
            anim?.Play("burrowed", 1f, true);
        }

        protected override void Think()
        {
            if (State == EnemyState.Dead) return;
            if (!emerged)
            {
                if (target != null && FlatDistance(target.transform.position) < 10f)
                {
                    emerged = true;
                    anim?.Play("emerge", 1f);
                    Services.Audio?.Play("vine_emerge", transform.position);
                    ctx.Vfx.Spawn("dust_puff", transform.position, Quaternion.identity);
                    SetState(EnemyState.Alert);
                    attackCooldown = 1.2f;
                }
                return;
            }
            if (State == EnemyState.Chase || State == EnemyState.Idle)
            {
                if (target != null && attackCooldown <= 0f && FlatDistance(target.transform.position) <= 12f && HasLineOfSight(target.Center))
                {
                    SetState(EnemyState.Windup);
                    aimT = 0f;
                    aimPoint = target.transform.position;
                    var tm = target.GetComponent<PlayerMotor>();
                    if (tm != null) aimPoint += tm.Velocity * 0.35f;
                    if (ctx.Map != null)
                    {
                        // Mantém o alvo sobre o piso real.
                        if (Physics.Raycast(aimPoint + Vector3.up * 3f, Vector3.down, out var g, 8f, Layers.EnvironmentMask)) aimPoint = g.point;
                    }
                    float r = Def.projectile != null && Def.projectile.areaOnImpact != null ? Def.projectile.areaOnImpact.radius : 1.8f;
                    ring = ctx.Vfx.ShowTelegraph(aimPoint + Vector3.up * 0.04f, r, TelegraphTime + FlightTime, new Color(0.55f, 1f, 0.3f, 0.9f), true, true);
                    anim?.Play("spit_windup", 1f);
                    Services.Audio?.Play("vine_charge", transform.position);
                }
            }
        }

        protected override void Tick(float dt)
        {
            StopAgent();
            if (aimT < 0f || State != EnemyState.Windup) return;
            aimT += dt;
            Vector3 look = aimPoint - transform.position;
            look.y = 0f;
            if (look.sqrMagnitude > 1e-4f) transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(look), 360f * dt);
            if (aimT < TelegraphTime) return;
            aimT = -1f;
            Launch();
            attackCooldown = Def.attackCooldown * ctx.Random.Range(0.9f, 1.2f);
            SetState(EnemyState.Chase);
        }

        void Launch()
        {
            if (Def.projectile == null) return;
            Vector3 origin = transform.position + Vector3.up * 2.1f * Def.scale;
            Vector3 delta = aimPoint - origin;
            float g = Mathf.Max(0.1f, Def.projectile.gravity);
            Vector3 flat = new Vector3(delta.x, 0f, delta.z);
            Vector3 v = flat / FlightTime;
            v.y = (delta.y + 0.5f * g * FlightTime * FlightTime) / FlightTime;
            ctx.Projectiles.SpawnWithVelocity(Def.projectile, Actor, Team.Enemy, origin, v, Def.projectileDamage);
            anim?.Play("spit", 1f);
            Services.Audio?.Play("vine_spit", transform.position);
        }

        protected override void OnDied(Actor a, DamageResult r)
        {
            if (ring != null) ring.Hide();
            ring = null;
            base.OnDied(a, r);
        }

        protected override void OnDamaged(Actor a, DamageResult r)
        {
            if (!emerged)
            {
                emerged = true;
                anim?.Play("emerge", 1f);
            }
            // Enraizada: não é interrompida por golpes comuns, mas cancela a mira com golpes fortes.
            if (r.request.stagger >= Def.poise && State == EnemyState.Windup)
            {
                aimT = -1f;
                if (ring != null) ring.Hide();
                ring = null;
                SetState(EnemyState.React);
            }
            Services.Audio?.Play(Def.hurtSfx, transform.position, 0.8f);
        }
    }
}

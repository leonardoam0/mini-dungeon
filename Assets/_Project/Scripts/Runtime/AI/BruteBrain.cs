using UnityEngine;

namespace Ruinas
{
    /// <summary>
    /// Elite resistente: alterna um golpe amplo com um impacto no chão em área (anel de aviso longo).
    /// Só é interrompido por golpes fortes.
    /// </summary>
    public class BruteBrain : EnemyBrain
    {
        float slamCooldown = 3f;

        protected override void Think()
        {
            switch (State)
            {
                case EnemyState.Idle:
                    if (target != null) SetState(EnemyState.Alert);
                    break;
                case EnemyState.Chase:
                case EnemyState.Position:
                    if (target == null) { ReleaseSlot(); SetState(EnemyState.Returning); break; }
                    var swipe = Def.attacks != null && Def.attacks.Length > 0 ? Def.attacks[0] : null;
                    var slam = Def.attacks != null && Def.attacks.Length > 1 ? Def.attacks[1] : null;
                    float d = FlatDistance(target.transform.position);
                    if (attackCooldown <= 0f && HasLineOfSight(target.Center))
                    {
                        if (slam != null && slamCooldown <= 0f && d <= slam.areaRadius - 0.3f)
                        {
                            slamCooldown = 7f;
                            BeginAttack(slam, Def.attackDamage * slam.damageMultiplier, target.transform.position);
                            break;
                        }
                        if (swipe != null && d <= swipe.range + target.radius * 0.8f)
                        {
                            BeginAttack(swipe, Def.attackDamage, target.transform.position);
                            break;
                        }
                    }
                    MoveTo(d < 4f ? RequestSlotPosition(false) : target.transform.position);
                    break;
                case EnemyState.Returning:
                    if (target != null) { SetState(EnemyState.Chase); break; }
                    MoveTo(home);
                    if (FlatDistance(home) < 1f) SetState(EnemyState.Idle);
                    break;
            }
        }

        protected override void Tick(float dt)
        {
            if (slamCooldown > 0f) slamCooldown -= dt;
        }
    }
}

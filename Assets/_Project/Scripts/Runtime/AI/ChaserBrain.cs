using UnityEngine;

namespace Ruinas
{
    /// <summary>Perseguidor corpo a corpo: aproxima-se pelo espaço de ataque e golpeia com preparação legível.</summary>
    public class ChaserBrain : EnemyBrain
    {
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
                    var atk = Def.attacks != null && Def.attacks.Length > 0 ? Def.attacks[0] : null;
                    float range = atk != null ? atk.range : 1.5f;
                    float d = FlatDistance(target.transform.position);
                    if (atk != null && attackCooldown <= 0f && d <= range + target.radius * 0.8f && HasLineOfSight(target.Center))
                    {
                        BeginAttack(atk, Def.attackDamage, target.transform.position);
                        break;
                    }
                    MoveTo(d < 3.5f ? RequestSlotPosition(false) : target.transform.position);
                    break;
                case EnemyState.Returning:
                    if (target != null) { SetState(EnemyState.Chase); break; }
                    MoveTo(home);
                    if (FlatDistance(home) < 1f) SetState(EnemyState.Idle);
                    break;
            }
        }
    }
}

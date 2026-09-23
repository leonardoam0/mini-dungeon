using System;
using UnityEngine;

namespace Ruinas
{
    public interface IKnockbackTarget
    {
        void ApplyKnockback(Vector3 velocity);
    }

    /// <summary>
    /// Entidade de combate (jogador, inimigo, acompanhante). Agrega vida, status, atributos e animação;
    /// o comportamento fica em PlayerController / EnemyBrain / CompanionBrain.
    /// </summary>
    [RequireComponent(typeof(DamageReceiver))]
    public class Actor : MonoBehaviour
    {
        static int nextId = 1;

        public Team team = Team.Enemy;
        public string displayName = "";
        public float centerHeight = 1f;
        public float radius = 0.4f;
        public float height = 1.9f;

        public int Id { get; private set; }
        public DamageReceiver Receiver { get; private set; }
        public StatusEffectSystem Status { get; private set; }
        public ProceduralAnimator Anim { get; private set; }
        public ActorStats Stats { get; } = new ActorStats();
        public IKnockbackTarget Knockback { get; set; }
        /// <summary>Definição de inimigo quando aplicável (XP, loot, nome).</summary>
        public EnemyDefinition EnemyDef { get; set; }
        public bool IsDead => Receiver != null && Receiver.IsDead;
        public bool IsAlive => !IsDead && isActiveAndEnabled;
        public float Hitstop { get; private set; }
        public Vector3 Center => transform.position + Vector3.up * centerHeight;
        public Vector3 Forward => transform.forward;

        public event Action<Actor, DamageResult> Damaged;
        public event Action<Actor, DamageResult> Died;
        /// <summary>Disparado quando este ator causa dano (encantamentos "ao acertar").</summary>
        public event Action<Actor, DamageResult> DealtHit;

        void Awake()
        {
            Id = nextId++;
            Receiver = GetComponent<DamageReceiver>();
            Receiver.Bind(this);
            Status = GetComponent<StatusEffectSystem>();
            Anim = GetComponentInChildren<ProceduralAnimator>();
        }

        void OnEnable() => LevelContext.Current?.Actors.Register(this);
        void OnDisable() => LevelContext.Current?.Actors.Unregister(this);

        void Update()
        {
            if (Hitstop > 0f)
            {
                Hitstop -= Time.deltaTime;
                if (Anim != null) Anim.LocalTimeScale = Hitstop > 0f ? 0.05f : 1f;
            }
        }

        public bool IsHostileTo(Actor other)
        {
            if (other == null || other == this) return false;
            if (team == Team.Neutral || other.team == Team.Neutral) return false;
            return team != other.team;
        }

        public void AddHitstop(float seconds)
        {
            if (seconds <= 0f) return;
            Hitstop = Mathf.Max(Hitstop, seconds);
            if (Anim != null) Anim.LocalTimeScale = 0.05f;
        }

        public void ClearHitstop()
        {
            Hitstop = 0f;
            if (Anim != null) Anim.LocalTimeScale = 1f;
        }

        internal void NotifyDamaged(in DamageResult r) => Damaged?.Invoke(this, r);
        internal void NotifyDied(in DamageResult r) => Died?.Invoke(this, r);
        internal void NotifyDealtHit(in DamageResult r) => DealtHit?.Invoke(this, r);

        public float DistanceTo(Actor other)
        {
            Vector3 d = other.transform.position - transform.position;
            d.y = 0f;
            return d.magnitude;
        }
    }
}

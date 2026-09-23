using System;
using UnityEngine;

namespace Ruinas
{
    public struct ArtifactUse
    {
        public PlayerController player;
        public ArtifactDefinition def;
        public Vector3 aimPoint;
        public Vector3 aimDir;
        public float power;
    }

    public interface IArtifactBehaviour
    {
        bool IsActive { get; }
        void Activate(in ArtifactUse use);
        void Tick(float dt);
        void Cancel();
    }

    /// <summary>
    /// Três slots de artefato: condições de uso, recarga (escalada por atributos), comportamento,
    /// feedback de início/atividade/fim, atualização do HUD e limpeza em morte/reinício.
    /// </summary>
    public class ArtifactSystem : MonoBehaviour
    {
        public class Slot
        {
            public ItemInstance item;
            public ArtifactDefinition def;
            public float cooldown;
            public float cooldownTotal = 1f;
            public IArtifactBehaviour behaviour;
            /// <summary>Invocações: a recarga só começa quando a criatura some.</summary>
            public bool cooldownPending;
        }

        public readonly Slot[] Slots = { new Slot(), new Slot(), new Slot() };
        PlayerController pc;
        readonly int[] lastUseFrame = { -1, -1, -1 };

        public event Action Changed;
        public event Action<int> Used;
        public event Action<int, string> Denied;

        public void Init(PlayerController controller) => pc = controller;

        public void SetSlot(int index, ItemInstance item)
        {
            var s = Slots[index];
            var def = item?.def?.artifact;
            if (s.item == item && s.def == def) return;
            s.behaviour?.Cancel();
            s.item = item;
            s.def = def;
            s.behaviour = def != null ? CreateBehaviour(def.kind) : null;
            s.cooldown = 0f;
            Changed?.Invoke();
        }

        static IArtifactBehaviour CreateBehaviour(ArtifactKind kind)
        {
            switch (kind)
            {
                case ArtifactKind.FeatherLeap: return new FeatherLeapArtifact();
                case ArtifactKind.MagentaPulse: return new MagentaPulseArtifact();
                case ArtifactKind.SummonCompanion: return new SummonCompanionArtifact();
            }
            return null;
        }

        public float CooldownFraction(int i)
        {
            var s = Slots[i];
            return s.cooldown > 0f ? Mathf.Clamp01(s.cooldown / Mathf.Max(0.01f, s.cooldownTotal)) : 0f;
        }

        public bool IsActive(int i) => Slots[i].behaviour != null && Slots[i].behaviour.IsActive;

        public bool TryUse(int i, Vector3 aimPoint, Vector3 aimDir)
        {
            if (i < 0 || i >= Slots.Length || pc == null) return false;
            if (lastUseFrame[i] == Time.frameCount) return false;
            lastUseFrame[i] = Time.frameCount;
            var s = Slots[i];
            if (s.def == null || s.behaviour == null) { Deny(i, "Slot vazio"); return false; }
            if (s.cooldown > 0f) { Deny(i, "Recarregando"); return false; }
            if (pc.IsDefeated || !pc.Actions.CanUseArtifact()) { Deny(i, "Ocupado"); return false; }
            if (s.behaviour.IsActive && s.def.kind == ArtifactKind.SummonCompanion) { Deny(i, "Já ativo"); return false; }

            var use = new ArtifactUse
            {
                player = pc,
                def = s.def,
                aimPoint = aimPoint,
                aimDir = aimDir,
                power = s.item != null ? s.item.PowerMultiplier : 1f,
            };
            s.behaviour.Activate(use);
            s.cooldownTotal = s.def.cooldown * pc.Actor.Stats.CooldownMultiplier;
            if (s.def.kind == ArtifactKind.SummonCompanion && s.behaviour.IsActive)
            {
                s.cooldown = 0f;
                s.cooldownPending = true;
            }
            else s.cooldown = s.cooldownTotal;
            Used?.Invoke(i);
            Changed?.Invoke();
            return true;
        }

        void Deny(int i, string reason)
        {
            Denied?.Invoke(i, reason);
            Services.Audio?.PlayUi("denied");
        }

        void Update()
        {
            float dt = Time.deltaTime;
            foreach (var s in Slots)
            {
                if (s.cooldown > 0f) s.cooldown = Mathf.Max(0f, s.cooldown - dt);
                s.behaviour?.Tick(dt);
                if (s.cooldownPending && (s.behaviour == null || !s.behaviour.IsActive))
                {
                    s.cooldownPending = false;
                    s.cooldown = s.cooldownTotal;
                    Changed?.Invoke();
                }
            }
        }

        public void CancelAll()
        {
            foreach (var s in Slots) s.behaviour?.Cancel();
            Changed?.Invoke();
        }

        public void ResetCooldowns()
        {
            foreach (var s in Slots)
            {
                s.cooldown = 0f;
                s.cooldownPending = false;
            }
            Changed?.Invoke();
        }

        void OnDisable() => CancelAll();
    }
}

using UnityEngine;

namespace Ruinas
{
    /// <summary>
    /// Fonte de comandos a partir do roteiro (ou de uma gravação): alimenta o MESMO PlayerController usado
    /// na jogabilidade. Botões "pressionados" disparam uma vez no quadro em que o tempo cruza a chave.
    /// Com trajetória, o movimento segue a posição medida no vídeo (velocidade prevista + correção do erro),
    /// sempre pelo motor normal do jogador — sem teleporte.
    /// </summary>
    public class ReferenceReplay : IPlayerCommandSource
    {
        const float PositionGain = 8f;

        readonly RefInputKey[] keys;
        readonly RefPathKey[] path;
        readonly Transform anchor;
        readonly float lead;
        float time;
        float prevTime = -1f;
        bool running;

        public bool Active => running;
        public float Time => time;
        public float Duration { get; }
        public bool HasPath => path != null && path.Length > 1 && anchor != null;

        public ReferenceReplay(RefInputKey[] inputKeys, Transform arenaAnchor, float duration, RefPathKey[] pathKeys = null, float pathLead = 0.08f)
        {
            keys = inputKeys ?? new RefInputKey[0];
            path = pathKeys;
            anchor = arenaAnchor;
            Duration = duration;
            lead = pathLead;
        }

        public void Start()
        {
            time = 0f;
            prevTime = -1f;
            running = true;
        }

        public void Stop() => running = false;

        public void Advance(float dt)
        {
            if (!running) return;
            prevTime = time;
            time += dt;
            if (time > Duration) running = false;
        }

        /// <summary>Posição da trajetória (mundo) no instante t, interpolada linearmente.</summary>
        public Vector3 SamplePath(float t)
        {
            if (!HasPath) return Vector3.zero;
            if (t <= path[0].time) return anchor.TransformPoint(path[0].local);
            int last = path.Length - 1;
            if (t >= path[last].time) return anchor.TransformPoint(path[last].local);
            int lo = 0, hi = last;
            while (hi - lo > 1)
            {
                int mid = (lo + hi) >> 1;
                if (path[mid].time <= t) lo = mid; else hi = mid;
            }
            float u = Mathf.InverseLerp(path[lo].time, path[hi].time, t);
            return anchor.TransformPoint(Vector3.Lerp(path[lo].local, path[hi].local, u));
        }

        public void Fill(ref PlayerCommands c, PlayerController player)
        {
            if (!running) return;
            c.gamepad = true;

            if (HasPath && player != null)
            {
                float tt = time + lead;
                Vector3 target = SamplePath(tt);
                Vector3 vel = (SamplePath(tt + 0.05f) - SamplePath(tt - 0.05f)) / 0.1f;
                Vector3 err = target - player.transform.position;
                err.y = 0f;
                vel.y = 0f;
                Vector3 desired = vel + err * PositionGain;
                float maxSpeed = Mathf.Max(0.1f, player.Motor.MaxSpeed);
                Vector3 mv = desired / maxSpeed;
                if (mv.sqrMagnitude > 1f) mv.Normalize();
                if (vel.sqrMagnitude < 0.04f && err.sqrMagnitude < 0.01f) mv = Vector3.zero;
                c.moveIsWorld = true;
                c.moveWorld = mv;
            }

            if (keys.Length == 0) return;
            int idx = 0;
            for (int i = 0; i < keys.Length; i++)
            {
                if (keys[i].time <= time) idx = i;
                else break;
            }
            var k = keys[idx];
            if (!HasPath) c.move = k.move;
            if (k.hasAim && anchor != null)
            {
                c.hasAimPoint = true;
                c.aimPoint = anchor.TransformPoint(k.aimLocal);
            }
            RefButtons pressed = RefButtons.None;
            for (int i = 0; i < keys.Length; i++)
                if (keys[i].time > prevTime && keys[i].time <= time) pressed |= keys[i].pressed;
            RefButtons held = k.held;

            c.meleePressed = (pressed & RefButtons.Melee) != 0;
            c.meleeHeld = (held & RefButtons.Melee) != 0;
            c.rangedPressed = (pressed & RefButtons.Ranged) != 0;
            c.rangedHeld = (held & RefButtons.Ranged) != 0;
            c.dodgePressed = (pressed & RefButtons.Dodge) != 0;
            c.artifact1 = (pressed & RefButtons.Artifact1) != 0;
            c.artifact2 = (pressed & RefButtons.Artifact2) != 0;
            c.artifact3 = (pressed & RefButtons.Artifact3) != 0;
            c.potionPressed = (pressed & RefButtons.Potion) != 0;
            c.interactPressed = (pressed & RefButtons.Interact) != 0;
            c.attackInPlace = (held & RefButtons.AttackInPlace) != 0;
        }
    }
}

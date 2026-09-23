using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Ruinas
{
    /// <summary>
    /// Grava as entradas reais do jogador (em relação à âncora da arena) para reproduzir o trecho depois.
    /// O arquivo JSON pode substituir a trilha autoral do roteiro de referência.
    /// </summary>
    public class InputRecorder
    {
        [Serializable]
        class Recording
        {
            public float duration;
            public List<RefInputKey> keys = new List<RefInputKey>();
        }

        readonly Transform anchor;
        Recording rec;
        float t;
        RefButtons lastHeld;
        Vector2 lastMove;

        public bool Recording_ => rec != null;
        public float Elapsed => t;

        public InputRecorder(Transform arenaAnchor) => anchor = arenaAnchor;

        public void Begin()
        {
            rec = new Recording();
            t = 0f;
            lastHeld = RefButtons.None;
            lastMove = new Vector2(99f, 99f);
        }

        public void Sample(float dt, in PlayerCommands c)
        {
            if (rec == null) return;
            t += dt;
            RefButtons pressed = RefButtons.None, held = RefButtons.None;
            if (c.meleePressed) pressed |= RefButtons.Melee;
            if (c.rangedPressed) pressed |= RefButtons.Ranged;
            if (c.dodgePressed) pressed |= RefButtons.Dodge;
            if (c.artifact1) pressed |= RefButtons.Artifact1;
            if (c.artifact2) pressed |= RefButtons.Artifact2;
            if (c.artifact3) pressed |= RefButtons.Artifact3;
            if (c.potionPressed) pressed |= RefButtons.Potion;
            if (c.interactPressed) pressed |= RefButtons.Interact;
            if (c.meleeHeld) held |= RefButtons.Melee;
            if (c.rangedHeld) held |= RefButtons.Ranged;
            if (c.attackInPlace) held |= RefButtons.AttackInPlace;

            bool changed = pressed != RefButtons.None || held != lastHeld || (c.move - lastMove).sqrMagnitude > 0.01f;
            if (!changed) return;
            lastHeld = held;
            lastMove = c.move;
            rec.keys.Add(new RefInputKey
            {
                time = t,
                move = c.move,
                hasAim = c.hasAimPoint,
                aimLocal = c.hasAimPoint && anchor != null ? anchor.InverseTransformPoint(c.aimPoint) : Vector3.zero,
                pressed = pressed,
                held = held,
            });
        }

        public string End(string directory)
        {
            if (rec == null) return null;
            rec.duration = t;
            Directory.CreateDirectory(directory);
            string path = Path.Combine(directory, $"gravacao_{DateTime.Now:yyyyMMdd_HHmmss}.json");
            File.WriteAllText(path, JsonUtility.ToJson(rec, true));
            rec = null;
            return path;
        }

        public static RefInputKey[] Load(string path, out float duration)
        {
            duration = 0f;
            try
            {
                var r = JsonUtility.FromJson<Recording>(File.ReadAllText(path));
                duration = r.duration;
                return r.keys.ToArray();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Gravação] Não foi possível ler '{path}': {e.Message}");
                return null;
            }
        }
    }
}

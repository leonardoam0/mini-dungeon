using System;
using UnityEngine;

namespace Ruinas
{
    /// <summary>
    /// Nível e experiência do personagem, separados do poder dos itens. Cada nível dá +3% de dano e
    /// +4% de vida — progressão curta e legível para uma missão de 10–15 minutos.
    /// </summary>
    public class ProgressionSystem
    {
        public const int MaxLevel = 50;
        public int Level { get; private set; } = 1;
        public int Xp { get; private set; }
        public int Emeralds { get; private set; }

        /// <summary>Exibição fixa (modo de referência, ex.: nível 88). Não altera regras.</summary>
        public bool DisplayOverride;
        public int DisplayLevel;
        public float DisplayFraction;

        public event Action Changed;
        public event Action<int> LeveledUp;

        public static int XpForLevel(int level) => 60 + (Mathf.Max(1, level) - 1) * 45;

        public int ShownLevel => DisplayOverride ? DisplayLevel : Level;
        public float ShownFraction => DisplayOverride ? DisplayFraction : (Level >= MaxLevel ? 1f : Xp / (float)XpForLevel(Level));

        public float DamageBonus => 1f + 0.03f * (Level - 1);
        public float HealthBonus => 1f + 0.04f * (Level - 1);

        public void AddXp(int amount)
        {
            if (amount <= 0 || Level >= MaxLevel) return;
            Xp += amount;
            while (Level < MaxLevel && Xp >= XpForLevel(Level))
            {
                Xp -= XpForLevel(Level);
                Level++;
                LeveledUp?.Invoke(Level);
            }
            if (Level >= MaxLevel) Xp = 0;
            Changed?.Invoke();
        }

        public void AddEmeralds(int n)
        {
            if (n <= 0) return;
            Emeralds += n;
            Changed?.Invoke();
        }

        public bool SpendEmeralds(int n)
        {
            if (n < 0 || Emeralds < n) return false;
            Emeralds -= n;
            Changed?.Invoke();
            return true;
        }

        public ProgressData ToData() => new ProgressData { level = Level, xp = Xp, emeralds = Emeralds };

        public static ProgressionSystem FromData(ProgressData d)
        {
            var p = new ProgressionSystem();
            if (d != null)
            {
                p.Level = Mathf.Clamp(d.level, 1, MaxLevel);
                p.Xp = Mathf.Clamp(d.xp, 0, XpForLevel(p.Level) - 1);
                p.Emeralds = Mathf.Max(0, d.emeralds);
            }
            return p;
        }
    }
}

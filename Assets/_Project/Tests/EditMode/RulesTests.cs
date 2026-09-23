using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Ruinas.Tests
{
    /// <summary>Regras críticas sem cena: dano único por evento, fórmula, status, encontro, estados e projeção do mapa.</summary>
    public class RulesTests
    {
        [Test]
        public void HitRegistry_AplicaUmaVezPorEventoEAlvo()
        {
            var reg = new HitRegistry();
            Assert.IsTrue(reg.TryRegister(10, 1, 0f));
            Assert.IsFalse(reg.TryRegister(10, 1, 0.1f), "o mesmo alvo não pode ser atingido duas vezes pelo mesmo evento");
            Assert.IsTrue(reg.TryRegister(10, 2, 0.1f), "outro alvo no mesmo evento é válido");
            Assert.IsTrue(reg.TryRegister(11, 1, 0.2f), "novo evento no mesmo alvo é válido");
            Assert.IsTrue(reg.TryRegister(0, 1, 0.2f) && reg.TryRegister(0, 1, 0.2f), "evento 0 (status) não é deduplicado");
        }

        [Test]
        public void HitRegistry_ExpiraRegistrosAntigos()
        {
            var reg = new HitRegistry { Retention = 1f };
            reg.TryRegister(5, 1, 0f);
            reg.Prune(0.5f);
            Assert.AreEqual(1, reg.Count);
            reg.Prune(2f);
            Assert.AreEqual(0, reg.Count);
        }

        [Test]
        public void DamageMath_ArmaduraCriticoEArredondamento()
        {
            Assert.AreEqual(10f, DamageMath.Compute(10f, 1f, false, 2f, 1f, 0f, false), 1e-4);
            Assert.AreEqual(20f, DamageMath.Compute(10f, 1f, true, 2f, 1f, 0f, false), 1e-4);
            Assert.AreEqual(9f, DamageMath.Compute(10f, 1f, false, 2f, 1f, 0.1f, false), 1e-4);
            Assert.AreEqual(2f, DamageMath.Compute(10f, 1f, false, 2f, 1f, 5f, false), 1e-4, "armadura é limitada a 80%");
            Assert.AreEqual(10f, DamageMath.Compute(10f, 1f, false, 2f, 1f, 0.5f, true), 1e-4, "ignorar armadura");
            Assert.AreEqual(1, DamageMath.Round(0.2f), "dano positivo mínimo é 1");
            Assert.AreEqual(0, DamageMath.Round(0f));
        }

        static StatusEffectDefinition Def(StatusKind kind, StackPolicy policy, float tick = 0.5f, int maxStacks = 1)
        {
            var d = ScriptableObject.CreateInstance<StatusEffectDefinition>();
            d.id = kind.ToString();
            d.kind = kind;
            d.defaultDuration = 2f;
            d.tickInterval = tick;
            d.stackPolicy = policy;
            d.maxStacks = maxStacks;
            return d;
        }

        [Test]
        public void Status_PoliticasDeAcumuloETicks()
        {
            var col = new StatusEffectCollection();
            var poison = Def(StatusKind.Poison, StackPolicy.RefreshDuration);
            col.Apply(poison, null, 2f);
            int ticks = 0;
            col.Tick(1.0f, e => ticks++);
            Assert.AreEqual(2, ticks, "0,5 s por tick em 1 s");
            col.Apply(poison, null, 2f);
            Assert.AreEqual(2f, col.Find(poison).remaining, 1e-4, "renovação restaura a duração");
            col.Tick(2.1f, e => { });
            Assert.IsNull(col.Find(poison), "expira e é removido");

            var stack = Def(StatusKind.Burning, StackPolicy.AddStacks, 0.5f, 3);
            for (int i = 0; i < 5; i++) col.Apply(stack, null, 1f);
            Assert.AreEqual(3, col.Find(stack).stacks, "respeita o máximo de acúmulos");

            var ignore = Def(StatusKind.Stun, StackPolicy.IgnoreIfActive, 0f);
            col.Apply(ignore, null, 1f);
            col.Tick(0.5f, e => { });
            col.Apply(ignore, null, 5f);
            Assert.AreEqual(0.5f, col.Find(ignore).remaining, 1e-4, "ignorar enquanto ativo");
        }

        [Test]
        public void Status_AgregaModificadores()
        {
            var col = new StatusEffectCollection();
            var slow = Def(StatusKind.Slow, StackPolicy.RefreshDuration, 0f);
            slow.moveSpeedMultiplier = 0.5f;
            var vuln = Def(StatusKind.Vulnerable, StackPolicy.RefreshDuration, 0f);
            vuln.damageTakenMultiplier = 1.25f;
            var stun = Def(StatusKind.Stun, StackPolicy.KeepLongest, 0f);
            stun.stuns = true;
            col.Apply(slow, null, 2f);
            col.Apply(vuln, null, 2f);
            col.Apply(stun, null, 2f);
            var mods = new StatModifiers();
            col.Aggregate(mods);
            Assert.AreEqual(0.5f, mods.moveSpeed, 1e-4);
            Assert.AreEqual(1.25f, mods.damageTaken, 1e-4);
            Assert.IsTrue(mods.stunned);
            col.Clear();
            col.Aggregate(mods);
            Assert.AreEqual(1f, mods.moveSpeed, 1e-4);
            Assert.IsFalse(mods.stunned);
        }

        [Test]
        public void Encontro_TransicoesERecompensaUnica()
        {
            var sm = new EncounterStateMachine(2, 1f, 1f);
            int completed = 0, waves = 0;
            var states = new List<EncounterState>();
            sm.Completed += () => completed++;
            sm.WaveStarted += w => waves++;
            sm.Changed += (a, b) => states.Add(b);

            Assert.IsTrue(sm.Activate());
            Assert.IsFalse(sm.Activate(), "não reativa enquanto ativo");
            sm.Tick(1.1f, 0, 0.5f, 0);
            Assert.AreEqual(EncounterState.Active, sm.State);
            Assert.AreEqual(1, waves);
            sm.Tick(0.1f, 3, 0.5f, 0);
            Assert.AreEqual(0, sm.WaveIndex, "com inimigos vivos a onda não avança");
            sm.Tick(0.1f, 0, 0.5f, 0);
            sm.Tick(0.6f, 0, 0.5f, 0);
            Assert.AreEqual(1, sm.WaveIndex, "segunda onda após o atraso");
            sm.Tick(0.1f, 0, 0.5f, 0);
            Assert.AreEqual(EncounterState.Resolving, sm.State);
            sm.Tick(1.1f, 0, 0.5f, 0);
            Assert.AreEqual(EncounterState.Cleared, sm.State);
            for (int i = 0; i < 5; i++) sm.Tick(1f, 0, 0.5f, 0);
            sm.Reset();
            Assert.AreEqual(EncounterState.Cleared, sm.State, "encontro concluído não reinicia");
            Assert.AreEqual(1, completed, "recompensa concedida uma única vez");
            CollectionAssert.AreEqual(new[] { EncounterState.Activating, EncounterState.Active, EncounterState.Resolving, EncounterState.Cleared }, states);
        }

        [Test]
        public void Encontro_ReinicioVoltaAoInicio()
        {
            var sm = new EncounterStateMachine(3, 0.5f, 1f);
            sm.Activate();
            sm.Tick(0.6f, 0, 1f, 0);
            Assert.AreEqual(EncounterState.Active, sm.State);
            sm.Reset();
            Assert.AreEqual(EncounterState.Dormant, sm.State);
            Assert.AreEqual(-1, sm.WaveIndex);
            Assert.IsTrue(sm.Activate(), "pode ser ativado de novo após reinício");
        }

        [Test]
        public void Encontro_SaveRestauraSemRecompensa()
        {
            var sm = new EncounterStateMachine(1, 1f, 1f);
            int completed = 0;
            sm.Completed += () => completed++;
            sm.ForceCleared();
            sm.Tick(5f, 0, 0f, 0);
            Assert.AreEqual(EncounterState.Cleared, sm.State);
            Assert.AreEqual(0, completed);
            Assert.IsTrue(sm.RewardGranted);
        }

        [Test]
        public void EstadosDoJogo_TransicoesValidas()
        {
            Assert.IsTrue(GameStateMachine.CanTransition(GameState.Boot, GameState.Menu));
            Assert.IsTrue(GameStateMachine.CanTransition(GameState.Menu, GameState.Loading));
            Assert.IsTrue(GameStateMachine.CanTransition(GameState.Loading, GameState.Gameplay));
            Assert.IsTrue(GameStateMachine.CanTransition(GameState.Gameplay, GameState.Paused));
            Assert.IsTrue(GameStateMachine.CanTransition(GameState.Paused, GameState.Gameplay));
            Assert.IsTrue(GameStateMachine.CanTransition(GameState.Gameplay, GameState.Defeat));
            Assert.IsFalse(GameStateMachine.CanTransition(GameState.Menu, GameState.Gameplay), "menu passa por carregamento");
            Assert.IsFalse(GameStateMachine.CanTransition(GameState.Paused, GameState.Inventory));
            var sm = new GameStateMachine { DriveTimeScale = false };
            Assert.IsTrue(sm.TrySet(GameState.Loading));
            Assert.IsTrue(sm.TrySet(GameState.Gameplay));
            Assert.IsTrue(sm.AcceptsGameplayInput);
            Assert.IsTrue(sm.TrySet(GameState.Paused));
            Assert.IsTrue(sm.IsWorldPaused);
            Assert.IsFalse(sm.AcceptsGameplayInput, "pausa não aceita comandos do mundo");
        }

        [Test]
        public void MapaProjecao_IdaEVoltaEPivoFixo()
        {
            var pivot = new Vector3(10f, 5f, -3f);
            var w = new Vector3(14f, 5f, 2f);
            var m = MapProjection.WorldToMap(w, pivot, 0.38f);
            Assert.AreEqual(pivot, MapProjection.WorldToMap(pivot, pivot, 0.38f), "o jogador fica no centro do mapa");
            var back = MapProjection.MapToWorld(m, pivot, 0.38f);
            Assert.Less((back - w).magnitude, 1e-4f);
            Assert.AreEqual(0.38f * (w - pivot).magnitude, (m - pivot).magnitude, 1e-4, "escala uniforme (orientação preservada)");
        }

        [Test]
        public void Entrada_ZonaMortaRadialNormaliza()
        {
            Assert.AreEqual(Vector2.zero, InputRouter.ApplyDeadzone(new Vector2(0.1f, 0.05f), 0.2f));
            var diag = InputRouter.ApplyDeadzone(new Vector2(1f, 1f), 0.2f);
            Assert.LessOrEqual(diag.magnitude, 1.0001f, "diagonal não fica mais rápida");
            var half = InputRouter.ApplyDeadzone(new Vector2(0.6f, 0f), 0.2f);
            Assert.AreEqual(0.5f, half.x, 1e-4);
        }

        [Test]
        public void Captura_RecorteNoveDezesseis()
        {
            var p = ScriptableObject.CreateInstance<ReferenceCaptureProfile>();
            var r = p.CropRect(1920, 1080);
            Assert.AreEqual(1080, r.height);
            Assert.AreEqual(608, r.width, "540/960 × 1080 ≈ 607,5");
            Assert.AreEqual(656, r.x, "centralizado");
        }

        [Test]
        public void Progressao_NiveisEEsmeraldas()
        {
            var p = new ProgressionSystem();
            int ups = 0;
            p.LeveledUp += l => ups++;
            p.AddXp(ProgressionSystem.XpForLevel(1) + ProgressionSystem.XpForLevel(2) + 5);
            Assert.AreEqual(3, p.Level);
            Assert.AreEqual(2, ups);
            Assert.AreEqual(5, p.Xp);
            p.AddEmeralds(10);
            Assert.IsFalse(p.SpendEmeralds(11));
            Assert.IsTrue(p.SpendEmeralds(10));
            Assert.AreEqual(0, p.Emeralds);
        }
    }
}

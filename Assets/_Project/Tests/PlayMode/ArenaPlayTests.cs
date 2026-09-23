using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Ruinas.Tests
{
    /// <summary>
    /// Regras na cena real da arena, com os mesmos sistemas da jogabilidade: um dano por evento de golpe,
    /// bloqueio por parede, projéteis, recargas de poção e artefato e reinícios sem acúmulo de objetos.
    /// </summary>
    public class ArenaPlayTests
    {
        LevelContext ctx;
        readonly List<GameObject> temporaries = new List<GameObject>();

        [UnitySetUp]
        public IEnumerator Load()
        {
            var previous = LevelContext.Current;
            SceneManager.LoadScene(SceneFlow.ReferenceScene);
            float t = 0f;
            while (t < 30f && (LevelContext.Current == null || LevelContext.Current == previous || !LevelContext.Current.Initialized))
            {
                t += Time.unscaledDeltaTime;
                yield return null;
            }
            ctx = LevelContext.Current;
            Assert.IsNotNull(ctx, "cena da arena carregada");
            Assert.IsNotNull(ctx.Reference, "diretor de referência presente");
            Services.State.DebugFreeze = false;
            Services.State.ApplyTimeScale();
            Time.captureDeltaTime = 0f;
            // Controle livre: sem roteiro, arena no estado inicial conhecido.
            ctx.Reference.RestartArena(false);
            for (int i = 0; i < 3; i++) yield return null;
        }

        [TearDown]
        public void Cleanup()
        {
            foreach (var go in temporaries) if (go != null) Object.Destroy(go);
            temporaries.Clear();
        }

        static IEnumerator WaitSeconds(float seconds)
        {
            float t = 0f;
            while (t < seconds)
            {
                t += Time.deltaTime;
                yield return null;
            }
        }

        /// <summary>Mantém um único inimigo (congelado, sem status) e remove os demais e o companheiro.</summary>
        EnemyBrain Isolate()
        {
            EnemyBrain target = null;
            foreach (var a in new List<Actor>(ctx.Actors.All))
            {
                if (a == null) continue;
                var companion = a.GetComponent<CompanionBrain>();
                if (companion != null) { companion.Dismiss(); continue; }
                if (a.team != Team.Enemy || a.IsDead) continue;
                var brain = a.GetComponent<EnemyBrain>();
                if (brain == null) continue;
                if (target == null)
                {
                    target = brain;
                    brain.Freeze(true);
                    a.Status?.ClearAll();
                }
                else brain.Despawn();
            }
            Assert.IsNotNull(target, "há inimigos na arena");
            // Projéteis, áreas e avisos já em andamento de outros atores não entram na medição.
            ctx.ClearTransient();
            return target;
        }

        static void Place(EnemyBrain e, Vector3 pos)
        {
            e.Freeze(true);
            var agent = e.GetComponent<NavMeshAgent>();
            if (agent != null && agent.enabled && agent.isOnNavMesh) agent.Warp(pos);
            else e.transform.position = pos;
            Physics.SyncTransforms();
        }

        /// <summary>Direção a partir do jogador com chão navegável no mesmo nível e sem cenário no caminho.</summary>
        Vector3 ClearDirection(float distance)
        {
            var p = ctx.Player;
            Vector3 origin = p.transform.position;
            Vector3 c = p.Actor.Center;
            for (int i = 0; i < 16; i++)
            {
                Vector3 d = Quaternion.Euler(0f, i * 22.5f, 0f) * Vector3.forward;
                if (Physics.Linecast(c, c + d * (distance + 0.8f), Layers.EnvironmentMask, QueryTriggerInteraction.Ignore)) continue;
                bool floorOk = true;
                for (float s = 0.5f; s <= distance + 0.01f; s += 0.5f)
                {
                    if (!NavMesh.SamplePosition(origin + d * s, out var hit, 0.3f, NavMesh.AllAreas) || Mathf.Abs(hit.position.y - origin.y) > 0.2f)
                    {
                        floorOk = false;
                        break;
                    }
                }
                if (floorOk) return d;
            }
            Assert.Fail("nenhuma direção livre ao redor do jogador");
            return Vector3.forward;
        }

        static AttackDefinition TestAttack(float range)
        {
            var a = ScriptableObject.CreateInstance<AttackDefinition>();
            a.id = "teste";
            a.activeStart = 0f;
            a.activeEnd = 0.25f;
            a.range = range;
            a.sweepFrom = 60f;
            a.sweepTo = -60f;
            a.height = 1f;
            a.knockback = 0f;
            a.stagger = 0f;
            a.hitstop = 0f;
            return a;
        }

        static bool IsMelee(in DamageResult r) => r.request.sourceTag != null && r.request.sourceTag.StartsWith("melee:");

        [UnityTest]
        public IEnumerator Golpe_AplicaDanoUmaVezPorEvento()
        {
            var player = ctx.Player;
            var target = Isolate();
            Vector3 dir = ClearDirection(1.4f);
            Place(target, player.transform.position + dir * 1.4f);
            yield return null;
            var hits = new List<DamageResult>();
            ctx.Events.Damaged += r => { if (r.Target == target.Actor && r.request.attacker == player.Actor && IsMelee(r)) hits.Add(r); };

            Assert.IsTrue(player.Actions.RequestMelee(dir), "golpe iniciado pela máquina de estados do jogador");
            yield return WaitSeconds(1.2f);

            Assert.AreEqual(1, hits.Count, "um golpe = um evento = um dano no alvo, mesmo com a varredura ativa por vários quadros");
            Assert.Greater(hits[0].amount, 0);
        }

        [UnityTest]
        public IEnumerator Golpe_NaoAtravessaParede()
        {
            var player = ctx.Player;
            var target = Isolate();
            Vector3 dir = ClearDirection(2.2f);
            Place(target, player.transform.position + dir * 2.2f);

            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            temporaries.Add(wall);
            wall.name = "ParedeDeTeste";
            wall.layer = Layers.Environment;
            wall.transform.SetPositionAndRotation(player.transform.position + dir * 1.1f + Vector3.up * 1.2f, Quaternion.LookRotation(dir));
            wall.transform.localScale = new Vector3(3f, 2.4f, 0.2f);
            Physics.SyncTransforms();
            yield return null;

            int hits = 0;
            ctx.Events.Damaged += r => { if (r.Target == target.Actor && r.request.attacker == player.Actor && IsMelee(r)) hits++; };
            var attack = TestAttack(3f);

            player.Hitbox.Begin(attack, 5f, ctx.Combat.NewEventId(), dir);
            yield return WaitSeconds(0.4f);
            player.Hitbox.End();
            Assert.AreEqual(0, hits, "com a parede entre atacante e alvo, o golpe não acerta");

            Object.Destroy(wall);
            yield return null;
            Physics.SyncTransforms();
            player.Hitbox.Begin(attack, 5f, ctx.Combat.NewEventId(), dir);
            yield return WaitSeconds(0.4f);
            player.Hitbox.End();
            Assert.AreEqual(1, hits, "sem a parede, o mesmo golpe na mesma posição acerta");
            Object.DestroyImmediate(attack);
        }

        [UnityTest]
        public IEnumerator Projetil_AtingeUmaVezPorEventoEVoltaAoPool()
        {
            var player = ctx.Player;
            var target = Isolate();
            Vector3 dir = ClearDirection(5f);
            Place(target, player.transform.position + dir * 5f);
            yield return null;

            int baseline = ctx.Projectiles.ActiveCount;
            int arrows = player.Inventory.Arrows;
            var perEvent = new Dictionary<int, int>();
            ctx.Events.Damaged += r =>
            {
                if (r.Target != target.Actor || r.request.attacker != player.Actor || r.request.kind != DamageKind.Ranged) return;
                if ((r.request.flags & DamageFlags.FromStatus) != 0) return;
                perEvent.TryGetValue(r.request.eventId, out int n);
                perEvent[r.request.eventId] = n + 1;
            };

            Assert.IsTrue(player.Actions.RequestRanged(dir), "disparo iniciado");
            yield return WaitSeconds(1.5f);
            float t = 0f;
            while (ctx.Projectiles.ActiveCount > baseline && t < 4f) { t += Time.deltaTime; yield return null; }

            Assert.AreEqual(arrows - 1, player.Inventory.Arrows, "um disparo consome uma flecha");
            Assert.IsNotEmpty(perEvent, "o projétil atingiu o alvo");
            foreach (var kv in perEvent) Assert.AreEqual(1, kv.Value, $"evento {kv.Key}: no máximo um dano por projétil e alvo");
            Assert.AreEqual(baseline, ctx.Projectiles.ActiveCount, "projéteis devolvidos ao pool");
        }

        [UnityTest]
        public IEnumerator Pocao_RespeitaRecargaEEntradaDuplicada()
        {
            var player = ctx.Player;
            var recv = player.Actor.Receiver;
            ctx.Combat.Apply(new DamageRequest { target = recv, amount = recv.MaxHealth * 0.5f, kind = DamageKind.Pure, flags = DamageFlags.NoReaction | DamageFlags.IgnoreArmor });
            yield return null;
            float hp = recv.Health;
            Assert.Less(hp, recv.MaxHealth);

            Assert.IsTrue(player.Actions.TryPotion(), "primeiro uso cura");
            Assert.IsFalse(player.Actions.TryPotion(), "segunda entrada no mesmo quadro é ignorada");
            Assert.Greater(recv.Health, hp);
            Assert.Greater(player.Actions.PotionCooldown, 0f, "recarga iniciada");

            ctx.Combat.Apply(new DamageRequest { target = recv, amount = 20f, kind = DamageKind.Pure, flags = DamageFlags.NoReaction | DamageFlags.IgnoreArmor });
            yield return null;
            Assert.IsFalse(player.Actions.TryPotion(), "em recarga, a poção é recusada");
        }

        [UnityTest]
        public IEnumerator Artefato_RespeitaRecarga()
        {
            var player = ctx.Player;
            player.Artifacts.ResetCooldowns();
            yield return null;
            Vector3 aim = player.transform.position + player.transform.forward * 3f;
            Assert.IsTrue(player.Artifacts.TryUse(0, aim, player.transform.forward), "artefato do slot 1 ativado");
            Assert.IsFalse(player.Artifacts.TryUse(0, aim, player.transform.forward), "entrada duplicada no mesmo quadro é ignorada");
            yield return WaitSeconds(0.5f);
            Assert.IsFalse(player.Artifacts.TryUse(0, aim, player.transform.forward), "em recarga, o artefato é recusado");
            Assert.Greater(player.Artifacts.CooldownFraction(0), 0f, "HUD recebe a fração de recarga");
        }

        [UnityTest]
        public IEnumerator Reinicio_NaoAcumulaObjetos()
        {
            ctx.Reference.RestartArena(false);
            yield return WaitSeconds(0.5f);
            int actors = ctx.Actors.All.Count;
            int enemies = ctx.Actors.CountAlive(Team.Enemy);
            int roots = SceneManager.GetActiveScene().rootCount;

            for (int n = 0; n < 6; n++)
            {
                // Deixa efeitos pendentes antes de cada reinício.
                ctx.Areas.Spawn(ctx.Player.Actor, Team.Player, ctx.Player.transform.position,
                    new AreaEffectSpec { enabled = true, radius = 2f, duration = 10f, tickInterval = 0.5f, damagePerTick = 1f, kind = DamageKind.Poison, visualKey = "cloud_green" });
                ctx.Projectiles.Spawn(Services.Database.projectiles[0], ctx.Player.Actor, Team.Player, ctx.Player.Actor.Center, ctx.Player.transform.forward, 1f);
                ctx.Reference.RestartArena(n % 2 == 0);
                // No mesmo quadro do reinício nada do estado anterior pode sobrar.
                Assert.AreEqual(0, ctx.Areas.ActiveCount, "áreas de efeito limpas no reinício");
                Assert.AreEqual(0, ctx.Projectiles.ActiveCount, "projéteis limpos no reinício");
                Assert.AreEqual(0, ctx.Vfx.ActiveTelegraphs, "avisos de ataque limpos no reinício");
                yield return WaitSeconds(0.3f);
            }
            ctx.Reference.RestartArena(false);
            yield return WaitSeconds(0.5f);

            Assert.AreEqual(enemies, ctx.Actors.CountAlive(Team.Enemy), "mesmo número de inimigos após reinícios");
            Assert.AreEqual(actors, ctx.Actors.All.Count, "atores não acumulam");
            Assert.AreEqual(roots, SceneManager.GetActiveScene().rootCount, "nenhum objeto raiz novo na cena");
        }
    }
}

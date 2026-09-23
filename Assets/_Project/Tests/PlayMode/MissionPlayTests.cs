using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Ruinas.Tests
{
    /// <summary>
    /// Percurso real: derrota leva de volta ao ponto de retorno com uma vida a menos, vida cheia e save
    /// atualizado. O save do teste fica em uma pasta temporária (o save do jogador não é tocado).
    /// </summary>
    public class MissionPlayTests
    {
        LevelContext ctx;
        SaveService originalSave;
        string tempDir;

        [UnitySetUp]
        public IEnumerator Load()
        {
            originalSave = Services.Save;
            tempDir = Path.Combine(Application.temporaryCachePath, "RuinasTeste_" + System.Guid.NewGuid().ToString("N"));
            Services.Save = new SaveService(tempDir);

            var previous = LevelContext.Current;
            SceneManager.LoadScene(SceneFlow.MissionScene);
            float t = 0f;
            while (t < 60f && (LevelContext.Current == null || LevelContext.Current == previous || !LevelContext.Current.Initialized))
            {
                t += Time.unscaledDeltaTime;
                yield return null;
            }
            ctx = LevelContext.Current;
            Assert.IsNotNull(ctx, "cena da missão carregada");
            Assert.IsNotNull(ctx.Mission, "diretor da missão presente");
            Services.State.DebugFreeze = false;
            Services.State.ApplyTimeScale();
            for (int i = 0; i < 3; i++) yield return null;
        }

        [TearDown]
        public void Restore()
        {
            if (originalSave != null) Services.Save = originalSave;
            try { if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true); } catch { }
        }

        [UnityTest]
        public IEnumerator Derrota_VoltaAoPontoDeRetornoComUmaVidaAMenos()
        {
            var mission = ctx.Mission;
            var player = ctx.Player;
            int lives = mission.Lives;
            Assert.Greater(lives, 0);
            var cp = Checkpoint.Find(mission.CheckpointId);
            Assert.IsNotNull(cp, "ponto de retorno inicial existe");

            var recv = player.Actor.Receiver;
            ctx.Combat.Apply(new DamageRequest { target = recv, amount = recv.MaxHealth * 10f, kind = DamageKind.Pure, flags = DamageFlags.NoReaction | DamageFlags.IgnoreArmor });
            yield return null;
            Assert.IsTrue(player.IsDefeated, "jogador derrotado");

            float t = 0f;
            while (t < 8f && (player.IsDefeated || mission.Lives == lives)) { t += Time.unscaledDeltaTime; yield return null; }
            yield return new WaitForSecondsRealtime(0.8f);

            Assert.IsFalse(player.IsDefeated, "jogador de volta ao percurso");
            Assert.AreEqual(lives - 1, mission.Lives, "uma vida consumida");
            Assert.AreEqual(recv.MaxHealth, recv.Health, 0.01f, "vida cheia ao voltar");
            Vector3 flat = player.transform.position - cp.SpawnPosition;
            flat.y = 0f;
            Assert.Less(flat.magnitude, 0.8f, "reposicionado no ponto de retorno");

            var saved = Services.Save.Load(Services.Database);
            Assert.IsNotNull(saved, "save gravado");
            Assert.AreEqual(lives - 1, saved.mission.lives, "save reflete as vidas restantes");
            Assert.AreEqual(mission.CheckpointId, saved.mission.checkpointId);
        }

        [UnityTest]
        public IEnumerator Equipar_AlteraAtributosDeVerdade()
        {
            yield return null;
            var player = ctx.Player;
            var inv = player.Inventory;
            var db = Services.Database;
            var stats = player.Actor.Stats;

            // Sem armadura: linha de base.
            inv.Unequip(ItemKind.Armor);
            yield return null;
            float armor0 = stats.ArmorReduction, hp0 = player.Actor.Receiver.MaxHealth, speed0 = stats.MoveSpeed;

            foreach (var id in new[] { "armadura_clara", "manto_musgo" })
            {
                var def = db.GetItem(id);
                Assert.IsNotNull(def, id);
                var it = inv.Add(def, 1);
                Assert.IsTrue(inv.Equip(it), id + " equipada");
                yield return null;
                float guard = 0f, vitality = 0f, swift = 1f;
                if (def.enchantments != null)
                    foreach (var e in def.enchantments)
                    {
                        if (e == null) continue;
                        if (e.kind == EnchantKind.Guard) guard += e.value;
                        if (e.kind == EnchantKind.Vitality) vitality += e.value;
                        if (e.kind == EnchantKind.Swiftness) swift *= 1f + e.value;
                    }
                Assert.AreEqual(Mathf.Clamp(armor0 + def.damageReduction + guard, 0f, 0.8f), stats.ArmorReduction, 1e-3f, id + ": redução de dano");
                Assert.AreEqual(hp0 + def.healthBonus * it.PowerMultiplier + vitality, player.Actor.Receiver.MaxHealth, 0.51f, id + ": vida máxima");
                Assert.AreEqual(speed0 * (1f + def.moveSpeedBonus) * swift, stats.MoveSpeed, 1e-3f, id + ": velocidade");
            }

            // Arma com encantamento de afiação multiplica o dano corpo a corpo.
            ItemDefinition sharp = null;
            foreach (var d in db.items)
            {
                if (d == null || d.kind != ItemKind.Melee || d.enchantments == null) continue;
                foreach (var e in d.enchantments) if (e != null && e.kind == EnchantKind.Sharpness) sharp = d;
            }
            if (sharp != null)
            {
                inv.Equip(inv.Add(sharp, 1));
                yield return null;
                float k = 1f;
                foreach (var e in sharp.enchantments) if (e != null && e.kind == EnchantKind.Sharpness) k *= 1f + e.value;
                Assert.AreEqual(k, stats.equipment.meleeDamage, 1e-3f, sharp.id + ": multiplicador corpo a corpo");
            }
        }

        [UnityTest]
        public IEnumerator Save_CapturaEReleituraSaoEquivalentes()
        {
            yield return null;
            var mission = ctx.Mission;
            string Strip(string s) => System.Text.RegularExpressions.Regex.Replace(s, "\"savedAtUtc\":\"[^\"]*\"", "");
            string before = JsonUtility.ToJson(mission.CaptureSave());
            Assert.IsTrue(mission.WriteSave());
            var reloaded = Services.Save.Load(Services.Database);
            Assert.IsNotNull(reloaded);
            Assert.AreEqual(SaveLoadStatus.Ok, Services.Save.LastStatus);
            Assert.AreEqual(Strip(before), Strip(JsonUtility.ToJson(reloaded)));
        }
    }
}

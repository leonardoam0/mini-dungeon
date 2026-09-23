using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace Ruinas.Tests
{
    /// <summary>Integridade do save: ida e volta, recuperação pela cópia, migração de versão e validação.</summary>
    public class SaveTests
    {
        string dir;
        GameDatabase db;
        ItemDefinition sword, feather;

        [SetUp]
        public void SetUp()
        {
            dir = Path.Combine(Path.GetTempPath(), "RuinasSaveTest_" + System.Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            sword = ScriptableObject.CreateInstance<ItemDefinition>();
            sword.id = "espada_teste";
            sword.kind = ItemKind.Melee;
            feather = ScriptableObject.CreateInstance<ItemDefinition>();
            feather.id = "pena_teste";
            feather.kind = ItemKind.Artifact;
            db = ScriptableObject.CreateInstance<GameDatabase>();
            db.items = new[] { sword, feather };
            db.BuildLookups();
        }

        [TearDown]
        public void TearDown()
        {
            try { Directory.Delete(dir, true); } catch { }
        }

        SaveData Sample()
        {
            var inv = new InventorySystem();
            var s = inv.Add(sword, 3);
            var f = inv.Add(feather);
            inv.Equip(s);
            inv.Equip(f, 1);
            inv.SetArrows(17);
            var d = new SaveData
            {
                progress = new ProgressData { level = 4, xp = 20, emeralds = 33 },
                inventory = inv.ToData(),
            };
            d.mission.checkpointId = "cp_sala";
            d.mission.lives = 2;
            d.mission.clearedEncounters.Add("enc_corredor");
            d.mission.openedChests.Add("bau_sala");
            d.mission.inProgress = true;
            return d;
        }

        [Test]
        public void Save_IdaEVolta()
        {
            var svc = new SaveService(dir);
            Assert.IsTrue(svc.Write(Sample()));
            var loaded = svc.Load(db);
            Assert.IsNotNull(loaded);
            Assert.AreEqual(SaveLoadStatus.Ok, svc.LastStatus);
            Assert.AreEqual(4, loaded.progress.level);
            Assert.AreEqual(33, loaded.progress.emeralds);
            Assert.AreEqual("cp_sala", loaded.mission.checkpointId);
            Assert.AreEqual(2, loaded.mission.lives);
            CollectionAssert.Contains(loaded.mission.clearedEncounters, "enc_corredor");
            var inv = InventorySystem.FromData(loaded.inventory, db);
            Assert.AreEqual(2, inv.Items.Count);
            Assert.IsNotNull(inv.Melee);
            Assert.AreEqual(3, inv.Melee.power);
            Assert.AreEqual(feather, inv.Artifacts[1].def);
            Assert.AreEqual(17, inv.Arrows);
        }

        [Test]
        public void Save_RecuperaDaCopiaQuandoPrincipalCorrompe()
        {
            var svc = new SaveService(dir);
            svc.Write(Sample());
            svc.Write(Sample()); // cria a cópia .bak na troca atômica
            File.WriteAllText(svc.MainPath, "{ corrompido ");
            var loaded = svc.Load(db);
            Assert.IsNotNull(loaded, "deve recuperar da cópia");
            Assert.AreEqual(SaveLoadStatus.RecoveredFromBackup, svc.LastStatus);
        }

        [Test]
        public void Save_AusenteOuIlegivelNaoQuebra()
        {
            var svc = new SaveService(dir);
            Assert.IsNull(svc.Load(db));
            Assert.AreEqual(SaveLoadStatus.NoSave, svc.LastStatus);
            File.WriteAllText(svc.MainPath, "lixo");
            Assert.IsNull(svc.Load(db));
            Assert.AreEqual(SaveLoadStatus.Corrupt, svc.LastStatus);
        }

        [Test]
        public void Save_VersaoFuturaERecusada()
        {
            var svc = new SaveService(dir);
            var d = Sample();
            string json = JsonUtility.ToJson(d).Replace($"\"version\":{SaveData.CurrentVersion}", "\"version\":99");
            File.WriteAllText(svc.MainPath, json);
            Assert.IsNull(svc.Load(db));
        }

        [Test]
        public void Save_MigraVersao1()
        {
            // v1: sem vidas, estatísticas e objetivos.
            string v1 = "{\"version\":1,\"progress\":{\"level\":2,\"xp\":5,\"emeralds\":1},\"inventory\":{\"items\":[],\"melee\":\"\",\"ranged\":\"\",\"armor\":\"\",\"artifacts\":[\"\",\"\",\"\"],\"arrows\":5},\"mission\":{\"inProgress\":true,\"checkpointId\":\"cp_sala\",\"lives\":0}}";
            var d = SaveService.TryParse(v1, db, out var status, out _);
            Assert.IsNotNull(d);
            Assert.AreEqual(SaveLoadStatus.Migrated, status);
            Assert.AreEqual(SaveData.CurrentVersion, d.version);
            Assert.AreEqual(3, d.mission.lives);
            Assert.IsNotNull(d.stats);
        }

        [Test]
        public void Save_ValidaItensDesconhecidosESlots()
        {
            var d = Sample();
            d.inventory.items.Add(new ItemInstanceData { uid = "x1", itemId = "item_que_nao_existe", power = 1 });
            d.inventory.ranged = "uid_inexistente";
            d.progress.level = -3;
            bool repaired = SaveService.Validate(d, db, out var msg);
            Assert.IsTrue(repaired);
            Assert.AreEqual(2, d.inventory.items.Count, "item desconhecido removido");
            Assert.AreEqual("", d.inventory.ranged, "slot apontando para item ausente é limpo");
            Assert.AreEqual(1, d.progress.level);
        }

        [Test]
        public void Inventario_EquiparTrocaSlotsEArtefatos()
        {
            var inv = new InventorySystem();
            var a = inv.Add(feather);
            var b = inv.Add(feather);
            inv.Equip(a, 0);
            inv.Equip(b, 1);
            inv.Equip(a, 1);
            Assert.AreEqual(a, inv.Artifacts[1]);
            Assert.AreEqual(b, inv.Artifacts[0], "troca de posição entre slots");
            inv.Remove(a);
            Assert.IsNull(inv.Artifacts[1], "remover item desequipa");
            Assert.IsFalse(inv.UseArrow(), "sem flechas");
            inv.AddArrows(2);
            Assert.IsTrue(inv.UseArrow());
            Assert.AreEqual(1, inv.Arrows);
        }
    }
}

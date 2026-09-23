using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Ruinas.EditorTools
{
    /// <summary>
    /// Cria os dados editáveis (ScriptableObjects). Valores de combate são PROPOSTOS para ritmo e legibilidade,
    /// não números oficiais. A configuração de demonstração (referência) fica separada da campanha.
    /// </summary>
    public static class DataForge
    {
        public const string Dir = "Assets/_Project/Data";
        public const string DatabasePath = "Assets/_Project/Resources/GameDatabase.asset";

        public static T Asset<T>(string relPath) where T : ScriptableObject
        {
            string path = $"{Dir}/{relPath}.asset";
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var a = AssetDatabase.LoadAssetAtPath<T>(path);
            if (a == null)
            {
                a = ScriptableObject.CreateInstance<T>();
                AssetDatabase.CreateAsset(a, path);
            }
            EditorUtility.SetDirty(a);
            return a;
        }

        static Sprite Icon(string file) => TextureForge.LoadSprite($"Icons/{file}.png");

        static Texture2D ReadableIcon(string file)
        {
            var bytes = File.ReadAllBytes($"{TextureForge.Root}/Icons/{file}.png");
            var t = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            t.LoadImage(bytes);
            return t;
        }

        static Mesh ItemMesh(string file, Vector2 grip, float pixel = MeshForge.ItemPixel)
        {
            var tex = ReadableIcon(file);
            var m = MeshForge.ExtrudeSprite("item_" + file, tex, grip, pixel);
            Object.DestroyImmediate(tex);
            return MeshForge.Save(m, "Items/" + file);
        }

        // ------------------------------------------------------------------ Tudo

        public static GameDatabase CreateAll()
        {
            // Status
            var poison = Status("veneno", "Veneno", StatusKind.Poison, 3f, 0.5f, 3f, DamageKind.Poison, vfx: "poison_bubbles", tint: new Color(0.4f, 1f, 0.3f));
            var burning = Status("queimadura", "Queimadura", StatusKind.Burning, 3f, 0.5f, 3.5f, DamageKind.Fire, vfx: "burning", tint: new Color(1f, 0.55f, 0.2f));
            var slow = Status("lentidao", "Lentidão", StatusKind.Slow, 2.5f, 0f, 0f, DamageKind.Pure, move: 0.6f);
            var stun = Status("atordoamento", "Atordoamento", StatusKind.Stun, 1.2f, 0f, 0f, DamageKind.Pure, stuns: true, vfx: "stun_stars", policy: StackPolicy.KeepLongest);
            var vuln = Status("vulneravel", "Vulnerável", StatusKind.Vulnerable, 4f, 0f, 0f, DamageKind.Pure, taken: 1.25f, vfx: "vulnerable_glow");
            var haste = Status("pressa", "Pressa", StatusKind.Haste, 5f, 0f, 0f, DamageKind.Pure, move: 1.25f, buff: true);
            var strength = Status("forca", "Força", StatusKind.Strength, 6f, 0f, 0f, DamageKind.Pure, dealt: 1.3f, buff: true);
            var regen = Status("regeneracao", "Regeneração", StatusKind.Regeneration, 4f, 0.5f, 0f, DamageKind.Pure, heal: 2f, buff: true);

            // Ataques (tempos alinhados às poses)
            var sword1 = Attack("sword_1", 0.45f, 0.16f, 0.26f, 0.1f, 0.45f, 0.26f, 0.3f, 1.95f, 60f, -60f, 1f, 2.5f, 1f, 0.05f, 0.35f);
            var sword2 = Attack("sword_2", 0.45f, 0.16f, 0.26f, 0.1f, 0.45f, 0.26f, 0.3f, 1.95f, -60f, 60f, 1f, 2.5f, 1f, 0.05f, 0.35f);
            var sword3 = Attack("sword_3", 0.6f, 0.24f, 0.34f, 0.2f, 0.6f, 0.36f, 0.5f, 2.1f, 30f, -30f, 1.5f, 5.5f, 2f, 0.07f, 0.55f);
            var axe1 = Attack("axe_1", 0.66f, 0.3f, 0.4f, 0.25f, 0.66f, 0.42f, 0.52f, 2.2f, 25f, -25f, 1f, 6f, 3f, 0.09f, 0.4f);
            var axe2 = Attack("axe_2", 0.72f, 0.3f, 0.44f, 0.25f, 0.72f, 0.46f, 0.6f, 2.1f, 100f, -100f, 0.9f, 4.5f, 2f, 0.07f, 0.25f);
            var spear1 = Attack("spear_1", 0.36f, 0.12f, 0.2f, 0.08f, 0.36f, 0.2f, 0.24f, 2.9f, 12f, -12f, 1f, 2f, 1f, 0.04f, 0.3f);
            var spear2 = Attack("spear_2", 0.36f, 0.12f, 0.2f, 0.08f, 0.36f, 0.2f, 0.24f, 2.9f, -12f, 12f, 1f, 2f, 1f, 0.04f, 0.3f);
            var spear3 = Attack("spear_3", 0.5f, 0.18f, 0.27f, 0.14f, 0.5f, 0.28f, 0.4f, 3.1f, 15f, -15f, 1.4f, 4f, 2f, 0.06f, 0.6f);
            var punch1 = Attack("punch_1", 0.34f, 0.12f, 0.2f, 0.08f, 0.34f, 0.2f, 0.24f, 1.4f, 30f, -30f, 1f, 1.5f, 1f, 0.04f, 0.2f);
            var punch2 = Attack("punch_2", 0.34f, 0.12f, 0.2f, 0.08f, 0.34f, 0.2f, 0.24f, 1.4f, -30f, 30f, 1f, 1.5f, 1f, 0.04f, 0.2f);
            var zombieAtk = Attack("zombie_attack", 1.0f, 0.55f, 0.68f, 0f, 0f, 0.7f, 0.9f, 1.7f, 50f, -50f, 1f, 3f, 1.5f, 0.05f, 0f);
            zombieAtk.telegraph = true; zombieAtk.telegraphRadius = 1.2f; zombieAtk.swingSfx = "swing";
            var bruteSwipe = Attack("brute_swipe", 1.1f, 0.55f, 0.7f, 0f, 0f, 0.8f, 1f, 2.4f, 70f, -70f, 1f, 6f, 3f, 0.07f, 0f);
            bruteSwipe.telegraph = true; bruteSwipe.telegraphRadius = 2f;
            var bruteSlam = Attack("brute_slam", 1.7f, 1.15f, 1.3f, 0f, 0f, 1.4f, 1.6f, 2.8f, 0f, 0f, 1.6f, 8f, 4f, 0.1f, 0f);
            bruteSlam.areaAroundSelf = true; bruteSlam.areaRadius = 2.8f; bruteSlam.telegraph = true; bruteSlam.telegraphRadius = 2.8f;
            foreach (var a in new[] { zombieAtk, bruteSwipe, bruteSlam }) a.trail = false;

            // Projéteis
            var arrow = Projectile("flecha", 26f, 0f, 1.6f, 0.12f, 0, 2f, 0.6f);
            var bolt = Projectile("virote", 36f, 0f, 1.4f, 0.14f, 2, 4f, 1.5f);
            var enemyArrow = Projectile("flecha_inimiga", 17f, 0f, 2.2f, 0.15f, 0, 2f, 0.8f);
            var spit = Projectile("cuspe", 16f, 0f, 1.5f, 0.16f, 0, 1.5f, 0.8f);
            spit.impactVfx = "impact_spit"; spit.impactSfx = "spore_splat"; spit.launchSfx = "";
            var spore = Projectile("esporo", 10f, 14f, 3f, 0.22f, 0, 0f, 0f);
            spore.impactVfx = "spore_burst"; spore.impactSfx = "spore_splat"; spore.launchSfx = ""; spore.alignToVelocity = false;
            spore.areaOnImpact = new AreaEffectSpec { enabled = true, radius = 1.9f, duration = 4f, tickInterval = 0.5f, damagePerTick = 4f, kind = DamageKind.Poison, status = new StatusApplication { effect = poison, duration = 2f, chance = 1f, potency = 1f }, visualKey = "cloud_green", hostileTint = true };
            enemyArrow.launchSfx = "bow_release";

            // Encantamentos
            var sharp = Enchant("afiada", "Afiada", "+20% de dano corpo a corpo.", EnchantKind.Sharpness, 0.2f);
            var fire = Enchant("chama", "Chama", "Golpes incendeiam o alvo por 3 s.", EnchantKind.Fire, 1f);
            fire.status = new StatusApplication { effect = burning, duration = 3f, chance = 1f, potency = 1f };
            var toxic = Enchant("nuvem_toxica", "Nuvem Tóxica", "35% de chance (recarga 3 s) de criar uma nuvem venenosa no alvo.", EnchantKind.ToxicCloud, 0f);
            toxic.chance = 0.35f; toxic.cooldown = 3f;
            toxic.area = new AreaEffectSpec { enabled = true, radius = 1.9f, duration = 3f, tickInterval = 0.5f, damagePerTick = 5f, kind = DamageKind.Poison, visualKey = "cloud_green", hostileTint = false };
            var toxicDemo = Enchant("nuvem_toxica_ancestral", "Nuvem Tóxica Ancestral", "50% de chance (recarga 1,2 s) de criar uma nuvem venenosa.", EnchantKind.ToxicCloud, 0f);
            toxicDemo.chance = 0.5f; toxicDemo.cooldown = 1.2f;
            toxicDemo.area = new AreaEffectSpec { enabled = true, radius = 2.1f, duration = 3.2f, tickInterval = 0.5f, damagePerTick = 5f, kind = DamageKind.Poison, visualKey = "cloud_green", hostileTint = false };
            var swift = Enchant("agilidade", "Agilidade", "+10% de velocidade.", EnchantKind.Swiftness, 0.1f);
            var recharge = Enchant("recarga", "Recarga", "−20% na recarga de artefatos.", EnchantKind.Recharge, 0.2f);
            var vitality = Enchant("vitalidade", "Vitalidade", "+20 de vida máxima.", EnchantKind.Vitality, 20f);
            var pierce = Enchant("perfuracao", "Perfuração", "Projéteis atravessam +1 alvo.", EnchantKind.Piercing, 1f);
            var guard = Enchant("guarda", "Guarda", "−8% de dano recebido.", EnchantKind.Guard, 0.08f);

            // Artefatos
            var feather = Artifact("pena_saltadora", ArtifactKind.FeatherLeap, 3f, 2.4f, 8f, 6f, 6.5f, new Color(0.9f, 0.95f, 1f), "feather_leap", "feather_land");
            feather.applyStatus = new StatusApplication { effect = stun, duration = 1.2f, chance = 1f, potency = 1f };
            var bell = Artifact("sino_pulso", ArtifactKind.MagentaPulse, 14f, 5.5f, 30f, 7f, 0f, new Color(2.0f, 0.95f, 2.2f), "pulse_charge", "pulse_blast");
            bell.applyStatus = new StatusApplication { effect = vuln, duration = 4f, chance = 1f, potency = 1f };
            var sheaf = Artifact("feixe_dourado", ArtifactKind.SummonCompanion, 30f, 0f, 0f, 0f, 0f, new Color(1f, 0.85f, 0.3f), "summon", "");
            sheaf.duration = 25f;

            // Itens
            var espada = Weapon("espada_curta", "Espada Curta", "Lâmina equilibrada: três golpes rápidos, o último mais forte.", Rarity.Common, "icon_sword", new Vector2(2, 2), 12f, new[] { sword1, sword2, sword3 }, 6);
            var machado = Weapon("machado_pesado", "Machado Pesado", "Golpes lentos que interrompem inimigos e ateiam fogo.", Rarity.Rare, "icon_axe", new Vector2(1, 1), 20f, new[] { axe1, axe2 }, 14, fire);
            var lanca = Weapon("lanca_templo", "Lança do Templo", "Alcance longo; às vezes libera esporos venenosos.", Rarity.Rare, "icon_spear", new Vector2(2, 2), 10f, new[] { spear1, spear2, spear3 }, 14, toxic);
            var lamina = Weapon("lamina_obelisco", "Lâmina do Obelisco", "Relíquia do templo: afiada, flamejante e tóxica.", Rarity.Unique, "icon_sword", new Vector2(2, 2), 14f, new[] { sword1, sword2, sword3 }, 30, sharp, fire, toxicDemo);
            var fists = new[] { punch1, punch2 };
            var arco = Ranged("arco_curto", "Arco Curto", "Disparos rápidos e leves.", Rarity.Common, "icon_bow", new Vector2(10, 10), 9f, 0.3f, 0.25f, arrow, 6);
            var besta = Ranged("besta_pesada", "Besta Pesada", "Virotes lentos que atravessam inimigos.", Rarity.Rare, "icon_crossbow", new Vector2(10, 8), 26f, 0.65f, 0.9f, bolt, 14, pierce);
            var clara = Armor("armadura_clara", "Armadura Clara", "Placas claras: +20 de vida e 10% de redução de dano.", Rarity.Common, "icon_armor_plate", 20f, 0.1f, 0f, 0f, MaterialForge.Get("M_Hero"), 6);
            var musgo = Armor("manto_musgo", "Manto de Musgo", "Leve: +12% de velocidade e recarga de artefatos mais curta.", Rarity.Rare, "icon_armor_moss", 10f, 0.05f, 0.12f, 0f, MaterialForge.Get("M_HeroMoss"), 14, recharge);
            var penaItem = ArtifactItem("pena_saltadora", "Pena Saltadora", "Salta na direção da mira; a aterrissagem empurra e atordoa.", Rarity.Common, "icon_feather", feather, 6);
            var sinoItem = ArtifactItem("sino_pulso", "Sino do Pulso", "Pulso magenta em área: dano, empurrão e vulnerabilidade.", Rarity.Rare, "icon_bell", bell, 14);
            var feixeItem = ArtifactItem("feixe_dourado", "Feixe Dourado", "Chama uma criatura de manta vermelha que luta ao seu lado por 25 s.", Rarity.Rare, "icon_sheaf", sheaf, 14);

            // Loot
            var dropsCommon = Loot("drops_comuns", 0.7f, 1, new[] { E(emMin: 1, emMax: 3, w: 5), E(arrows: 4, w: 2), E(orb: 1f, w: 1) });
            var dropsArcher = Loot("drops_arqueiro", 0.85f, 1, new[] { E(arrows: 6, w: 4), E(emMin: 1, emMax: 2, w: 3) });
            var dropsVine = Loot("drops_vinha", 0.9f, 1, new[] { E(emMin: 2, emMax: 4, w: 3), E(orb: 1f, w: 2) });
            var dropsElite = Loot("drops_elite", 1f, 0, null, new[] { E(item: lanca), E(emMin: 10, emMax: 15) });
            var bauSala = Loot("bau_sala", 1f, 0, null, new[] { E(item: feixeItem), E(emMin: 5, emMax: 8), E(arrows: 10) });
            var bauDesvio = Loot("bau_desvio", 1f, 0, null, new[] { E(item: machado), E(item: musgo), E(emMin: 8, emMax: 12) });
            var bauArena = Loot("bau_arena", 1f, 0, null, new[] { E(item: sinoItem), E(item: besta), E(emMin: 10, emMax: 14) });
            var bauFinal = Loot("bau_final", 1f, 0, null, new[] { E(item: lamina), E(emMin: 20, emMax: 25), E(arrows: 12) });

            // Inimigos (prefabs são ligados pelo PrefabForge)
            var carnical = Enemy("carnical", "Carniçal Musgoso", EnemyArchetype.Chaser, 55f, 3.1f, 1f, new[] { zombieAtk }, 9f, 1.3f, 1.5f, 0f, null, 0f, 12, dropsCommon, 1f, false,
                new Color(0.36f, 0.55f, 0.28f), new Color(0.18f, 0.42f, 0.4f), "zombie_groan");
            var arqueiro = Enemy("arqueiro", "Arqueiro Ossudo", EnemyArchetype.Archer, 40f, 3.0f, 1f, null, 0f, 2.2f, 8f, 3.5f, enemyArrow, 7f, 14, dropsArcher, 1f, false,
                new Color(0.85f, 0.82f, 0.75f), new Color(0.5f, 0.48f, 0.44f), "skeleton_rattle");
            arqueiro.aimTime = 0.85f;
            var vinha = Enemy("vinha", "Vinha Esporífera", EnemyArchetype.SporeVine, 70f, 0f, 2f, null, 0f, 4.5f, 12f, 0f, spore, 3f, 16, dropsVine, 1f, false,
                new Color(0.55f, 0.25f, 0.5f), new Color(0.3f, 0.5f, 0.2f), "vine_charge");
            var guardiao = Enemy("guardiao", "Guardião Musgoso", EnemyArchetype.Brute, 420f, 2.5f, 6f, new[] { bruteSwipe, bruteSlam }, 16f, 1.0f, 2f, 0f, null, 0f, 90, dropsElite, 1.45f, true,
                new Color(0.3f, 0.45f, 0.42f), new Color(0.35f, 0.55f, 0.25f), "brute_roar");
            guardiao.decisionInterval = 0.2f;
            guardiao.hurtSfx = "hit_flesh";

            // Encontros
            // PROPOSTO: ondas escalonadas (combate → pausa curta → combate) para um percurso de 10–15 min em ritmo humano.
            var encCorredor = Encounter("enc_corredor", "Corredor", "Derrote os guardas do corredor", 0.4f, 1f, 20,
                W(0f, 1, S(carnical, 2)),
                W(1.5f, 0, S(carnical, 2), S(arqueiro, 1)));
            var encSala = Encounter("enc_sala", "Sala das Colunas", "Derrote os inimigos da sala das colunas", 1.2f, 2f, 40,
                W(0.5f, 1, S(carnical, 3), S(arqueiro, 1, "fundo")),
                W(1.5f, 1, S(carnical, 2), S(arqueiro, 2, "fundo")),
                W(2f, 0, S(vinha, 1), S(carnical, 3)));
            var encDesvio = Encounter("enc_desvio", "Santuário Coberto", "Opcional: explore o santuário coberto de vinhas", 0.3f, 1.5f, 30,
                W(0f, 1, S(vinha, 1, "vinha"), S(carnical, 2)),
                W(1.5f, 0, S(carnical, 2), S(arqueiro, 1)));
            var encArena = Encounter("enc_arena", "Arena do Obelisco", "Ative o obelisco e sobreviva às ondas", 2.2f, 3f, 80,
                W(0.5f, 1, S(carnical, 3), S(arqueiro, 1, "fundo")),
                W(1.5f, 1, S(vinha, 1, "vinha"), S(carnical, 2), S(arqueiro, 1, "fundo")),
                W(1.5f, 1, S(carnical, 3), S(arqueiro, 2, "fundo")),
                W(2f, 0, S(carnical, 2), S(arqueiro, 2, "fundo"), S(vinha, 1, "vinha")));
            encArena.perimeterShiftAt = 0.6f;
            var encFinal = Encounter("enc_final", "Salão do Guardião", "Derrote o Guardião Musgoso", 2f, 3f, 120,
                W(0.5f, 1, S(carnical, 3), S(arqueiro, 2, "fundo")),
                W(1.5f, 1, S(vinha, 2, "vinha"), S(carnical, 2)),
                W(1.5f, 1, S(carnical, 3), S(arqueiro, 1, "fundo")),
                W(2f, 0, S(guardiao, 1, "elite"), S(carnical, 2), S(arqueiro, 1, "fundo")));
            var encRef = Encounter("enc_referencia", "Arena (referência)", "", 0.5f, 3f, 0, W(0f, 0, S(carnical, 1)));

            // Jogador
            var player = Asset<PlayerDefinition>("Player");
            player.maxHealth = 100f; player.moveSpeed = 4.6f; player.acceleration = 42f; player.deceleration = 55f; player.turnSpeed = 1080f; player.gravity = 30f;
            player.dodgeDistance = 3.2f; player.dodgeDuration = 0.34f; player.dodgeRecovery = 0.1f; player.dodgeCooldown = 0.9f;
            player.dodgeInvulnerable = false; player.dodgeInvulnerableTime = 0f;
            player.potionHealFraction = 0.6f; player.potionCooldown = 25f; player.potionUseTime = 0.45f;
            player.critChance = 0.06f; player.critMultiplier = 2f; player.startingArrows = 30; player.maxArrows = 99; player.lives = 3;
            player.startingItems = new[] { "espada_curta", "arco_curto", "armadura_clara", "pena_saltadora" };
            player.startMelee = "espada_curta"; player.startRanged = "arco_curto"; player.startArmor = "armadura_clara";
            player.startArtifacts = new[] { "pena_saltadora", "", "" };

            // Poses
            var poses = Asset<PoseLibrary>("Poses");
            poses.clips = PoseForge.All();
            poses.InvalidateCache();

            // Perfis
            var cam = Asset<CameraProfile>("Profiles/Camera_Jogabilidade");
            // ESTIMADO no vídeo: ~47 px por unidade em 1080p no plano do jogador → ~23 unidades visíveis na vertical.
            cam.yaw = 45f; cam.pitch = 45f; cam.distance = 46f; cam.fieldOfView = 28f; cam.orthographic = false; cam.orthographicSize = 11.5f;
            // MEDIDO: o marcador do jogador no mapa sobreposto fica fixo no centro do quadro (269,5; 479,4) em 415 de 438 quadros,
            // com o herói sobre ele: a câmera centraliza os pés, sem antecipação perceptível.
            cam.nearClip = 8f; cam.farClip = 160f; cam.targetOffset = Vector3.zero; cam.followSmoothTime = 0.06f; cam.verticalSmoothTime = 0.08f;
            cam.lookAhead = 0f; cam.lookAheadSmoothTime = 0.45f; cam.useRoomBounds = true; cam.shakeScale = 1f; cam.maxShake = 0.25f;
            var capture = Asset<ReferenceCaptureProfile>("Profiles/Captura_Referencia");
            capture.outputWidth = 540; capture.outputHeight = 960; capture.cropCenter = new Vector2(0.5f, 0.5f); capture.cropHeightFraction = 1f;
            capture.captureTimes = new[] { 0f, 0.3f, 0.5f, 0.7f, 1f, 1.5f, 2.5f, 4f, 5.5f, 5.9f, 7f, 8f, 9.5f, 11.4f, 12f, 13.9f, 14f };
            capture.cameraOverride = null;

            var refScript = ReferenceScriptData(carnical, arqueiro, burning);

            // Banco central
            Directory.CreateDirectory(Path.GetDirectoryName(DatabasePath));
            var db = AssetDatabase.LoadAssetAtPath<GameDatabase>(DatabasePath);
            if (db == null)
            {
                db = ScriptableObject.CreateInstance<GameDatabase>();
                AssetDatabase.CreateAsset(db, DatabasePath);
            }
            db.items = new[] { espada, machado, lanca, lamina, arco, besta, clara, musgo, penaItem, sinoItem, feixeItem };
            db.enemies = new[] { carnical, arqueiro, vinha, guardiao };
            db.statusEffects = new[] { poison, burning, slow, stun, vuln, haste, strength, regen };
            db.enchantments = new[] { sharp, fire, toxic, toxicDemo, swift, recharge, vitality, pierce, guard };
            db.artifacts = new[] { feather, bell, sheaf };
            db.lootTables = new[] { dropsCommon, dropsArcher, dropsVine, dropsElite, bauSala, bauDesvio, bauArena, bauFinal };
            db.encounters = new[] { encCorredor, encSala, encDesvio, encArena, encFinal, encRef };
            db.projectiles = new[] { arrow, bolt, enemyArrow, spit, spore };
            var swiftPotion = Asset<ConsumableDefinition>("Consumables/pocao_rapidez");
            swiftPotion.id = "pocao_rapidez"; swiftPotion.displayName = "Poção de Rapidez";
            swiftPotion.description = "Aumenta a velocidade de movimento por alguns segundos.";
            swiftPotion.liquidColor = new Color(0.45f, 0.78f, 1f); swiftPotion.status = haste; swiftPotion.statusDuration = 8f; swiftPotion.healFraction = 0f;
            swiftPotion.pickupSfx = "heal";
            swiftPotion.worldMesh = ItemMesh("icon_potion_swift", new Vector2(10, 2), 0.05f);
            db.consumables = new[] { swiftPotion };
            db.player = player;
            db.poses = poses;
            db.sfx = AudioForge.Library;
            db.font = AssetDatabase.LoadAssetAtPath<PixelFont>(FontForge.AssetPath);
            db.inputActions = AssetDatabase.LoadAssetAtPath<UnityEngine.InputSystem.InputActionAsset>("Assets/_Project/Settings/Input/RuinasControls.inputactions");
            db.gameplayCamera = cam;
            db.captureProfile = capture;
            db.referenceScript = refScript;
            db.itemMaterial = MaterialForge.Get("M_Item");
            db.characterMaterial = MaterialForge.Get("M_Hero");
            db.poison = poison; db.burning = burning; db.stun = stun; db.vulnerable = vuln; db.slow = slow;
            db.emeraldMesh = ItemMesh("icon_emerald", new Vector2(7, 1), 0.06f);
            db.arrowsMesh = ItemMesh("icon_arrow", new Vector2(8, 8), 0.05f);
            db.healthOrbMesh = ItemMesh("icon_health_orb", new Vector2(5, 1), 0.07f);
            db.uiSkin = BuildSkin();
            EditorUtility.SetDirty(db);

            // Guardados para o PrefabForge ligar prefabs e para o LevelForge.
            Cache = new DataCache
            {
                carnical = carnical, arqueiro = arqueiro, vinha = vinha, guardiao = guardiao,
                encCorredor = encCorredor, encSala = encSala, encDesvio = encDesvio, encArena = encArena, encFinal = encFinal, encRef = encRef,
                bauSala = bauSala, bauDesvio = bauDesvio, bauArena = bauArena, bauFinal = bauFinal,
                spit = spit, fists = fists, db = db,
            };
            AssetDatabase.SaveAssets();
            return db;
        }

        public class DataCache
        {
            public EnemyDefinition carnical, arqueiro, vinha, guardiao;
            public EncounterDefinition encCorredor, encSala, encDesvio, encArena, encFinal, encRef;
            public LootTable bauSala, bauDesvio, bauArena, bauFinal;
            public ProjectileDefinition spit;
            public AttackDefinition[] fists;
            public GameDatabase db;
        }

        public static DataCache Cache;

        // ------------------------------------------------------------------ Construtores

        static StatusEffectDefinition Status(string id, string name, StatusKind kind, float dur, float tick, float dmg, DamageKind dk,
            float move = 1f, float taken = 1f, float dealt = 1f, bool stuns = false, float heal = 0f, bool buff = false, string vfx = null,
            StackPolicy policy = StackPolicy.RefreshDuration, Color? tint = null)
        {
            var s = Asset<StatusEffectDefinition>("Status/" + id);
            s.id = id; s.displayName = name; s.kind = kind; s.defaultDuration = dur; s.tickInterval = tick; s.damagePerTick = dmg; s.damageKind = dk;
            s.moveSpeedMultiplier = move; s.damageTakenMultiplier = taken; s.damageDealtMultiplier = dealt; s.stuns = stuns; s.healPerTick = heal; s.isBuff = buff;
            s.vfxKey = vfx; s.stackPolicy = policy; s.maxStacks = 1; s.tint = tint ?? Color.white;
            return s;
        }

        static AttackDefinition Attack(string id, float dur, float a0, float a1, float cw0, float cw1, float dodge, float chain, float range,
            float from, float to, float dmg, float kb, float stagger, float hitstop, float lunge)
        {
            var a = Asset<AttackDefinition>("Attacks/" + id);
            a.id = id; a.poseClip = id; a.duration = dur; a.activeStart = a0; a.activeEnd = a1; a.comboWindowStart = cw0; a.comboWindowEnd = cw1;
            a.dodgeCancelAfter = dodge; a.chainAt = chain; a.range = range; a.sweepFrom = from; a.sweepTo = to; a.arcDegrees = Mathf.Abs(to - from);
            a.height = 1f; a.damageMultiplier = dmg; a.knockback = kb; a.stagger = stagger; a.hitstop = hitstop; a.lunge = lunge;
            a.areaAroundSelf = false; a.areaRadius = 0f; a.telegraph = false; a.swingSfx = "swing"; a.hitSfx = "hit_flesh"; a.trail = true;
            return a;
        }

        static ProjectileDefinition Projectile(string id, float speed, float gravity, float life, float radius, int pierce, float kb, float stagger)
        {
            var p = Asset<ProjectileDefinition>("Projectiles/" + id);
            p.id = id; p.speed = speed; p.gravity = gravity; p.maxLifetime = life; p.radius = radius; p.pierce = pierce; p.knockback = kb; p.stagger = stagger;
            p.impactVfx = "impact_small"; p.impactSfx = "arrow_hit"; p.launchSfx = id.StartsWith("flecha") || id == "virote" ? "bow_release" : "";
            p.alignToVelocity = true; p.areaOnImpact = new AreaEffectSpec { enabled = false }; p.statuses = null;
            return p;
        }

        static EnchantmentDefinition Enchant(string id, string name, string desc, EnchantKind kind, float value)
        {
            var e = Asset<EnchantmentDefinition>("Enchantments/" + id);
            e.id = id; e.displayName = name; e.description = desc; e.kind = kind; e.value = value; e.chance = 1f; e.cooldown = 0f;
            e.area = new AreaEffectSpec { enabled = false };
            return e;
        }

        static ArtifactDefinition Artifact(string id, ArtifactKind kind, float cd, float radius, float dmg, float kb, float dist, Color color, string sfx0, string sfx1)
        {
            var a = Asset<ArtifactDefinition>("Artifacts/" + id);
            a.id = id; a.kind = kind; a.cooldown = cd; a.radius = radius; a.damage = dmg; a.knockback = kb; a.distance = dist; a.color = color;
            a.sfxStart = sfx0; a.sfxEnd = sfx1; a.useLock = 0.3f; a.duration = 0f; a.stunDuration = 1.2f;
            return a;
        }

        static ItemDefinition BaseItem(string id, string name, string desc, ItemKind kind, Rarity rarity, string icon, int value, EnchantmentDefinition[] enchants)
        {
            var i = Asset<ItemDefinition>("Items/" + id);
            i.id = id; i.displayName = name; i.description = desc; i.kind = kind; i.rarity = rarity; i.icon = Icon(icon); i.emeraldValue = value;
            i.enchantments = enchants ?? new EnchantmentDefinition[0];
            i.worldMaterial = MaterialForge.Get("M_Item");
            return i;
        }

        static ItemDefinition Weapon(string id, string name, string desc, Rarity r, string icon, Vector2 grip, float dmg, AttackDefinition[] combo, int value, params EnchantmentDefinition[] ench)
        {
            var i = BaseItem(id, name, desc, ItemKind.Melee, r, icon, value, ench);
            i.meleeDamage = dmg;
            i.combo = combo;
            i.worldMesh = ItemMesh(icon, grip);
            i.holdOffset = new Vector3(0f, -0.02f, 0.04f);
            i.holdEuler = new Vector3(0f, -90f, -45f);
            i.holdScale = 0.85f;
            return i;
        }

        static ItemDefinition Ranged(string id, string name, string desc, Rarity r, string icon, Vector2 grip, float dmg, float draw, float cd, ProjectileDefinition proj, int value, params EnchantmentDefinition[] ench)
        {
            var i = BaseItem(id, name, desc, ItemKind.Ranged, r, icon, value, ench);
            i.rangedDamage = dmg; i.drawTime = draw; i.fireCooldown = cd; i.projectile = proj; i.projectilesPerShot = 1;
            i.worldMesh = ItemMesh(icon, grip);
            i.holdOffset = new Vector3(0f, 0f, 0.05f);
            i.holdEuler = new Vector3(0f, 90f, 45f);
            i.holdScale = 0.8f;
            return i;
        }

        static ItemDefinition Armor(string id, string name, string desc, Rarity r, string icon, float hp, float red, float move, float cdr, Material skin, int value, params EnchantmentDefinition[] ench)
        {
            var i = BaseItem(id, name, desc, ItemKind.Armor, r, icon, value, ench);
            i.healthBonus = hp; i.damageReduction = red; i.moveSpeedBonus = move; i.cooldownReduction = cdr;
            i.worldMesh = ItemMesh(icon, new Vector2(10, 2), 0.05f);
            i.worldMaterial = skin;
            return i;
        }

        static ItemDefinition ArtifactItem(string id, string name, string desc, Rarity r, string icon, ArtifactDefinition art, int value)
        {
            var i = BaseItem(id, name, desc, ItemKind.Artifact, r, icon, value, null);
            i.artifact = art;
            i.worldMesh = ItemMesh(icon, new Vector2(10, 2), 0.05f);
            return i;
        }

        static LootEntry E(ItemDefinition item = null, int emMin = 0, int emMax = 0, int arrows = 0, float orb = 0f, float w = 1f)
            => new LootEntry { item = item, emeraldsMin = emMin, emeraldsMax = emMax, arrows = arrows, healthOrb = orb, weight = w };

        static LootTable Loot(string id, float chance, int rolls, LootEntry[] entries, LootEntry[] guaranteed = null)
        {
            var t = Asset<LootTable>("Loot/" + id);
            t.id = id; t.dropChance = chance; t.rolls = rolls; t.entries = entries ?? new LootEntry[0]; t.guaranteed = guaranteed ?? new LootEntry[0];
            return t;
        }

        static EnemyDefinition Enemy(string id, string name, EnemyArchetype arch, float hp, float speed, float poise, AttackDefinition[] attacks, float dmg, float cd,
            float preferred, float retreat, ProjectileDefinition proj, float projDmg, int xp, LootTable loot, float scale, bool elite, Color debrisA, Color debrisB, string alert)
        {
            var e = Asset<EnemyDefinition>("Enemies/" + id);
            e.id = id; e.displayName = name; e.archetype = arch; e.maxHealth = hp; e.moveSpeed = speed; e.poise = poise; e.attacks = attacks ?? new AttackDefinition[0];
            e.attackDamage = dmg; e.attackCooldown = cd; e.preferredRange = preferred; e.retreatRange = retreat; e.projectile = proj; e.projectileDamage = projDmg;
            e.xpReward = xp; e.loot = loot; e.scale = scale; e.isElite = elite; e.debrisColorA = debrisA; e.debrisColorB = debrisB; e.alertSfx = alert;
            e.hurtSfx = "hit_flesh"; e.deathSfx = "enemy_death"; e.perceptionRadius = 13f; e.leashRadius = 30f; e.decisionInterval = 0.25f; e.aimTime = 0.8f;
            return e;
        }

        static SpawnEntry S(EnemyDefinition e, int count, string group = "") => new SpawnEntry { enemy = e, count = count, spawnGroup = group };
        static EncounterWave W(float delay, int advance, params SpawnEntry[] spawns) => new EncounterWave { delayBefore = delay, advanceWhenRemaining = advance, spawns = spawns };

        static EncounterDefinition Encounter(string id, string name, string objective, float activating, float resolving, int xp, params EncounterWave[] waves)
        {
            var e = Asset<EncounterDefinition>("Encounters/" + id);
            e.id = id; e.displayName = name; e.objectiveText = objective; e.activatingDuration = activating; e.resolvingDuration = resolving; e.xpReward = xp;
            e.waves = waves; e.lockGatesWhileActive = true; e.perimeterShiftAt = 0.6f;
            return e;
        }

        static UISkin BuildSkin()
        {
            var s = Asset<UISkin>("UISkin");
            Sprite U(string n) => TextureForge.LoadSprite($"UI/{n}.png");
            s.slotFrame = U("slot_frame"); s.slotFrameActive = U("slot_frame_active"); s.panel = U("panel"); s.panelLight = U("panel_light");
            s.button = U("button"); s.buttonSelected = U("select"); s.badge = U("badge"); s.tooltip = U("tooltip"); s.bar = U("bar");
            s.barFill = U("white"); s.white = U("white"); s.gradientBottom = U("gradient_bottom");
            s.heartFrame = U("heart_frame"); s.heartFill = U("heart_fill"); s.heartBack = U("heart_back"); s.heartShine = U("heart_shine");
            s.potionIcon = Icon("icon_potion"); s.mapIcon = Icon("icon_map"); s.livesIcon = Icon("icon_lives"); s.arrowIcon = Icon("icon_arrow");
            s.emeraldIcon = Icon("icon_emerald"); s.dpadIcon = Icon("icon_dpad"); s.lockIcon = Icon("icon_lock");
            s.mapChest = Icon("map_chest"); s.mapGate = Icon("map_gate"); s.mapPlayer = Icon("map_player"); s.mapExit = Icon("map_exit");
            s.mapCheckpoint = Icon("map_checkpoint"); s.mapMerchant = Icon("map_merchant"); s.mapObjective = Icon("map_objective");
            s.damageBox = U("damage_box"); s.interactRing = U("select");
            return s;
        }

        // ------------------------------------------------------------------ Roteiro de referência (coordenadas locais da arena)

        static RefInputKey Key(float t, float mx, float my, RefButtons pressed = RefButtons.None, RefButtons held = RefButtons.None, Vector3? aim = null)
            => new RefInputKey { time = t, move = new Vector2(mx, my), pressed = pressed, held = held, hasAim = aim.HasValue, aimLocal = aim ?? Vector3.zero };

        /// <summary>
        /// Trajetória do jogador no trecho de referência (x, z relativos à âncora da arena, a cada 0,1 s).
        /// MEDIDA no vídeo: deslocamento da câmera por fluxo óptico (regiões fora do mapa sobreposto) ancorado
        /// na posição do topo do obelisco em ~60 quadros; escala ESTIMADA de 42,3 px/u no recorte 540×960.
        /// </summary>
        static readonly float[] ReferencePathXZ =
        {
            2.78f, -0.38f, 3.32f, -0.10f, 3.63f, 0.23f, 3.24f, 0.05f, 2.53f, -0.37f, 1.72f, -0.64f,
            0.88f, -0.34f, 0.22f, 0.46f, -0.21f, 1.13f, -0.34f, 1.28f, -0.40f, 1.32f, -0.63f, 1.52f,
            -1.39f, 2.32f, -2.56f, 3.58f, -3.73f, 4.78f, -4.78f, 5.54f, -5.00f, 5.31f, -4.44f, 5.04f,
            -3.69f, 5.20f, -2.93f, 5.43f, -2.32f, 5.61f, -2.19f, 5.67f, -2.22f, 5.70f, -2.07f, 5.76f,
            -1.79f, 5.81f, -1.77f, 5.78f, -1.79f, 5.73f, -1.49f, 5.71f, -0.61f, 5.70f, 0.28f, 5.40f,
            0.95f, 4.77f, 1.28f, 4.00f, 0.95f, 3.36f, 0.15f, 2.97f, -0.76f, 2.68f, -1.57f, 2.67f,
            -1.87f, 3.19f, -1.65f, 3.82f, -0.98f, 4.00f, -0.42f, 3.41f, -0.51f, 2.62f, -0.92f, 1.98f,
            -1.30f, 1.35f, -1.27f, 0.60f, -0.90f, -0.17f, -0.34f, -0.82f, 0.36f, -1.04f, 0.97f, -0.80f,
            1.49f, -0.37f, 1.76f, 0.11f, 1.45f, 0.27f, 0.77f, 0.21f, 0.06f, 0.18f, -0.59f, 0.54f,
            -1.00f, 1.25f, -1.03f, 1.98f, -0.69f, 2.53f, -0.04f, 2.85f, 0.53f, 2.65f, 0.60f, 2.47f,
            0.36f, 2.43f, -0.11f, 2.18f, -0.81f, 1.92f, -1.71f, 1.93f, -2.70f, 2.21f, -4.33f, 2.76f,
            -6.37f, 3.43f, -8.05f, 4.10f, -9.52f, 4.65f, -10.51f, 4.68f, -10.55f, 4.31f, -10.20f, 3.93f,
            -9.69f, 3.79f, -9.12f, 3.82f, -8.54f, 3.88f, -7.97f, 3.95f, -7.36f, 4.03f, -6.79f, 4.12f,
            -6.40f, 4.18f, -6.31f, 4.19f, -6.27f, 4.17f, -6.02f, 4.02f, -5.54f, 3.63f, -5.19f, 3.10f,
            -5.08f, 2.48f, -5.16f, 1.91f, -5.05f, 1.80f, -4.57f, 1.97f, -3.91f, 1.94f, -3.30f, 1.70f,
            -2.89f, 1.22f, -3.02f, 0.79f, -3.33f, 0.99f, -3.25f, 1.42f, -2.80f, 1.50f, -2.47f, 1.09f,
            -2.71f, 0.83f, -2.93f, 1.17f, -2.60f, 1.31f, -2.11f, 0.97f, -1.98f, 0.48f, -2.33f, 0.37f,
            -2.41f, 0.66f, -1.96f, 0.74f, -1.44f, 0.53f, -1.35f, 0.13f, -1.80f, -0.03f, -2.29f, 0.22f,
            -2.47f, 0.73f, -2.25f, 1.18f, -1.83f, 1.48f, -1.25f, 1.61f, -0.56f, 1.72f, -0.07f, 1.86f,
            0.01f, 1.92f, 0.00f, 1.97f, -0.01f, 1.99f, -0.22f, 1.75f, -0.60f, 1.35f, -1.11f, 0.91f,
            -1.54f, 0.53f, -1.58f, 0.42f, -1.53f, 0.42f, -1.49f, 0.45f, -1.26f, 0.67f, -1.26f, 0.67f,
            -1.56f, 0.68f, -1.56f, 1.08f, -1.80f, 1.07f, -2.41f, 1.25f, -2.86f, 1.69f, -3.34f, 1.52f,
            -3.96f, 1.38f, -4.75f, 1.68f, -5.54f, 2.00f, -6.39f, 2.21f, -7.02f, 2.35f, -7.34f, 2.45f,
            -7.50f, 2.49f, -7.74f, 2.42f, -8.09f, 2.19f, -8.44f, 1.88f, -8.86f, 1.54f, -9.35f, 1.50f,
            -9.79f, 1.91f, -10.14f, 2.50f, -10.28f, 2.78f,
        };

        static ReferenceScript ReferenceScriptData(EnemyDefinition zombie, EnemyDefinition archer, StatusEffectDefinition burning)
        {
            var r = Asset<ReferenceScript>("ReferenceScript");
            r.duration = 14.6f;
            r.seed = 88;
            r.path = new RefPathKey[ReferencePathXZ.Length / 2];
            for (int k = 0; k < r.path.Length; k++)
                r.path[k] = new RefPathKey { time = k * 0.1f, local = new Vector3(ReferencePathXZ[k * 2], 0f, ReferencePathXZ[k * 2 + 1]) };
            r.pathLead = 0.08f;
            r.playerStartLocal = r.path[0].local;
            r.playerStartYaw = 250f;
            r.actors = new[]
            {
                new RefActorSpawn { enemy = zombie, localPosition = new Vector3(1.6f, 0f, -1.9f), yaw = 60f },
                new RefActorSpawn { enemy = zombie, localPosition = new Vector3(-3.8f, 0f, 1.0f), yaw = 90f, startStatus = burning, statusDuration = 3f, holdUntil = 0.6f },
                new RefActorSpawn { enemy = zombie, localPosition = new Vector3(-3.4f, 0f, 6.2f), yaw = 150f, holdUntil = 1.4f },
                new RefActorSpawn { enemy = zombie, localPosition = new Vector3(2.9f, 0f, 3.3f), yaw = 225f, holdUntil = 2.8f },
                new RefActorSpawn { enemy = archer, localPosition = new Vector3(4.8f, 0f, 5.6f), yaw = 225f, holdUntil = 1.5f },
                new RefActorSpawn { companion = true, localPosition = new Vector3(3.6f, 0f, -1.8f), yaw = 250f },
            };
            var M = RefButtons.Melee;
            var N = RefButtons.None;
            // O movimento vem da trajetória medida; as chaves só trazem botões e pontos de mira (locais à arena).
            // HUD do vídeo: pena (B) usada em ~1,1 s e de volta em ~4,2 s; o slot Y (invocação) fica ativo o trecho todo.
            r.input = new[]
            {
                Key(0.00f, 0f, 0f, RefButtons.Artifact2),
                // Pulso: esfera no auge em ~0,7 s (0,14 s de preparação + expansão).
                Key(0.45f, 0f, 0f, RefButtons.Artifact1, N, new Vector3(0.9f, 0f, 0.3f)),
                Key(1.10f, 0f, 0f, RefButtons.Artifact3, N, new Vector3(-4.57f, 0f, 5.39f)),
                Key(2.15f, 0f, 0f, M, N, new Vector3(-3.4f, 0f, 6.3f)),
                Key(2.40f, 0f, 0f, M, N, new Vector3(-3.3f, 0f, 6.4f)),
                Key(2.65f, 0f, 0f, M, N, new Vector3(-3.2f, 0f, 6.3f)),
                // Golpes só nos intervalos em que o herói está parado no vídeo (golpear prende o personagem):
                // 2,0–2,6 s, 11,3–11,5 s e 12,0–12,4 s. Entre 2,7 e 5,7 s ele corre sem parar.
                Key(2.90f, 0f, 0f),
                Key(6.40f, 0f, 0f, RefButtons.Dodge),
                Key(11.28f, 0f, 0f, M, N, new Vector3(-0.2f, 0f, 2.6f)),
                Key(12.02f, 0f, 0f, M, N, new Vector3(-0.2f, 0f, 1.6f)),
                Key(12.40f, 0f, 0f),
                Key(14.60f, 0f, 0f),
            };
            // Linha do tempo observada no vídeo (quadros a cada 0,1 s).
            r.events = new[]
            {
                new RefEvent { time = 0.28f, kind = RefEventKind.FireColumn, localPosition = new Vector3(3.2f, 0f, 3.4f), duration = 0.35f },
                new RefEvent { time = 0.28f, kind = RefEventKind.FireColumn, localPosition = new Vector3(-4.5f, 0f, 0.7f), duration = 0.4f },
                new RefEvent { time = 0.30f, kind = RefEventKind.ObeliskCharge, duration = 0.22f },
                new RefEvent { time = 0.50f, kind = RefEventKind.ObeliskBeam, duration = 0.62f },
                new RefEvent { time = 0.85f, kind = RefEventKind.Cloud, localPosition = new Vector3(0.6f, 0f, 0.4f), duration = 1.8f },
                new RefEvent { time = 0.88f, kind = RefEventKind.Cloud, localPosition = new Vector3(2.0f, 0f, 1.9f), duration = 1.7f },
                new RefEvent { time = 0.92f, kind = RefEventKind.Cloud, localPosition = new Vector3(-1.1f, 0f, 2.2f), duration = 1.6f },
                new RefEvent { time = 1.00f, kind = RefEventKind.Cloud, localPosition = new Vector3(1.6f, 0f, -1.0f), duration = 1.5f },
                new RefEvent { time = 5.55f, kind = RefEventKind.FireColumn, localPosition = new Vector3(1.6f, 0f, 4.8f), duration = 0.35f },
                new RefEvent { time = 5.58f, kind = RefEventKind.ObeliskCharge, duration = 0.22f },
                new RefEvent { time = 5.80f, kind = RefEventKind.ObeliskBeam, duration = 0.62f },
                new RefEvent { time = 6.85f, kind = RefEventKind.Explosion, localPosition = new Vector3(-4.1f, 0f, 2.8f) },
                new RefEvent { time = 7.25f, kind = RefEventKind.Cloud, localPosition = new Vector3(-3.6f, 0f, 2.4f), duration = 2.2f },
                new RefEvent { time = 7.35f, kind = RefEventKind.Cloud, localPosition = new Vector3(-2.2f, 0f, 3.4f), duration = 2.2f },
                new RefEvent { time = 7.45f, kind = RefEventKind.Cloud, localPosition = new Vector3(-1.0f, 0f, 0.8f), duration = 2.0f },
                new RefEvent { time = 8.30f, kind = RefEventKind.PerimeterShift },
                new RefEvent { time = 9.00f, kind = RefEventKind.ResolveEncounter },
                new RefEvent { time = 10.95f, kind = RefEventKind.ObeliskState, text = "Resolving" },
                new RefEvent { time = 11.85f, kind = RefEventKind.ObeliskState, text = "Cleared" },
                new RefEvent { time = 11.90f, kind = RefEventKind.Flash, localPosition = new Vector3(6.3f, 3f, 6.4f) },
                new RefEvent { time = 13.00f, kind = RefEventKind.SpawnConsumable, localPosition = new Vector3(-7.4f, 0f, 3.5f), text = "pocao_rapidez" },
                new RefEvent { time = 14.50f, kind = RefEventKind.Marker, text = "fim do trecho" },
            };
            r.displayLevel = 88; r.xpFraction = 0.86f; r.lives = 3; r.arrows = 30;
            r.playerMaxHealth = 420f; r.playerDamageMultiplier = 12f; r.enemyHealthMultiplier = 28f; r.enemyDamageMultiplier = 0.8f; r.itemPower = 12;
            // ESTIMADO: o herói do vídeo percorre ~5–8 u/s nos trechos de caminhada (picos de 9–10 u/s entre 2,7 e 3,3 s);
            // a folga acima disso permite recuperar o atraso depois de golpes e esquivas.
            r.playerMoveSpeed = 9.2f;
            r.meleeId = "lamina_obelisco"; r.rangedId = "arco_curto"; r.armorId = "armadura_clara";
            // Slots: X (1) = sino, Y (2) = feixe dourado, B (3) = pena — como os ícones visíveis no HUD do vídeo.
            r.artifactIds = new[] { "sino_pulso", "feixe_dourado", "pena_saltadora" };
            return r;
        }
    }
}

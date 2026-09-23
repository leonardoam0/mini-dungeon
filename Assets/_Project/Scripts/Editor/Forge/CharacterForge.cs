using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

namespace Ruinas.EditorTools
{
    /// <summary>Monta personagens de peças rígidas (hierarquia de pivôs), com colisores e componentes de jogo.</summary>
    public static class CharacterForge
    {
        public const string Dir = "Assets/_Project/Prefabs/Characters";
        const float K = MeshForge.Texel;

        static GameObject Part(Transform parent, string name, Vector3 pivot, Mesh mesh, Material mat)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pivot;
            if (mesh != null)
            {
                var mf = go.AddComponent<MeshFilter>();
                mf.sharedMesh = mesh;
                var mr = go.AddComponent<MeshRenderer>();
                mr.sharedMaterial = mat;
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            }
            return go;
        }

        static Transform Empty(Transform parent, string name, Vector3 pos)
        {
            var t = new GameObject(name).transform;
            t.SetParent(parent, false);
            t.localPosition = pos;
            return t;
        }

        /// <summary>Humanoide 2 blocos de altura (32 texels). Membros finos para o esqueleto.</summary>
        public static CharacterRig BuildHumanoid(Transform root, string prefix, Material mat, bool thin, bool crest)
        {
            var model = new GameObject("Modelo").transform;
            model.SetParent(root, false);
            int limb = thin ? 2 : 4;
            float half = limb * 0.5f;
            var legBoxR = thin ? SkinLayout.ThinLegR : SkinLayout.LegR;
            var legBoxL = thin ? SkinLayout.ThinLegL : SkinLayout.LegL;
            var armBoxR = thin ? SkinLayout.ThinArmR : SkinLayout.ArmR;
            var armBoxL = thin ? SkinLayout.ThinArmL : SkinLayout.ArmL;

            Mesh M(string n, SkinBox b, Vector3 min) => MeshForge.Save(MeshForge.SkinBoxMesh(n, b, min, 64, 64), $"Characters/{prefix}_{n}");

            var legR = Part(model, "PernaD", new Vector3(2f * K, 12f * K, 0f), M("perna_d", legBoxR, new Vector3(-half, -12f, -half)), mat);
            var legL = Part(model, "PernaE", new Vector3(-2f * K, 12f * K, 0f), M("perna_e", legBoxL, new Vector3(-half, -12f, -half)), mat);
            var body = Part(model, "Tronco", new Vector3(0f, 12f * K, 0f), M("tronco", SkinLayout.Body, new Vector3(-4f, 0f, -2f)), mat);
            var head = Part(body.transform, "Cabeca", new Vector3(0f, 12f * K, 0f), M("cabeca", SkinLayout.Head, new Vector3(-4f, 0f, -4f)), mat);
            if (crest) Part(head.transform, "Plumagem", Vector3.zero, M("plumagem", SkinLayout.Crest, new Vector3(-1f, 8f, -3f)), mat);
            float armX = (4f + half) * K;
            var armR = Part(body.transform, "BracoD", new Vector3(armX, 10f * K, 0f), M("braco_d", armBoxR, new Vector3(-half, -10f, -half)), mat);
            var armL = Part(body.transform, "BracoE", new Vector3(-armX, 10f * K, 0f), M("braco_e", armBoxL, new Vector3(-half, -10f, -half)), mat);
            var handR = Empty(armR.transform, "MaoD", new Vector3(0f, -9.5f * K, 0f));
            var handL = Empty(armL.transform, "MaoE", new Vector3(0f, -9.5f * K, 0f));
            var back = Empty(body.transform, "Costas", new Vector3(0f, 7f * K, -2.4f * K));
            back.localRotation = Quaternion.Euler(0f, 0f, 35f);

            var rig = model.gameObject.AddComponent<CharacterRig>();
            rig.model = model;
            rig.body = body.transform; rig.head = head.transform;
            rig.armL = armL.transform; rig.armR = armR.transform;
            rig.legL = legL.transform; rig.legR = legR.transform;
            rig.handR = handR; rig.handL = handL; rig.back = back;
            rig.renderers = model.GetComponentsInChildren<Renderer>(true);
            var anim = model.gameObject.AddComponent<ProceduralAnimator>();
            anim.rig = rig;
            anim.library = DataForge.Cache?.db?.poses;
            return rig;
        }

        static CharacterRig BuildLlama(Transform root, Material mat)
        {
            var model = new GameObject("Modelo").transform;
            model.SetParent(root, false);
            Mesh M(string n, SkinBox b, Vector3 min) => MeshForge.Save(MeshForge.SkinBoxMesh(n, b, min, 128, 64), $"Characters/lhama_{n}");
            float legH = 12f;
            var body = Part(model, "Corpo", new Vector3(0f, (legH + 5f) * K, 0f), M("corpo", SkinLayout.LlamaBody, new Vector3(-6f, -5f, -9f)), mat);
            var neck = Part(body.transform, "Pescoco", new Vector3(0f, 3f * K, 6f * K), M("pescoco", SkinLayout.LlamaNeck, new Vector3(-3f, 0f, -3f)), mat);
            var head = Part(neck.transform, "Cabeca", new Vector3(0f, 12f * K, 0f), M("cabeca", SkinLayout.LlamaHead, new Vector3(-3.5f, 0f, -3f)), mat);
            Part(head.transform, "OrelhaE", new Vector3(-2.2f * K, 7f * K, -1f * K), M("orelha", SkinLayout.LlamaEar, new Vector3(-1f, 0f, -1f)), mat);
            Part(head.transform, "OrelhaD", new Vector3(2.2f * K, 7f * K, -1f * K), M("orelha", SkinLayout.LlamaEar, new Vector3(-1f, 0f, -1f)), mat);
            var legMesh = M("perna", SkinLayout.LlamaLeg, new Vector3(-2f, -12f, -2f));
            var fl = Part(model, "PataDE", new Vector3(-3.5f * K, legH * K, 6.5f * K), legMesh, mat);
            var fr = Part(model, "PataDD", new Vector3(3.5f * K, legH * K, 6.5f * K), legMesh, mat);
            var bl = Part(model, "PataTE", new Vector3(-3.5f * K, legH * K, -6.5f * K), legMesh, mat);
            var br = Part(model, "PataTD", new Vector3(3.5f * K, legH * K, -6.5f * K), legMesh, mat);
            var rig = model.gameObject.AddComponent<CharacterRig>();
            rig.model = model;
            rig.body = body.transform; rig.head = neck.transform;
            rig.armL = fl.transform; rig.armR = fr.transform; rig.legL = bl.transform; rig.legR = br.transform;
            rig.renderers = model.GetComponentsInChildren<Renderer>(true);
            var anim = model.gameObject.AddComponent<ProceduralAnimator>();
            anim.rig = rig;
            anim.quadruped = true;
            anim.strideLength = 1.2f;
            anim.legSwing = 30f;
            anim.library = DataForge.Cache?.db?.poses;
            return rig;
        }

        static CharacterRig BuildVine(Transform root, Material mat)
        {
            var model = new GameObject("Modelo").transform;
            model.SetParent(root, false);
            Mesh M(string n, SkinBox b, Vector3 min) => MeshForge.Save(MeshForge.SkinBoxMesh(n, b, min, 64, 64), $"Characters/vinha_{n}");
            var stalkMesh = M("caule", SkinLayout.VineStalk, new Vector3(-3f, 0f, -3f));
            var baseSeg = Part(model, "Caule", Vector3.zero, stalkMesh, mat);
            var mid = Part(baseSeg.transform, "Caule2", new Vector3(0f, 8f * K, 0f), stalkMesh, mat);
            mid.transform.localRotation = Quaternion.Euler(8f, 0f, 0f);
            var top = Part(mid.transform, "Caule3", new Vector3(0f, 8f * K, 0f), stalkMesh, mat);
            top.transform.localRotation = Quaternion.Euler(-12f, 0f, 0f);
            var bulb = Part(top.transform, "Bulbo", new Vector3(0f, 8f * K, 0f), M("bulbo", SkinLayout.VineBulb, new Vector3(-5f, 0f, -5f)), mat);
            var petal = M("petala", SkinLayout.VinePetal, new Vector3(-4f, 0f, 0f));
            for (int i = 0; i < 4; i++)
            {
                var p = Part(bulb.transform, "Petala" + i, new Vector3(0f, 7f * K, 0f), petal, mat);
                p.transform.localRotation = Quaternion.Euler(0f, i * 90f, 0f) * Quaternion.Euler(-35f, 0f, 0f);
                p.transform.localPosition += p.transform.localRotation * new Vector3(0f, 0f, 3.5f * K);
            }
            var leaf = M("folha", SkinLayout.VineLeaf, new Vector3(-3f, 0f, 0f));
            for (int i = 0; i < 3; i++)
            {
                var l = Part(model, "Folha" + i, new Vector3(0f, 0.05f, 0f), leaf, mat);
                l.transform.localRotation = Quaternion.Euler(0f, i * 120f + 20f, 0f) * Quaternion.Euler(-12f, 0f, 0f);
            }
            var rig = model.gameObject.AddComponent<CharacterRig>();
            rig.model = model;
            rig.body = baseSeg.transform;
            rig.head = bulb.transform;
            rig.renderers = model.GetComponentsInChildren<Renderer>(true);
            var anim = model.gameObject.AddComponent<ProceduralAnimator>();
            anim.rig = rig;
            anim.library = DataForge.Cache?.db?.poses;
            return rig;
        }

        static GameObject Save(GameObject go, string name)
        {
            Directory.CreateDirectory(Dir);
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, $"{Dir}/{name}.prefab");
            Object.DestroyImmediate(go);
            return prefab;
        }

        static void SetLayerRecursive(GameObject go, int layer)
        {
            go.layer = layer;
            foreach (Transform t in go.transform) SetLayerRecursive(t.gameObject, layer);
        }

        public static GameObject Player()
        {
            var go = new GameObject("Jogador");
            var cc = go.AddComponent<CharacterController>();
            cc.height = 1.8f; cc.radius = 0.35f; cc.center = new Vector3(0f, 0.92f, 0f);
            cc.stepOffset = 0.45f; cc.slopeLimit = 50f; cc.skinWidth = 0.04f; cc.minMoveDistance = 0f;
            var obs = go.AddComponent<NavMeshObstacle>();
            obs.shape = NavMeshObstacleShape.Capsule; obs.radius = 0.45f; obs.height = 1.8f; obs.center = new Vector3(0f, 0.9f, 0f); obs.carving = false;
            var actor = go.AddComponent<Actor>();
            actor.team = Team.Player; actor.centerHeight = 1.0f; actor.radius = 0.38f; actor.height = 1.92f;
            go.AddComponent<StatusEffectSystem>();
            go.AddComponent<PlayerMotor>();
            go.AddComponent<PlayerActionStateMachine>();
            var pc = go.AddComponent<PlayerController>();
            pc.FistCombo = DataForge.Cache.fists;
            go.AddComponent<ArtifactSystem>();
            go.AddComponent<EquipmentSystem>();
            go.AddComponent<HitboxController>();
            go.AddComponent<AttackSlotManager>();
            BuildHumanoid(go.transform, "heroina", MaterialForge.Get("M_Hero"), false, true);
            SetLayerRecursive(go, Layers.Actors);
            return Save(go, "Jogador");
        }

        static GameObject EnemyBase<TBrain>(string name, float radius, float height, bool agent) where TBrain : EnemyBrain
        {
            var go = new GameObject(name);
            var col = go.AddComponent<CapsuleCollider>();
            col.radius = radius; col.height = height; col.center = new Vector3(0f, height * 0.5f, 0f);
            var rb = go.AddComponent<Rigidbody>();
            rb.isKinematic = true; rb.useGravity = false;
            if (agent)
            {
                var a = go.AddComponent<NavMeshAgent>();
                a.radius = radius; a.height = height; a.baseOffset = 0f; a.speed = 3f; a.angularSpeed = 0f; a.acceleration = 28f;
                a.stoppingDistance = 0.1f; a.autoBraking = true; a.obstacleAvoidanceType = ObstacleAvoidanceType.MedQualityObstacleAvoidance;
            }
            else
            {
                var o = go.AddComponent<NavMeshObstacle>();
                o.shape = NavMeshObstacleShape.Capsule; o.radius = radius; o.height = height; o.center = new Vector3(0f, height * 0.5f, 0f); o.carving = false;
            }
            var actor = go.AddComponent<Actor>();
            actor.team = Team.Enemy; actor.centerHeight = height * 0.52f; actor.radius = radius; actor.height = height;
            go.AddComponent<StatusEffectSystem>();
            go.AddComponent<HitboxController>();
            go.AddComponent<TBrain>();
            return go;
        }

        public static void BuildAll()
        {
            var cache = DataForge.Cache;
            var player = Player();
            cache.db.player.prefab = player;
            EditorUtility.SetDirty(cache.db.player);

            var zombie = EnemyBase<ChaserBrain>("Carnical", 0.38f, 1.9f, true);
            BuildHumanoid(zombie.transform, "carnical", MaterialForge.Get("M_Zombie"), false, false);
            SetLayerRecursive(zombie, Layers.Actors);
            cache.carnical.prefab = Save(zombie, "Carnical");

            var skeleton = EnemyBase<ArcherBrain>("Arqueiro", 0.34f, 1.9f, true);
            var srig = BuildHumanoid(skeleton.transform, "arqueiro", MaterialForge.Get("M_Skeleton"), true, false);
            AttachHeld(srig.handL, "icon_bow", new Vector2(10, 10), new Vector3(0f, 90f, 45f), 0.8f);
            SetLayerRecursive(skeleton, Layers.Actors);
            cache.arqueiro.prefab = Save(skeleton, "Arqueiro");

            var brute = EnemyBase<BruteBrain>("Guardiao", 0.42f, 1.95f, true);
            BuildHumanoid(brute.transform, "guardiao", MaterialForge.Get("M_Brute"), false, false);
            SetLayerRecursive(brute, Layers.Actors);
            cache.guardiao.prefab = Save(brute, "Guardiao");

            var vine = EnemyBase<SporeVineBrain>("Vinha", 0.45f, 2.1f, false);
            BuildVine(vine.transform, MaterialForge.Get("M_Vine"));
            vine.transform.GetChild(vine.transform.childCount - 1).localScale = Vector3.one * 1.3f;
            SetLayerRecursive(vine, Layers.Actors);
            cache.vinha.prefab = Save(vine, "Vinha");

            // Acompanhante (criatura de manta vermelha)
            var llama = new GameObject("Lhama");
            var lc = llama.AddComponent<CapsuleCollider>();
            lc.isTrigger = true; lc.radius = 0.45f; lc.height = 1.7f; lc.center = new Vector3(0f, 0.85f, 0f);
            var lrb = llama.AddComponent<Rigidbody>();
            lrb.isKinematic = true; lrb.useGravity = false;
            var la = llama.AddComponent<NavMeshAgent>();
            la.radius = 0.45f; la.height = 1.7f; la.speed = 5.2f; la.angularSpeed = 0f; la.acceleration = 24f; la.avoidancePriority = 90;
            var lactor = llama.AddComponent<Actor>();
            lactor.team = Team.Player; lactor.centerHeight = 1.1f; lactor.radius = 0.45f; lactor.height = 1.8f;
            var brain = llama.AddComponent<CompanionBrain>();
            brain.spit = cache.spit;
            brain.spitDamage = 9f;
            BuildLlama(llama.transform, MaterialForge.Get("M_Llama"));
            SetLayerRecursive(llama, Layers.Actors);
            cache.db.companionPrefab = Save(llama, "Lhama");

            foreach (var e in new[] { cache.carnical, cache.arqueiro, cache.guardiao, cache.vinha }) EditorUtility.SetDirty(e);
            EditorUtility.SetDirty(cache.db);
            BuildProjectiles();
            AssetDatabase.SaveAssets();
        }

        static void AttachHeld(Transform hand, string icon, Vector2 grip, Vector3 euler, float scale)
        {
            if (hand == null) return;
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>($"{MeshForge.Dir}/Items/{icon}.asset");
            var go = new GameObject("Held_" + icon);
            go.transform.SetParent(hand, false);
            go.transform.localRotation = Quaternion.Euler(euler);
            go.transform.localScale = Vector3.one * scale;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = MaterialForge.Get("M_Item");
        }

        // ------------------------------------------------------------------ Projéteis

        static void BuildProjectiles()
        {
            var db = DataForge.Cache.db;
            var arrowMesh = AssetDatabase.LoadAssetAtPath<Mesh>($"{MeshForge.Dir}/Items/icon_arrow.asset");
            GameObject ArrowVisual(string name, float scale, Color trail)
            {
                var root = new GameObject(name);
                var v = new GameObject("Seta");
                v.transform.SetParent(root.transform, false);
                v.transform.localRotation = Quaternion.Euler(0f, -90f, -45f);
                v.transform.localScale = Vector3.one * scale;
                v.transform.localPosition = new Vector3(0f, 0f, -0.2f);
                v.AddComponent<MeshFilter>().sharedMesh = arrowMesh;
                var mr = v.AddComponent<MeshRenderer>();
                mr.sharedMaterial = MaterialForge.Get("M_Item");
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                var tr = root.AddComponent<TrailRenderer>();
                tr.time = 0.12f;
                tr.widthCurve = new AnimationCurve(new Keyframe(0f, 0.07f), new Keyframe(1f, 0f));
                tr.sharedMaterial = MaterialForge.Get("M_Trail");
                var g = new Gradient();
                g.SetKeys(new[] { new GradientColorKey(trail, 0f), new GradientColorKey(trail, 1f) }, new[] { new GradientAlphaKey(0.45f, 0f), new GradientAlphaKey(0f, 1f) });
                tr.colorGradient = g;
                tr.emitting = false;
                return root;
            }
            GameObject Blob(string name, Color col, float size, Color trail)
            {
                var root = new GameObject(name);
                var b = new MeshForge.Builder();
                var c32 = (Color32)col;
                b.Box(new Vector3(-size * 0.5f, -size * 0.5f, -size * 0.5f), Vector3.one * size, c32, new Rect(0, 0, 1, 1));
                b.Box(new Vector3(-size * 0.3f, size * 0.3f, -size * 0.3f), Vector3.one * size * 0.6f, (Color32)Color.Lerp(col, Color.white, 0.35f), new Rect(0, 0, 1, 1));
                var mesh = MeshForge.Save(b.ToMesh(name), "Projectiles/" + name);
                root.AddComponent<MeshFilter>().sharedMesh = mesh;
                var mr = root.AddComponent<MeshRenderer>();
                mr.sharedMaterial = MaterialForge.Get("M_Prop");
                var tr = root.AddComponent<TrailRenderer>();
                tr.time = 0.15f;
                tr.widthCurve = new AnimationCurve(new Keyframe(0f, size * 0.7f), new Keyframe(1f, 0f));
                tr.sharedMaterial = MaterialForge.Get("M_Trail");
                var g = new Gradient();
                g.SetKeys(new[] { new GradientColorKey(trail, 0f), new GradientColorKey(trail, 1f) }, new[] { new GradientAlphaKey(0.5f, 0f), new GradientAlphaKey(0f, 1f) });
                tr.colorGradient = g;
                tr.emitting = false;
                return root;
            }

            string dir = "Assets/_Project/Prefabs/Projectiles";
            Directory.CreateDirectory(dir);
            GameObject SaveP(GameObject g, string n)
            {
                var p = PrefabUtility.SaveAsPrefabAsset(g, $"{dir}/{n}.prefab");
                Object.DestroyImmediate(g);
                return p;
            }
            foreach (var proj in db.projectiles)
            {
                switch (proj.id)
                {
                    case "flecha": proj.visualPrefab = SaveP(ArrowVisual("Flecha", 0.9f, new Color(0.9f, 0.95f, 1f)), "Flecha"); break;
                    case "virote": proj.visualPrefab = SaveP(ArrowVisual("Virote", 1.05f, new Color(1f, 0.85f, 0.6f)), "Virote"); break;
                    case "flecha_inimiga": proj.visualPrefab = SaveP(ArrowVisual("FlechaInimiga", 0.9f, new Color(1f, 0.45f, 0.4f)), "FlechaInimiga"); break;
                    case "cuspe": proj.visualPrefab = SaveP(Blob("Cuspe", new Color(0.9f, 0.95f, 0.85f), 0.22f, new Color(0.9f, 1f, 0.9f)), "Cuspe"); break;
                    case "esporo": proj.visualPrefab = SaveP(Blob("Esporo", new Color(0.55f, 0.25f, 0.55f), 0.34f, new Color(0.5f, 1f, 0.35f)), "Esporo"); break;
                }
                EditorUtility.SetDirty(proj);
            }
        }
    }
}

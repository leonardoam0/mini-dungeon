using System.Collections.Generic;
using System.IO;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

namespace Ruinas.EditorTools
{
    /// <summary>Converte um LevelBuilder em objetos de cena estáveis e inspecionáveis.</summary>
    public static class LevelAssembler
    {
        public class Result
        {
            public GameObject root;
            public MapData map;
            public readonly Dictionary<string, GateController> gates = new Dictionary<string, GateController>();
            public readonly Dictionary<string, Chest> chests = new Dictionary<string, Chest>();
            public readonly Dictionary<string, List<SpawnPoint>> spawns = new Dictionary<string, List<SpawnPoint>>();
            public readonly Dictionary<string, TriggerZone> zones = new Dictionary<string, TriggerZone>();
            public readonly Dictionary<string, Transform> anchors = new Dictionary<string, Transform>();
            public readonly List<Renderer> strips = new List<Renderer>();
            public readonly List<Light> stripLights = new List<Light>();
            public readonly List<Pickup> pickups = new List<Pickup>();
            public ObeliskController obelisk;
            public ExitPortal exit;
            public Transform spawnDefault;
            public NavMeshSurface surface;
        }

        const int Chunk = 16;

        public static Result Assemble(LevelBuilder b, string sceneKey)
        {
            var res = new Result();
            var root = new GameObject("Nivel");
            res.root = root;
            GameObjectUtility.SetStaticEditorFlags(root, StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccluderStatic | StaticEditorFlags.OccludeeStatic);

            var world = MaterialForge.Get("M_World");
            string meshDir = $"Levels/{sceneKey}";
            string absDir = $"{MeshForge.Dir}/{meshDir}";
            if (Directory.Exists(absDir))
                foreach (var f in Directory.GetFiles(absDir, "*.asset")) AssetDatabase.DeleteAsset(f.Replace('\\', '/'));

            // Blocos
            var blocks = new GameObject("Blocos").transform;
            blocks.SetParent(root.transform, false);
            for (int cx = 0; cx < b.SX; cx += Chunk)
                for (int cz = 0; cz < b.SZ; cz += Chunk)
                {
                    int x1 = Mathf.Min(b.SX, cx + Chunk), z1 = Mathf.Min(b.SZ, cz + Chunk);
                    var md = VoxelMesher.Build(b.Grid, b.Reg, cx, cz, x1, z1, false);
                    if (md.vertices.Count == 0) continue;
                    var go = new GameObject($"Bloco_{cx}_{cz}");
                    go.transform.SetParent(blocks, false);
                    go.transform.position = new Vector3(cx, 0f, cz);
                    go.layer = Layers.Environment;
                    GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccludeeStatic | StaticEditorFlags.OccluderStatic);
                    var mesh = MeshForge.Save(md.ToMesh($"{sceneKey}_bloco_{cx}_{cz}"), $"{meshDir}/bloco_{cx}_{cz}");
                    go.AddComponent<MeshFilter>().sharedMesh = mesh;
                    var mr = go.AddComponent<MeshRenderer>();
                    mr.sharedMaterial = world;
                    mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;

                    var cd = VoxelMesher.Build(b.Grid, b.Reg, cx, cz, x1, z1, true);
                    if (cd.vertices.Count > 0)
                    {
                        var cmesh = MeshForge.Save(cd.ToMesh($"{sceneKey}_colisao_{cx}_{cz}", false), $"{meshDir}/colisao_{cx}_{cz}");
                        var mc = go.AddComponent<MeshCollider>();
                        mc.sharedMesh = cmesh;
                    }
                }

            // Rampas das escadas
            var ramps = new GameObject("Rampas").transform;
            ramps.SetParent(root.transform, false);
            foreach (var r in b.Ramps)
            {
                var go = new GameObject("Rampa");
                go.transform.SetParent(ramps, false);
                go.transform.SetPositionAndRotation(r.center, r.rotation);
                go.layer = Layers.Environment;
                var bc = go.AddComponent<BoxCollider>();
                bc.size = r.size;
            }

            BuildInvisibleWalls(b, root.transform);

            // Contorno luminoso
            var stripRoot = new GameObject("Contorno").transform;
            stripRoot.SetParent(root.transform, false);
            var cyan = MaterialForge.Get("M_GlowCyan");
            foreach (var s in b.Strips)
            {
                Vector3 d = s.to - s.from;
                var go = new GameObject("Faixa");
                go.transform.SetParent(stripRoot, false);
                go.transform.position = (s.from + s.to) * 0.5f;
                var mb = new MeshForge.Builder();
                Vector3 size = Mathf.Abs(d.x) > Mathf.Abs(d.z) ? new Vector3(Mathf.Abs(d.x) + s.width, 0.035f, s.width) : new Vector3(s.width, 0.035f, Mathf.Abs(d.z) + s.width);
                mb.Box(-size * 0.5f, size, new Color32(255, 255, 255, 255), new Rect(0.5f, 0.5f, 0f, 0f));
                go.AddComponent<MeshFilter>().sharedMesh = MeshForge.Save(mb.ToMesh("faixa"), $"{meshDir}/faixa_{res.strips.Count}");
                var mr = go.AddComponent<MeshRenderer>();
                mr.sharedMaterial = cyan;
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                res.strips.Add(mr);
            }

            // Luzes avulsas
            var lightsRoot = new GameObject("Luzes").transform;
            lightsRoot.SetParent(root.transform, false);
            foreach (var l in b.Lights)
            {
                var go = new GameObject("Luz");
                go.transform.SetParent(lightsRoot, false);
                go.transform.position = l.position;
                var light = go.AddComponent<Light>();
                light.type = LightType.Point;
                light.color = l.color;
                light.range = l.range;
                light.intensity = l.intensity;
                light.shadows = l.shadows ? LightShadows.Soft : LightShadows.None;
                if (l.flicker) go.AddComponent<LightFlicker>().baseIntensity = l.intensity;
                res.stripLights.Add(light);
            }

            // Adereços
            var props = new GameObject("Aderecos").transform;
            props.SetParent(root.transform, false);
            foreach (var p in b.Props)
            {
                var go = (GameObject)PrefabUtility.InstantiatePrefab(p.prefab, props);
                go.transform.SetPositionAndRotation(p.position, Quaternion.Euler(0f, p.yaw, 0f));
                go.transform.localScale = Vector3.one * p.scale;
                if (!string.IsNullOrEmpty(p.name)) go.name = p.name;
                p.configure?.Invoke(go);
                var ob = go.GetComponent<ObeliskController>();
                if (ob != null) res.obelisk = ob;
                var ex = go.GetComponent<ExitPortal>();
                if (ex != null) res.exit = ex;
            }

            // Portões, baús, checkpoints
            foreach (var g in b.Gates)
            {
                var go = (GameObject)PrefabUtility.InstantiatePrefab(PropForge.Gate, props);
                go.name = "Portao_" + g.id;
                go.transform.SetPositionAndRotation(g.pos, Quaternion.Euler(0f, g.yaw, 0f));
                var gc = go.GetComponent<GateController>();
                gc.gateId = g.id;
                // Bloqueio do portão em camada Default: colide com atores, fica fora do cozimento da NavMesh.
                foreach (var col in go.GetComponentsInChildren<BoxCollider>())
                    if (col.gameObject.name == "Bloqueio") col.gameObject.layer = Layers.Default;
                res.gates[g.id] = gc;
            }
            var chestLoot = new Dictionary<string, LootTable>();
            if (DataForge.Cache != null)
                foreach (var t in DataForge.Cache.db.lootTables) chestLoot[t.id] = t;
            foreach (var c in b.Chests)
            {
                var go = (GameObject)PrefabUtility.InstantiatePrefab(PropForge.Chest, props);
                go.name = "Bau_" + c.id;
                go.transform.SetPositionAndRotation(c.pos, Quaternion.Euler(0f, c.yaw, 0f));
                var chest = go.GetComponent<Chest>();
                chest.chestId = c.id;
                chest.loot = chestLoot.TryGetValue(c.loot, out var lt) ? lt : null;
                chest.locked = c.locked;
                res.chests[c.id] = chest;
            }
            foreach (var cp in b.Checkpoints)
            {
                var go = (GameObject)PrefabUtility.InstantiatePrefab(PropForge.Checkpoint, props);
                go.name = "Retorno_" + cp.id;
                go.transform.SetPositionAndRotation(cp.pos, Quaternion.Euler(0f, cp.yaw, 0f));
                var c = go.GetComponent<Checkpoint>();
                c.checkpointId = cp.id;
                c.order = cp.order;
                if (res.spawnDefault == null) res.spawnDefault = go.transform;
            }
            foreach (var pk in b.Pickups)
            {
                var go = new GameObject("Coletavel_" + pk.id);
                go.transform.SetParent(props, false);
                go.transform.position = pk.pos;
                var p = go.AddComponent<Pickup>();
                p.kind = pk.kind;
                p.amount = pk.amount;
                p.persistentId = pk.id;
                p.visual = BuildPickupVisual(go.transform, pk.kind);
                res.pickups.Add(p);
            }

            // Zonas, salas, surgimentos, âncoras
            var logic = new GameObject("Logica").transform;
            logic.SetParent(root.transform, false);
            foreach (var z in b.Zones)
            {
                var go = new GameObject("Zona_" + z.id);
                go.transform.SetParent(logic, false);
                go.transform.position = z.bounds.center;
                var tz = go.AddComponent<TriggerZone>();
                tz.zoneId = z.id;
                tz.size = z.bounds.size;
                tz.action = z.action;
                tz.text = z.text;
                res.zones[z.id] = tz;
            }
            foreach (var r in b.Rooms)
            {
                var go = new GameObject("Sala_" + r.name);
                go.transform.SetParent(logic, false);
                go.transform.position = r.bounds.center;
                var rb = go.AddComponent<RoomBounds>();
                rb.size = r.bounds.size;
                rb.priority = r.priority;
                rb.margin = 0f;
            }
            foreach (var kv in b.SpawnPoints)
            {
                var list = new List<SpawnPoint>();
                int sep = kv.Key.IndexOf(':');
                string group = sep >= 0 ? kv.Key.Substring(sep + 1) : "";
                foreach (var p in kv.Value)
                {
                    var go = new GameObject($"Surgimento_{kv.Key}");
                    go.transform.SetParent(logic, false);
                    go.transform.position = p;
                    var sp = go.AddComponent<SpawnPoint>();
                    sp.group = group;
                    list.Add(sp);
                }
                res.spawns[kv.Key] = list;
            }
            foreach (var a in b.Anchors)
            {
                var go = new GameObject("Ancora_" + a.Key);
                go.transform.SetParent(logic, false);
                go.transform.position = a.Value;
                res.anchors[a.Key] = go.transform;
            }

            res.map = BuildMap(b, sceneKey, res);
            return res;
        }

        static Transform BuildPickupVisual(Transform parent, PickupKind kind)
        {
            var db = DataForge.Cache.db;
            var v = new GameObject("Visual");
            v.transform.SetParent(parent, false);
            Mesh mesh = kind == PickupKind.Emeralds ? db.emeraldMesh : kind == PickupKind.Arrows ? db.arrowsMesh : db.healthOrbMesh;
            v.AddComponent<MeshFilter>().sharedMesh = mesh;
            v.AddComponent<MeshRenderer>().sharedMaterial = db.itemMaterial;
            v.transform.localScale = Vector3.one * (kind == PickupKind.Emeralds ? 0.55f : 0.7f);
            return v.transform;
        }

        /// <summary>Paredes invisíveis nas bordas da área jogável (quedas e limites), mescladas em segmentos longos.</summary>
        static void BuildInvisibleWalls(LevelBuilder b, Transform parent)
        {
            var root = new GameObject("ParedesInvisiveis").transform;
            root.SetParent(parent, false);
            // key: (orientação, linha, altura) → lista de posições ao longo
            var segs = new Dictionary<(int, int, float), List<int>>();
            void Add(int orient, int line, float h, int along)
            {
                var k = (orient, line, h);
                if (!segs.TryGetValue(k, out var l)) segs[k] = l = new List<int>();
                l.Add(along);
            }
            for (int x = 0; x < b.SX; x++)
                for (int z = 0; z < b.SZ; z++)
                {
                    if (!b.Playable[x, z]) continue;
                    float h = b.Walk[x, z];
                    for (int d = 0; d < 4; d++)
                    {
                        int nx = x + (d == 0 ? 1 : d == 1 ? -1 : 0);
                        int nz = z + (d == 2 ? 1 : d == 3 ? -1 : 0);
                        bool block;
                        if (nx < 0 || nz < 0 || nx >= b.SX || nz >= b.SZ || !b.Playable[nx, nz]) block = true;
                        else block = h - b.Walk[nx, nz] > 0.6f;
                        if (!block) continue;
                        // orientação 0: linha em x constante (borda leste/oeste); 1: z constante.
                        if (d == 0) Add(0, x + 1, h, z);
                        else if (d == 1) Add(0, x, h, z);
                        else if (d == 2) Add(1, z + 1, h, x);
                        else Add(1, z, h, x);
                    }
                }
            foreach (var kv in segs)
            {
                var list = kv.Value;
                list.Sort();
                int start = list[0], prev = list[0];
                for (int i = 1; i <= list.Count; i++)
                {
                    if (i < list.Count && list[i] == prev + 1) { prev = list[i]; continue; }
                    int orient = kv.Key.Item1, line = kv.Key.Item2;
                    float h = kv.Key.Item3;
                    float len = prev - start + 1;
                    var go = new GameObject("Parede");
                    go.transform.SetParent(root, false);
                    go.layer = Layers.InvisibleWall;
                    var bc = go.AddComponent<BoxCollider>();
                    float mid = start + len * 0.5f;
                    go.transform.position = orient == 0 ? new Vector3(line, h + 1.2f, mid) : new Vector3(mid, h + 1.2f, line);
                    bc.size = orient == 0 ? new Vector3(0.3f, 3.4f, len) : new Vector3(len, 3.4f, 0.3f);
                    if (i < list.Count) { start = list[i]; prev = list[i]; }
                }
            }
        }

        static MapData BuildMap(LevelBuilder b, string sceneKey, Result res)
        {
            string path = $"Assets/_Project/Data/Maps/Mapa_{sceneKey}.asset";
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var map = AssetDatabase.LoadAssetAtPath<MapData>(path);
            if (map == null)
            {
                map = ScriptableObject.CreateInstance<MapData>();
                AssetDatabase.CreateAsset(map, path);
            }
            map.width = b.SX;
            map.depth = b.SZ;
            map.origin = Vector3.zero;
            map.heights = new short[b.SX * b.SZ];
            map.flags = new byte[b.SX * b.SZ];
            for (int z = 0; z < b.SZ; z++)
                for (int x = 0; x < b.SX; x++)
                {
                    int i = z * b.SX + x;
                    if (!b.Playable[x, z])
                    {
                        map.heights[i] = -1;
                        continue;
                    }
                    map.heights[i] = (short)Mathf.RoundToInt(b.Walk[x, z] * 2f);
                    var f = MapCellFlags.Walkable;
                    if (b.StairCell[x, z]) f |= MapCellFlags.Stairs;
                    map.flags[i] = (byte)f;
                }
            EditorUtility.SetDirty(map);
            return map;
        }

        /// <summary>Coze a NavMesh com colisores do cenário + paredes invisíveis (portões ficam de fora e usam obstáculo).</summary>
        public static void BakeNavMesh(Result res, string sceneKey)
        {
            var surface = res.root.AddComponent<NavMeshSurface>();
            surface.collectObjects = CollectObjects.Children;
            surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
            surface.layerMask = (1 << Layers.Environment) | (1 << Layers.InvisibleWall);
            surface.agentTypeID = 0;
            surface.BuildNavMesh();
            string path = $"Assets/_Project/Scenes/NavMesh_{sceneKey}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<NavMeshData>(path);
            if (existing != null) AssetDatabase.DeleteAsset(path);
            if (surface.navMeshData != null) AssetDatabase.CreateAsset(surface.navMeshData, path);
            res.surface = surface;
        }
    }
}

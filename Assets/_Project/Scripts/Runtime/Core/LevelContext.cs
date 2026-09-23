using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

namespace Ruinas
{
    public enum LevelKind { Mission, Reference, Menu }

    /// <summary>
    /// Ponto de composição de uma cena jogável: cria os sistemas de cena (combate, VFX, projéteis, áreas,
    /// loot), instancia o jogador, a câmera e a UI, e entrega o controle ao diretor (missão ou referência).
    /// Tudo o que é criado aqui morre com a cena — reinícios não acumulam objetos nem assinaturas.
    /// </summary>
    [DefaultExecutionOrder(-500)]
    public class LevelContext : MonoBehaviour
    {
        public static LevelContext Current { get; private set; }

        [Header("Cena")]
        public LevelKind kind = LevelKind.Mission;
        public LightingProfile lighting;
        public MapData map;
        public CameraRig cameraRig;
        public Transform defaultSpawn;
        public Light sun;
        public Volume postVolume;
        public string ambienceKey = "amb_temple";
        public string musicKey = "music_explore";

        public EventBus Events { get; private set; }
        public ActorRegistry Actors { get; } = new ActorRegistry();
        public CombatResolver Combat { get; private set; }
        public VfxPool Vfx { get; private set; }
        public ProjectileSystem Projectiles { get; private set; }
        public AreaEffectSystem Areas { get; private set; }
        public LootSystem Loot { get; private set; }
        public AtmosphereController Atmosphere { get; private set; }
        public GameRandom Random { get; private set; }
        public PlayerController Player { get; private set; }
        public InventorySystem Inventory { get; private set; }
        public ProgressionSystem Progression { get; private set; }
        public UIRoot UI { get; private set; }
        public MissionDirector Mission { get; private set; }
        public ReferenceDirector Reference { get; private set; }
        public CameraRig CameraRig => cameraRig;
        public MapData Map => map;
        public bool Initialized { get; private set; }

        T GetOrAdd<T>() where T : Component
        {
            var c = GetComponent<T>();
            return c != null ? c : gameObject.AddComponent<T>();
        }

        void Awake()
        {
            Current = this;
            Events = new EventBus();
            Random = new GameRandom(Services.Args != null ? Services.Args.Seed : 1337);
            Combat = GetOrAdd<CombatResolver>();
            Vfx = GetOrAdd<VfxPool>();
            Projectiles = GetOrAdd<ProjectileSystem>();
            Areas = GetOrAdd<AreaEffectSystem>();
            Loot = GetOrAdd<LootSystem>();
            Atmosphere = GetOrAdd<AtmosphereController>();
            Mission = FindAnyObjectByType<MissionDirector>();
            Reference = FindAnyObjectByType<ReferenceDirector>();
        }

        IEnumerator Start()
        {
            var db = Services.Database;
            Combat.Init(this, Random);
            Vfx.Init(db != null ? db.vfx : null);
            Projectiles.Init(this);
            Areas.Init();
            Loot.Init(Random);
            Atmosphere.profile = lighting;
            Atmosphere.sun = sun;
            Atmosphere.volume = postVolume;
            Events.Killed += OnKilled;

            if (kind == LevelKind.Menu)
            {
                Atmosphere.Init(lighting, null);
                Initialized = true;
                yield break;
            }

            var request = Services.Flow != null ? Services.Flow.Pending : new LoadRequest();
            if (Mission != null) Mission.Init(this, request);
            else if (Reference != null) Reference.Init(this, request);
            else
            {
                var inv = new InventorySystem();
                SpawnPlayer(defaultSpawn != null ? defaultSpawn.position : Vector3.zero, Quaternion.Euler(0f, 45f, 0f), inv, new ProgressionSystem());
            }

            Atmosphere.Init(lighting, Player != null ? Player.transform : null);
            UI = UIRoot.Create(this);
            Services.Audio?.PlayAmbience(ambienceKey);
            Services.Audio?.PlayMusic(musicKey, 2f);

            var st = Services.State;
            if (st != null)
            {
                if (st.Current == GameState.Boot || st.Current == GameState.Menu) st.TrySet(GameState.Loading);
                if (st.Current == GameState.Loading) st.TrySet(GameState.Gameplay);
            }
            Initialized = true;
            Mission?.OnLevelReady();
            Reference?.OnLevelReady();
        }

        public PlayerController SpawnPlayer(Vector3 position, Quaternion rotation, InventorySystem inventory, ProgressionSystem progression, float healthOverride = 0f)
        {
            var db = Services.Database;
            Inventory = inventory;
            Progression = progression;
            var go = Instantiate(db.player.prefab, position, rotation);
            go.name = "Jogador";
            Player = go.GetComponent<PlayerController>();
            Player.Init(this, db.player, inventory, progression, healthOverride);
            if (cameraRig != null) cameraRig.Init(Player.transform, db.gameplayCamera);
            if (lighting != null && cameraRig != null) cameraRig.SetSeeThroughRadius(lighting.seeThroughRadius);
            return Player;
        }

        void OnKilled(Actor victim, Actor killer, DamageResult r)
        {
            if (victim == null || victim.team != Team.Enemy || victim.EnemyDef == null || Progression == null) return;
            Progression.AddXp(victim.EnemyDef.xpReward);
        }

        /// <summary>Remove ataques e avisos pendentes (projéteis, nuvens, anéis) — usado em reinícios e mortes.</summary>
        public void ClearTransient()
        {
            Projectiles.ClearAll();
            Areas.ClearAll();
            Vfx.ClearTelegraphs();
            Combat.ResetRegistry();
        }

        void OnDestroy()
        {
            if (Current == this) Current = null;
            Time.captureDeltaTime = 0f;
        }
    }
}

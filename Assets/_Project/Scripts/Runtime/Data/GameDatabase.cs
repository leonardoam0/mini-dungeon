using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Ruinas
{
    /// <summary>
    /// Catálogo central carregado de Resources na inicialização. Referencia os dados editáveis
    /// (itens, inimigos, efeitos, encontros) e os recursos compartilhados (fonte, skin de UI, sons, efeitos).
    /// </summary>
    [CreateAssetMenu(menuName = "Ruinas/Game Database")]
    public class GameDatabase : ScriptableObject
    {
        [Header("Conteúdo")]
        public ItemDefinition[] items;
        public EnemyDefinition[] enemies;
        public StatusEffectDefinition[] statusEffects;
        public EnchantmentDefinition[] enchantments;
        public ArtifactDefinition[] artifacts;
        public LootTable[] lootTables;
        public EncounterDefinition[] encounters;
        public ProjectileDefinition[] projectiles;
        public ConsumableDefinition[] consumables;
        public PlayerDefinition player;
        public GameObject companionPrefab;

        [Header("Apresentação")]
        public PoseLibrary poses;
        public SfxLibrary sfx;
        public VfxLibrary vfx;
        public UISkin uiSkin;
        public PixelFont font;
        public InputActionAsset inputActions;
        public CameraProfile gameplayCamera;
        public ReferenceCaptureProfile captureProfile;
        public ReferenceScript referenceScript;
        public Material itemMaterial;
        public Material characterMaterial;
        public Mesh emeraldMesh;
        public Mesh arrowsMesh;
        public Mesh healthOrbMesh;

        [Header("Status usados por sistemas")]
        public StatusEffectDefinition poison;
        public StatusEffectDefinition burning;
        public StatusEffectDefinition stun;
        public StatusEffectDefinition vulnerable;
        public StatusEffectDefinition slow;

        [NonSerialized] Dictionary<string, ItemDefinition> itemMap;
        [NonSerialized] Dictionary<string, EnemyDefinition> enemyMap;

        public void BuildLookups()
        {
            itemMap = new Dictionary<string, ItemDefinition>();
            if (items != null) foreach (var i in items) if (i != null && !string.IsNullOrEmpty(i.id)) itemMap[i.id] = i;
            enemyMap = new Dictionary<string, EnemyDefinition>();
            if (enemies != null) foreach (var e in enemies) if (e != null && !string.IsNullOrEmpty(e.id)) enemyMap[e.id] = e;
            poses?.InvalidateCache();
        }

        public ItemDefinition GetItem(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            if (itemMap == null) BuildLookups();
            return itemMap.TryGetValue(id, out var v) ? v : null;
        }

        public ConsumableDefinition GetConsumable(string id)
        {
            if (string.IsNullOrEmpty(id) || consumables == null) return null;
            foreach (var c in consumables) if (c != null && c.id == id) return c;
            return null;
        }

        public EnemyDefinition GetEnemy(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            if (enemyMap == null) BuildLookups();
            return enemyMap.TryGetValue(id, out var v) ? v : null;
        }
    }
}

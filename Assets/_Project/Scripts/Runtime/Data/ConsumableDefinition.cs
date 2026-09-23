using UnityEngine;

namespace Ruinas
{
    /// <summary>
    /// Consumível encontrado no chão (poções). É usado ao ser pego com o botão de interação: aplica um
    /// status e/ou cura na hora. Não ocupa espaço no inventário.
    /// </summary>
    [CreateAssetMenu(menuName = "Ruinas/Consumable")]
    public class ConsumableDefinition : ScriptableObject
    {
        public string id;
        public string displayName;
        [TextArea] public string description;
        public Color liquidColor = new Color(0.45f, 0.78f, 1f);
        public Mesh worldMesh;
        public StatusEffectDefinition status;
        public float statusDuration = 8f;
        [Range(0, 1)] public float healFraction;
        public string pickupSfx = "heal";
    }
}

using UnityEngine;

namespace Ruinas
{
    /// <summary>Sprites da interface (molduras pixeladas, coração, ícones do mapa e selos de botões).</summary>
    [CreateAssetMenu(menuName = "Ruinas/UI Skin")]
    public class UISkin : ScriptableObject
    {
        [Header("Molduras (9-slice)")]
        public Sprite slotFrame;
        public Sprite slotFrameActive;
        public Sprite panel;
        public Sprite panelLight;
        public Sprite button;
        public Sprite buttonSelected;
        public Sprite badge;
        public Sprite tooltip;
        public Sprite bar;
        public Sprite barFill;
        public Sprite white;
        public Sprite gradientBottom;

        [Header("Coração")]
        public Sprite heartFrame;
        public Sprite heartFill;
        public Sprite heartBack;
        public Sprite heartShine;

        [Header("Ícones do HUD")]
        public Sprite potionIcon;
        public Sprite mapIcon;
        public Sprite livesIcon;
        public Sprite arrowIcon;
        public Sprite emeraldIcon;
        public Sprite dpadIcon;
        public Sprite lockIcon;

        [Header("Mapa")]
        public Sprite mapChest;
        public Sprite mapGate;
        public Sprite mapPlayer;
        public Sprite mapExit;
        public Sprite mapCheckpoint;
        public Sprite mapMerchant;
        public Sprite mapObjective;

        [Header("Mundo")]
        public Sprite damageBox;
        public Sprite interactRing;
    }
}

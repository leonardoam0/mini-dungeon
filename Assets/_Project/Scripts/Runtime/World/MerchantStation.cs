using UnityEngine;

namespace Ruinas
{
    /// <summary>Altar de refino/mercador do trecho de respiro: flechas e aprimoramento de itens por esmeraldas.</summary>
    public class MerchantStation : Interactable
    {
        public int arrowBundle = 12;
        public int arrowPrice = 6;
        public int upgradeBasePrice = 18;

        public override string Prompt => "Negociar";

        public override void Interact(PlayerController player)
        {
            LevelContext.Current?.UI?.OpenMerchant(this);
        }

        public int UpgradePrice(ItemInstance it) => it == null ? 0 : upgradeBasePrice + (it.power - 1) * 8;
    }
}

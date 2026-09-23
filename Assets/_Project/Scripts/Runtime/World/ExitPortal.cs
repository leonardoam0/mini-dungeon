using UnityEngine;

namespace Ruinas
{
    /// <summary>Saída da missão: conclui o percurso quando ativada (após o encontro final).</summary>
    public class ExitPortal : Interactable
    {
        public bool active;
        public GameObject activeVisual;

        public override bool CanInteract(PlayerController player) => base.CanInteract(player) && active;
        public override string Prompt => "Sair das ruínas";

        public void SetActive(bool on)
        {
            active = on;
            if (activeVisual != null) activeVisual.SetActive(on);
        }

        public override void Interact(PlayerController player)
        {
            if (!active) return;
            active = false;
            Services.Audio?.Play("portal", transform.position);
            LevelContext.Current?.Mission?.CompleteMission();
        }
    }
}

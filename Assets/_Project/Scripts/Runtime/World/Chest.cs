using System.Collections;
using UnityEngine;

namespace Ruinas
{
    /// <summary>Baú com ID estável: abre uma única vez (estado salvo), anima a tampa e rola a tabela de loot.</summary>
    public class Chest : Interactable
    {
        public string chestId;
        public LootTable loot;
        public Transform lid;
        public Light glow;
        public bool locked;
        public string lockedPrompt = "Trancado";

        public bool Opened { get; private set; }

        public override bool CanInteract(PlayerController player) => base.CanInteract(player) && !Opened && !locked;
        public override string Prompt => locked ? lockedPrompt : "Abrir baú";

        public override void Interact(PlayerController player)
        {
            if (Opened || locked) return;
            Opened = true;
            StartCoroutine(OpenRoutine(player));
        }

        /// <summary>Aplica o estado salvo sem conceder recompensa novamente.</summary>
        public void SetOpenedSilently()
        {
            Opened = true;
            if (lid != null) lid.localRotation = Quaternion.Euler(-105f, 0f, 0f);
            if (glow != null) glow.enabled = false;
        }

        public void Unlock()
        {
            locked = false;
        }

        IEnumerator OpenRoutine(PlayerController player)
        {
            var ctx = LevelContext.Current;
            Services.Audio?.Play("chest_open", transform.position);
            float t = 0f;
            while (t < 0.45f)
            {
                t += Time.deltaTime;
                if (lid != null) lid.localRotation = Quaternion.Euler(Mathf.Lerp(0f, -105f, Mathf.SmoothStep(0f, 1f, t / 0.45f)), 0f, 0f);
                yield return null;
            }
            if (glow != null) glow.enabled = false;
            if (ctx != null)
            {
                ctx.Vfx.Spawn("chest_sparkle", transform.position + Vector3.up * 0.8f, Quaternion.identity);
                ctx.Loot.Roll(loot, transform.position + transform.forward * 0.9f + Vector3.up * 0.5f, 1);
                ctx.Events.RaiseChestOpened(chestId);
            }
        }
    }
}

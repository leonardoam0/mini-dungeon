using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Ruinas
{
    /// <summary>
    /// Menu principal: continuar (se houver save válido), novo jogo, cena de referência, configurações,
    /// controles e sair. O diorama ao fundo gira devagar (fora do combate não há restrição de câmera).
    /// </summary>
    public class MainMenuController : MonoBehaviour
    {
        public Transform cameraPivot;
        public float orbitSpeed = 4f;

        UIRoot root;
        readonly List<Selectable> buttons = new List<Selectable>();
        PixelText saveInfo;
        RectTransform panel;

        void Start()
        {
            var st = Services.State;
            if (st.Current == GameState.Boot) st.TrySet(GameState.Menu);
            else if (st.Current == GameState.Loading) st.TrySet(GameState.Menu);
            else if (st.Current != GameState.Menu) { st.TrySet(GameState.Loading); st.TrySet(GameState.Menu); }

            root = UIRoot.CreateForMenu();
            root.StackEmptied += () => { panel.gameObject.SetActive(true); UIFactory.Select(buttons[0]); };
            Build();
            // Diorama: obelisco aceso ao fundo.
            var obelisk = FindAnyObjectByType<ObeliskController>();
            if (obelisk != null) obelisk.SetState(EncounterState.Active);
            Services.Audio?.PlayMusic("music_menu", 1.5f);
            Services.Audio?.PlayAmbience("amb_temple");
        }

        void Build()
        {
            var canvas = UIFactory.CreateCanvas("MenuPrincipal", 50, transform);
            var rt = (RectTransform)canvas.transform;
            var shade = UIFactory.Image("Sombra", rt, UIFactory.Skin.white, new Color(0.01f, 0.03f, 0.03f, 0.58f), false);
            shade.rectTransform.anchorMin = Vector2.zero;
            shade.rectTransform.anchorMax = new Vector2(0.42f, 1f);
            shade.rectTransform.offsetMin = Vector2.zero;
            shade.rectTransform.offsetMax = Vector2.zero;

            var title = UIFactory.Text("Titulo", rt, "RUÍNAS DO OBELISCO", 8f, UIFactory.TextGold, TextAnchor.UpperLeft);
            title.rectTransform.Place(new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(110f, -120f), new Vector2(1400f, 120f));
            title.Outline = true;
            var sub = UIFactory.Text("Sub", rt, "Um trecho de masmorra em blocos — templo, obelisco e selva", 3f, UIFactory.TextDim, TextAnchor.UpperLeft);
            sub.rectTransform.Place(new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(116f, -230f), new Vector2(1400f, 40f));

            panel = UIFactory.Rect("Botoes", rt);
            panel.Place(new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(110f, -40f), new Vector2(560f, 560f));

            bool hasSave = Services.Save.HasSave;
            SaveData save = hasSave ? Services.Save.Load(Services.Database) : null;
            bool canContinue = save != null && (save.mission.inProgress || save.mission.completions > 0);
            float y = 0f;
            Button Add(string label, UnityEngine.Events.UnityAction a, bool enabled = true)
            {
                var b = UIFactory.Button(label, panel, label, a, new Vector2(520f, 70f), 3.2f);
                b.GetComponent<RectTransform>().Place(new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, y), new Vector2(520f, 70f));
                b.interactable = enabled;
                y -= 82f;
                if (enabled) buttons.Add(b);
                return b;
            }

            if (canContinue) Add(save.mission.inProgress ? "Continuar" : "Voltar ao acampamento", () => Go(SceneFlow.MissionScene, save.mission.inProgress ? MissionStartMode.Continue : MissionStartMode.ReturnToCamp));
            Add("Novo jogo", () => Go(SceneFlow.MissionScene, MissionStartMode.NewGame));
            Add("Cena de referência", () => Services.Flow.Load(new LoadRequest { scene = SceneFlow.ReferenceScene, referenceReplay = true }));
            Add("Configurações", () => { panel.gameObject.SetActive(false); root.Push(root.Settings); });
            Add("Controles", () => { panel.gameObject.SetActive(false); root.Push(root.Controls); });
            Add("Sair", GameBootstrap.Quit);
            UIFactory.SetupVerticalNavigation(buttons.ToArray());

            saveInfo = UIFactory.Text("Save", rt, "", 2.4f, UIFactory.TextDim, TextAnchor.LowerLeft);
            saveInfo.rectTransform.Place(new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(116f, 60f), new Vector2(1200f, 80f));
            if (save != null)
            {
                string when = save.savedAtUtc.Length >= 16 ? save.savedAtUtc.Substring(0, 16).Replace('T', ' ') : "";
                saveInfo.Text = $"Save: nível {save.progress.level} · {save.inventory.items.Count} itens · missões concluídas: {save.mission.completions} · {when} UTC";
                if (Services.Save.LastStatus == SaveLoadStatus.RecoveredFromBackup) saveInfo.Text += "\n{#FFD54A}" + Services.Save.LastMessage + "{/}";
            }
            else if (Services.Save.LastStatus == SaveLoadStatus.Corrupt) saveInfo.Text = "{#FF8A6A}" + Services.Save.LastMessage + "{/}";

            var hint = UIFactory.Text("Dica", rt, "Teclado/mouse ou controle · ESC/B volta", 2.2f, UIFactory.TextDim, TextAnchor.LowerRight);
            hint.rectTransform.Place(new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-60f, 50f), new Vector2(800f, 40f));

            UIFactory.Select(buttons[0]);
        }

        void Go(string scene, MissionStartMode mode) => Services.Flow.Load(new LoadRequest { scene = scene, mode = mode });

        void Update()
        {
            if (cameraPivot != null) cameraPivot.Rotate(0f, orbitSpeed * Time.unscaledDeltaTime, 0f, Space.World);
            if (UnityEngine.EventSystems.EventSystem.current != null && UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject == null && !root.ScreenOpen && buttons.Count > 0)
            {
                // Controle sem seleção: devolve o foco ao primeiro botão.
                if (UnityEngine.InputSystem.Gamepad.current != null && UnityEngine.InputSystem.Gamepad.current.wasUpdatedThisFrame)
                    UIFactory.Select(buttons[0]);
            }
        }
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Ruinas
{
    public enum ActiveDevice { KeyboardMouse, Gamepad }

    /// <summary>
    /// Ponto único de leitura das ações remapeáveis. Detecta o dispositivo ativo (para os ícones do HUD),
    /// persiste remapeamentos nas configurações e resolve conflitos trocando atalhos entre ações.
    /// </summary>
    public class InputRouter : MonoBehaviour
    {
        public const string GroupKbm = "KeyboardMouse";
        public const string GroupGamepad = "Gamepad";

        public InputActionAsset Asset { get; private set; }
        public InputActionMap Gameplay { get; private set; }

        public InputAction Move, Aim, Point, Melee, Ranged, Dodge, Artifact1, Artifact2, Artifact3,
            Potion, Interact, Inventory, Map, Pause, AttackInPlace;

        public ActiveDevice Device { get; private set; } = ActiveDevice.KeyboardMouse;
        /// <summary>Dispositivo usado para os ícones de botões (pode ser forçado, p. ex. no modo de referência).</summary>
        public ActiveDevice DisplayDevice => displayOverride ?? Device;
        public event Action<ActiveDevice> DeviceChanged;
        ActiveDevice? displayOverride;

        /// <summary>Força (ou libera, com null) os ícones de um dispositivo sem alterar a leitura das entradas.</summary>
        public void SetDisplayOverride(ActiveDevice? device)
        {
            if (displayOverride == device) return;
            displayOverride = device;
            DeviceChanged?.Invoke(DisplayDevice);
        }
        public event Action BindingsChanged;

        /// <summary>
        /// Execuções automatizadas (piloto automático, captura, gravação, teste de save) não reagem a teclado,
        /// mouse ou controle reais: teclas digitadas em outra janela não podem abrir menus nem mover o herói.
        /// </summary>
        public bool AutomationLock { get; private set; }

        public void LockForAutomation()
        {
            AutomationLock = true;
            Gameplay?.Disable();
        }

        /// <summary>Ignora o botão esquerdo do mouse até ser solto (evita que o clique que fecha um menu vire ataque).</summary>
        public bool SuppressPointerUntilRelease { get; set; }

        SettingsService settings;
        InputActionRebindingExtensions.RebindingOperation rebind;
        readonly List<InputAction> remappable = new List<InputAction>();

        public IReadOnlyList<InputAction> RemappableActions => remappable;

        public void Init(InputActionAsset asset, SettingsService settingsService)
        {
            settings = settingsService;
            Asset = asset != null ? asset : ScriptableObject.CreateInstance<InputActionAsset>();
            Gameplay = Asset.FindActionMap("Gameplay", false);
            if (Gameplay == null)
            {
                Debug.LogError("[Input] Mapa 'Gameplay' ausente; controles indisponíveis.");
                Gameplay = new InputActionMap("Gameplay");
            }

            InputAction A(string n)
            {
                var a = Gameplay.FindAction(n, false);
                if (a == null)
                {
                    Debug.LogWarning($"[Input] Ação '{n}' ausente.");
                    a = new InputAction(n);
                }
                return a;
            }

            Move = A("Move"); Aim = A("Aim"); Point = A("Point");
            Melee = A("Melee"); Ranged = A("Ranged"); Dodge = A("Dodge");
            Artifact1 = A("Artifact1"); Artifact2 = A("Artifact2"); Artifact3 = A("Artifact3");
            Potion = A("Potion"); Interact = A("Interact"); Inventory = A("Inventory");
            Map = A("Map"); Pause = A("Pause"); AttackInPlace = A("AttackInPlace");

            remappable.Clear();
            remappable.AddRange(new[] { Melee, Ranged, Dodge, Artifact1, Artifact2, Artifact3, Potion, Interact, Inventory, Map, AttackInPlace });

            LoadOverrides();
            Gameplay.Enable();
            InputSystem.onActionChange += OnActionChange;
        }

        void OnDestroy()
        {
            InputSystem.onActionChange -= OnActionChange;
            rebind?.Dispose();
        }

        void OnActionChange(object obj, InputActionChange change)
        {
            if (change != InputActionChange.ActionPerformed) return;
            if (!(obj is InputAction action)) return;
            var control = action.activeControl;
            if (control == null) return;
            if (control.device is Gamepad) SetDevice(ActiveDevice.Gamepad);
            else if (control.device is Keyboard || control.device is Mouse) SetDevice(ActiveDevice.KeyboardMouse);
        }

        void Update()
        {
            if (SuppressPointerUntilRelease && Mouse.current != null && !Mouse.current.leftButton.isPressed)
                SuppressPointerUntilRelease = false;

            // Movimento do mouse também indica teclado/mouse (a ação Point é PassThrough).
            if (Device == ActiveDevice.Gamepad && Mouse.current != null && Mouse.current.delta.ReadValue().sqrMagnitude > 4f)
                SetDevice(ActiveDevice.KeyboardMouse);
        }

        void SetDevice(ActiveDevice d)
        {
            if (Device == d) return;
            Device = d;
            DeviceChanged?.Invoke(d);
        }

        public float Deadzone => settings != null ? settings.Data.gamepadDeadzone : 0.2f;

        /// <summary>Vetor de movimento normalizado com zona morta radial (diagonais não ficam mais rápidas).</summary>
        public Vector2 MoveVector
        {
            get
            {
                Vector2 v = Move.ReadValue<Vector2>();
                return ApplyDeadzone(v, Deadzone);
            }
        }

        public Vector2 AimVector => ApplyDeadzone(Aim.ReadValue<Vector2>(), Deadzone);

        public static Vector2 ApplyDeadzone(Vector2 v, float deadzone)
        {
            float m = v.magnitude;
            if (m < deadzone) return Vector2.zero;
            if (m > 1f) return v / m;
            float scaled = (m - deadzone) / (1f - deadzone);
            return v / m * Mathf.Clamp01(scaled);
        }

        public Vector2 PointerPosition => Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;

        public static bool PointerOverUI()
        {
            var es = EventSystem.current;
            return es != null && es.IsPointerOverGameObject();
        }

        // ------------------------------------------------------------------ Remapeamento

        public string BindingLabel(InputAction action, ActiveDevice device)
        {
            if (action == null) return "";
            string group = device == ActiveDevice.Gamepad ? GroupGamepad : GroupKbm;
            int idx = action.GetBindingIndex(InputBinding.MaskByGroup(group));
            if (idx < 0) return "-";
            string s = action.GetBindingDisplayString(idx, InputBinding.DisplayStringOptions.DontIncludeInteractions);
            return ShortLabel(s, action.bindings[idx].effectivePath);
        }

        static string ShortLabel(string display, string path)
        {
            if (string.IsNullOrEmpty(path)) return display;
            switch (path)
            {
                case "<Mouse>/leftButton": return "LMB";
                case "<Mouse>/rightButton": return "RMB";
                case "<Mouse>/middleButton": return "MMB";
                case "<Gamepad>/buttonSouth": return "A";
                case "<Gamepad>/buttonEast": return "B";
                case "<Gamepad>/buttonWest": return "X";
                case "<Gamepad>/buttonNorth": return "Y";
                case "<Gamepad>/leftShoulder": return "LB";
                case "<Gamepad>/rightShoulder": return "RB";
                case "<Gamepad>/leftTrigger": return "LT";
                case "<Gamepad>/rightTrigger": return "RT";
                case "<Gamepad>/start": return "START";
                case "<Gamepad>/select": return "VIEW";
                case "<Gamepad>/dpad/down": return "+";
                case "<Gamepad>/dpad/up": return "+";
                case "<Keyboard>/leftShift": return "SHIFT";
                case "<Keyboard>/space": return "ESPAÇO";
                case "<Keyboard>/escape": return "ESC";
                case "<Keyboard>/tab": return "TAB";
            }
            if (string.IsNullOrEmpty(display)) return "?";
            return display.ToUpperInvariant();
        }

        /// <summary>
        /// Rótulo do botão de interação exibido nas dicas: no controle, a interação é contextual no botão de
        /// ataque (A), como no trecho de referência; no teclado, a tecla de interagir.
        /// </summary>
        public string InteractLabel(ActiveDevice device)
            => device == ActiveDevice.Gamepad ? BindingLabel(Melee, device) : BindingLabel(Interact, device);

        public bool IsRebinding => rebind != null;

        public void StartRebind(InputAction action, ActiveDevice device, Action<bool> done)
        {
            if (action == null || rebind != null) { done?.Invoke(false); return; }
            string group = device == ActiveDevice.Gamepad ? GroupGamepad : GroupKbm;
            int idx = action.GetBindingIndex(InputBinding.MaskByGroup(group));
            if (idx < 0) { done?.Invoke(false); return; }

            string previousPath = action.bindings[idx].effectivePath;
            bool wasEnabled = Gameplay.enabled;
            Gameplay.Disable();

            rebind = action.PerformInteractiveRebinding(idx)
                .WithControlsExcluding("<Mouse>/position")
                .WithControlsExcluding("<Mouse>/delta")
                .WithControlsExcluding("<Mouse>/scroll")
                .WithControlsExcluding("<Pointer>/position")
                .WithCancelingThrough("<Keyboard>/escape")
                .WithExpectedControlType("Button")
                .OnMatchWaitForAnother(0.08f);

            if (device == ActiveDevice.Gamepad) rebind.WithControlsHavingToMatchPath("<Gamepad>");
            else
            {
                rebind.WithControlsExcluding("<Gamepad>");
            }

            rebind.OnComplete(op =>
            {
                string newPath = action.bindings[idx].effectivePath;
                ResolveConflict(action, idx, group, newPath, previousPath);
                FinishRebind(wasEnabled);
                done?.Invoke(true);
            });
            rebind.OnCancel(op =>
            {
                FinishRebind(wasEnabled);
                done?.Invoke(false);
            });
            rebind.Start();
        }

        void FinishRebind(bool reenable)
        {
            rebind?.Dispose();
            rebind = null;
            if (reenable) Gameplay.Enable();
            SaveOverrides();
            BindingsChanged?.Invoke();
        }

        /// <summary>Se outra ação do mesmo grupo usar o novo atalho, ela recebe o atalho antigo.</summary>
        void ResolveConflict(InputAction changed, int changedIdx, string group, string newPath, string oldPath)
        {
            foreach (var other in Gameplay.actions)
            {
                for (int i = 0; i < other.bindings.Count; i++)
                {
                    if (other == changed && i == changedIdx) continue;
                    var b = other.bindings[i];
                    if (b.isComposite || string.IsNullOrEmpty(b.groups) || !b.groups.Contains(group)) continue;
                    if (!string.Equals(b.effectivePath, newPath, StringComparison.OrdinalIgnoreCase)) continue;
                    // Movimento e mira não participam de troca (são compostos/eixos).
                    if (other == Move || other == Aim || other == Point) continue;
                    other.ApplyBindingOverride(i, oldPath);
                    Debug.Log($"[Input] Conflito resolvido: '{other.name}' passou a usar {oldPath}.");
                }
            }
        }

        public void ResetBindings()
        {
            Asset.RemoveAllBindingOverrides();
            SaveOverrides();
            BindingsChanged?.Invoke();
        }

        void LoadOverrides()
        {
            if (settings == null || string.IsNullOrEmpty(settings.Data.bindingOverridesJson)) return;
            try
            {
                Asset.LoadBindingOverridesFromJson(settings.Data.bindingOverridesJson);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Input] Remapeamentos inválidos descartados: {e.Message}");
                Asset.RemoveAllBindingOverrides();
            }
        }

        void SaveOverrides()
        {
            if (settings == null) return;
            settings.Data.bindingOverridesJson = Asset.SaveBindingOverridesAsJson();
            settings.Save();
        }
    }
}

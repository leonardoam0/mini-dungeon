using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Ruinas
{
    /// <summary>
    /// Cena de referência: inicia já com a arena ativa (como o começo do vídeo), com semente, câmera,
    /// equipamento e posições conhecidos; reproduz entradas e eventos do roteiro usando os sistemas reais;
    /// captura quadros; permite congelar, alternar HUD/mapa, gravar entradas e reiniciar sem acumular nada.
    /// </summary>
    public class ReferenceDirector : MonoBehaviour
    {
        public ArenaDirector arena;
        public Transform anchor;
        public ReferenceScript script;

        LevelContext ctx;
        ReferenceReplay replay;
        FrameCapture capture;
        InputRecorder recorder;
        readonly List<EnemyBrain> spawned = new List<EnemyBrain>();
        readonly List<CompanionBrain> companions = new List<CompanionBrain>();
        readonly HashSet<int> firedEvents = new HashSet<int>();
        readonly HashSet<int> capturedTimes = new HashSet<int>();
        float replayTime;
        bool captureRun;
        bool finishedReported;
        RefInputKey[] recordedTrack;
        float recordedDuration;

        public bool ReplayRunning => replay != null && replay.Active;
        public float ReplayTime => replayTime;
        public bool Recording => recorder != null && recorder.Recording_;
        public FrameCapture Capture => capture;

        public void Init(LevelContext context, LoadRequest request)
        {
            ctx = context;
            var db = Services.Database;
            if (script == null) script = db.referenceScript;
            var args = Services.Args;
            captureRun = args != null && args.Capture;

            // Equipamento e progresso de demonstração (separados da campanha).
            var inv = new InventorySystem();
            ItemInstance Add(string id)
            {
                var def = db.GetItem(id);
                return def != null ? inv.Add(def, script.itemPower) : null;
            }
            inv.Equip(Add(script.meleeId));
            inv.Equip(Add(script.rangedId));
            inv.Equip(Add(script.armorId));
            for (int i = 0; i < 3; i++)
                if (script.artifactIds != null && i < script.artifactIds.Length) inv.Equip(Add(script.artifactIds[i]), i);
            inv.SetArrows(script.arrows);

            var prog = new ProgressionSystem
            {
                DisplayOverride = true,
                DisplayLevel = script.displayLevel,
                DisplayFraction = script.xpFraction,
            };

            Vector3 start = anchor.TransformPoint(script.playerStartLocal);
            var player = ctx.SpawnPlayer(start, Quaternion.Euler(0f, script.playerStartYaw, 0f), inv, prog, script.playerMaxHealth);
            player.DemoDamageMultiplier = script.playerDamageMultiplier;
            player.Actor.Stats.globalDamage = script.playerDamageMultiplier;
            if (script.playerMoveSpeed > 0f) player.Actor.Stats.baseMoveSpeed = script.playerMoveSpeed;

            if (captureRun)
            {
                // Registro dos danos da captura (calibração dos números exibidos em relação ao vídeo).
                ctx.Events.Damaged += r =>
                {
                    if (r.applied) Debug.Log($"[Dano] t={replayTime:0.00} alvo={(r.Target != null ? r.Target.name : "-")} fonte={r.request.sourceTag} tipo={r.request.kind} valor={r.amount} crit={r.crit}");
                };
            }

            capture = gameObject.AddComponent<FrameCapture>();
            string dir = args != null && !string.IsNullOrEmpty(args.CaptureDir) ? args.CaptureDir : Path.Combine(Application.persistentDataPath, "Capturas");
            capture.Init(db.captureProfile, dir);
            recorder = new InputRecorder(anchor);

            SetupScene();
        }

        public void OnLevelReady()
        {
            if (Services.Args != null && Services.Args.NoHud) ctx.UI?.SetHudVisible(false);
            if (Services.Args != null && Services.Args.NoMap) ctx.UI?.SetMapVisible(false);
            if (Services.Args != null && Services.Args.CaptureProfile) ctx.UI?.SetCaptureMask(true);
            var request = Services.Flow != null ? Services.Flow.Pending : null;
            if (request == null || request.referenceReplay || captureRun) StartCoroutine(StartWhenVisible());
        }

        /// <summary>Congela a simulação até o esmaecimento da tela de carregamento terminar; então inicia o roteiro.</summary>
        System.Collections.IEnumerator StartWhenVisible()
        {
            var st = Services.State;
            st.DebugFreeze = true;
            st.ApplyTimeScale();
            while (Services.Flow != null && (Services.Flow.IsLoading || Services.Flow.FadeAlpha > 0.01f)) yield return null;
            yield return null;
            st.DebugFreeze = false;
            st.ApplyTimeScale();
            StartReplay();
        }

        void SetupScene()
        {
            ctx.ClearTransient();
            foreach (var e in spawned) if (e != null) e.Despawn();
            spawned.Clear();
            foreach (var c in companions) if (c != null) c.Dismiss();
            companions.Clear();
            firedEvents.Clear();
            capturedTimes.Clear();
            ctx.Loot.ClearDynamic();

            ctx.Random.Reset(script.seed);
            arena.HealthMultiplier = script.enemyHealthMultiplier;
            arena.DamageMultiplier = script.enemyDamageMultiplier;
            arena.ResetEncounter();
            arena.ForceActiveForReference();
            if (arena.obelisk != null)
            {
                // Feixes só quando o roteiro pede (o feixe periódico da jogabilidade criaria eventos fora do vídeo).
                arena.obelisk.beamInterval = 0f;
                arena.obelisk.ResetVisuals();
            }

            var player = ctx.Player;
            player.Revive(anchor.TransformPoint(script.playerStartLocal), Quaternion.Euler(0f, script.playerStartYaw, 0f), 1f);
            player.Inventory.SetArrows(script.arrows);

            if (script.actors != null)
            {
                foreach (var a in script.actors)
                {
                    Vector3 p = anchor.TransformPoint(a.localPosition);
                    if (a.companion)
                    {
                        var prefab = Services.Database.companionPrefab;
                        if (prefab == null) continue;
                        var go = Instantiate(prefab, p, Quaternion.Euler(0f, a.yaw, 0f));
                        var cb = go.GetComponent<CompanionBrain>();
                        if (cb != null)
                        {
                            cb.Init(player, 999f, script.playerDamageMultiplier, true);
                            companions.Add(cb);
                        }
                        continue;
                    }
                    if (a.enemy == null) continue;
                    var brain = arena.Spawn(a.enemy, p, a.yaw, false);
                    if (brain == null) continue;
                    spawned.Add(brain);
                    if (a.holdUntil > 0f) brain.Freeze(true);
                    if (a.startStatus != null) brain.Actor.Status?.Apply(a.startStatus, null, a.statusDuration);
                }
            }
            ctx.CameraRig?.Snap();
        }

        public void StartReplay()
        {
            var keys = recordedTrack ?? script.input;
            float duration = recordedTrack != null ? recordedDuration : script.duration;
            replay = new ReferenceReplay(keys, anchor, duration, recordedTrack == null ? script.path : null, script.pathLead);
            replay.Start();
            replayTime = 0f;
            finishedReported = false;
            ctx.Player.OverrideSource = replay;
            // O trecho de referência foi jogado com controle: os selos do HUD mostram os botões do controle.
            Services.Input?.SetDisplayOverride(ActiveDevice.Gamepad);
            if (captureRun)
            {
                // Passo fixo e efeitos visuais sem aleatoriedade: duas capturas da mesma build saem iguais.
                Time.captureDeltaTime = 1f / 30f;
                DeterministicVfx.Begin(script.seed);
            }
        }

        public void RestartArena(bool withReplay)
        {
            SetupScene();
            if (withReplay) StartReplay();
            else
            {
                replay?.Stop();
                ctx.Player.OverrideSource = null;
                Services.Input?.SetDisplayOverride(null);
                DeterministicVfx.End();
            }
        }

        void Update()
        {
            if (ctx == null || !ctx.Initialized) return;
            HandleKeys();

            if (replay != null && replay.Active)
            {
                float dt = Time.deltaTime;
                replay.Advance(dt);
                replayTime = replay.Time;
                FireEvents();
                ReleaseActors();
                CaptureFrames();
                if (!replay.Active) OnReplayFinished();
            }

            if (recorder != null && recorder.Recording_)
                recorder.Sample(Time.deltaTime, ctx.Player.LastCommands);
        }

        void FireEvents()
        {
            if (script.events == null) return;
            for (int i = 0; i < script.events.Length; i++)
            {
                var e = script.events[i];
                if (e == null || e.time > replayTime || !firedEvents.Add(i)) continue;
                Vector3 p = anchor.TransformPoint(e.localPosition);
                switch (e.kind)
                {
                    case RefEventKind.ObeliskBeam: arena.obelisk?.EmitBeam(e.duration); break;
                    case RefEventKind.PerimeterShift: arena.perimeter?.SnapColor(true); break;
                    case RefEventKind.ResolveEncounter:
                        foreach (var b in spawned) if (b != null && b.IsAlive) b.Actor.Receiver.Invulnerable = false;
                        arena.ReleaseExternalControl();
                        break;
                    case RefEventKind.SpawnLoot:
                        var item = Services.Database.GetItem(e.text);
                        if (item != null) ctx.Loot.SpawnPickup(PickupKind.Item, p, item, 1, script.itemPower);
                        break;
                    case RefEventKind.Cloud:
                        var spec = new AreaEffectSpec { enabled = true, radius = 1.9f, duration = Mathf.Max(0.5f, e.duration), tickInterval = 0.5f, damagePerTick = 3f, kind = DamageKind.Poison, visualKey = "cloud_green", hostileTint = false };
                        // Nuvem do roteiro é só visual (no vídeo não aparecem números vindos dela).
                        ctx.Areas.Spawn(ctx.Player.Actor, Team.Player, p, spec, 0f);
                        break;
                    case RefEventKind.FireBurst:
                        ctx.Vfx.Spawn("fire_burst", p, Quaternion.identity, e.duration);
                        break;
                    case RefEventKind.Marker:
                        Debug.Log($"[Referência] {replayTime:0.00}s {e.text}");
                        break;
                    case RefEventKind.ObeliskCharge:
                        arena.obelisk?.Charge(e.duration);
                        break;
                    case RefEventKind.FireColumn:
                        ctx.Vfx.Spawn("fire_column", p, Quaternion.identity, e.duration);
                        Services.Audio?.Play("fire_roar", p);
                        break;
                    case RefEventKind.Explosion:
                        ctx.Vfx.Spawn("explosion_fire", p, Quaternion.identity, 0f, new VfxParams { color = new Color(2.6f, 1.35f, 0.4f), hasColor = true });
                        Services.Audio?.Play("explosion", p);
                        ctx.CameraRig?.AddShake(0.35f);
                        break;
                    case RefEventKind.Flash:
                        ctx.Vfx.Spawn("flash_white", p, Quaternion.identity, 0f, new VfxParams { color = new Color(2.2f, 2.2f, 2.1f), hasColor = true });
                        break;
                    case RefEventKind.ObeliskState:
                        if (arena.obelisk != null && System.Enum.TryParse<EncounterState>(e.text, out var st)) arena.obelisk.SetState(st);
                        break;
                    case RefEventKind.SpawnConsumable:
                        ctx.Loot.SpawnConsumable(e.text, p);
                        break;
                }
            }
        }

        void ReleaseActors()
        {
            if (script.actors == null) return;
            int si = 0;
            foreach (var a in script.actors)
            {
                if (a.companion || a.enemy == null) continue;
                if (si >= spawned.Count) break;
                var b = spawned[si++];
                if (b != null && a.holdUntil > 0f && replayTime >= a.holdUntil) b.Freeze(false);
            }
        }

        void CaptureFrames()
        {
            var times = Services.Database.captureProfile != null ? Services.Database.captureProfile.captureTimes : null;
            if (!captureRun || times == null) return;
            for (int i = 0; i < times.Length; i++)
            {
                if (replayTime + 1e-4f < times[i] || !capturedTimes.Add(i)) continue;
                string name = $"ref_{times[i]:00.0}s".Replace(',', '.');
                capture.Capture(name);
                LogCaptureState(name);
            }
        }

        /// <summary>
        /// Registra a posição do jogador e a projeção exata do topo do obelisco no recorte 540×960 de cada captura
        /// (usado na matriz de validação para medir o desvio de enquadramento em relação ao vídeo).
        /// </summary>
        void LogCaptureState(string name)
        {
            var cam = ctx.CameraRig != null ? ctx.CameraRig.cam : Camera.main;
            Vector3 local = anchor.InverseTransformPoint(ctx.Player.transform.position);
            string ob = "-";
            if (cam != null && arena != null && arena.obelisk != null)
            {
                Vector3 sp = cam.WorldToScreenPoint(arena.obelisk.transform.position + Vector3.up * 5.1f);
                float h = cam.pixelHeight, cropW = h * 9f / 16f, x0 = (cam.pixelWidth - cropW) * 0.5f;
                ob = string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0:0.0};{1:0.0}", (sp.x - x0) / cropW * 540f, (h - sp.y) / h * 960f);
            }
            Debug.Log(string.Format(System.Globalization.CultureInfo.InvariantCulture,
                "[Captura] {0} jogador_local={1:0.00};{2:0.00} obelisco_topo_9x16={3}", name, local.x, local.z, ob));
        }

        void OnReplayFinished()
        {
            if (finishedReported) return;
            finishedReported = true;
            ctx.Player.OverrideSource = null;
            if (captureRun)
            {
                // Quadro final e liberação do tempo real.
                capture.Capture("ref_final");
                Time.captureDeltaTime = 0f;
                Debug.Log($"[Referência] Reprodução concluída; capturas em {capture.OutputDir}");
                if (Services.Args.QuitWhenDone) StartCoroutine(QuitAfterCaptures());
            }
            else
            {
                Services.Input?.SetDisplayOverride(null);
                ctx.Events.Notify("Reprodução concluída", "Controle livre. R reinicia a arena · F1 ajuda");
            }
        }

        System.Collections.IEnumerator QuitAfterCaptures()
        {
            float t = 0f;
            while (capture.Pending > 0 && t < 10f) { t += Time.unscaledDeltaTime; yield return null; }
            yield return new WaitForSecondsRealtime(0.5f);
            GameBootstrap.Quit();
        }

        void OnDestroy()
        {
            Services.Input?.SetDisplayOverride(null);
            DeterministicVfx.End();
        }

        // ------------------------------------------------------------ Ferramentas de comparação

        void HandleKeys()
        {
            if (Services.Input != null && Services.Input.AutomationLock) return;
            var kb = Keyboard.current;
            if (kb == null || Services.State.Current == GameState.Paused) return;
            if (kb.rKey.wasPressedThisFrame) RestartArena(kb.leftCtrlKey.isPressed);
            if (kb.f1Key.wasPressedThisFrame) ctx.UI?.ToggleReferenceHelp();
            if (kb.f2Key.wasPressedThisFrame) ctx.UI?.SetHudVisible(!ctx.UI.HudVisible);
            if (kb.f3Key.wasPressedThisFrame) ctx.UI?.SetMapVisible(!ctx.UI.MapVisible);
            if (kb.f5Key.wasPressedThisFrame) ToggleFreeze();
            if (kb.f6Key.wasPressedThisFrame && ctx.CameraRig != null) ctx.CameraRig.freeLook = !ctx.CameraRig.freeLook;
            if (kb.f7Key.wasPressedThisFrame) ctx.UI?.ToggleOnionSkin(replayTime);
            if (kb.f8Key.wasPressedThisFrame) capture.Capture($"manual_{System.DateTime.Now:HHmmss_fff}");
            if (kb.f9Key.wasPressedThisFrame) ToggleRecording();
            if (kb.f10Key.wasPressedThisFrame) RestartArena(true);
            if (kb.f11Key.wasPressedThisFrame) ctx.UI?.SetCaptureMask(!ctx.UI.CaptureMaskVisible);

            var rig = ctx.CameraRig;
            if (rig != null && rig.freeLook)
            {
                float dt = Time.unscaledDeltaTime;
                if (kb.jKey.isPressed) rig.freeYaw -= 45f * dt;
                if (kb.lKey.isPressed) rig.freeYaw += 45f * dt;
                if (kb.iKey.isPressed) rig.freePitch = Mathf.Clamp(rig.freePitch + 30f * dt, 10f, 85f);
                if (kb.kKey.isPressed) rig.freePitch = Mathf.Clamp(rig.freePitch - 30f * dt, 10f, 85f);
                if (kb.uKey.isPressed) rig.freeDistance = Mathf.Max(5f, rig.freeDistance - 10f * dt);
                if (kb.oKey.isPressed) rig.freeDistance += 10f * dt;
            }
        }

        void ToggleFreeze()
        {
            var st = Services.State;
            st.DebugFreeze = !st.DebugFreeze;
            st.ApplyTimeScale();
            ctx.Events.Notify(st.DebugFreeze ? "Simulação congelada" : "Simulação retomada", st.DebugFreeze ? "F6 câmera livre (J/L/I/K/U/O)" : "");
        }

        void ToggleRecording()
        {
            if (recorder.Recording_)
            {
                string path = recorder.End(Path.Combine(Application.persistentDataPath, "Gravacoes"));
                var keys = InputRecorder.Load(path, out recordedDuration);
                if (keys != null) recordedTrack = keys;
                ctx.Events.Notify("Gravação salva", "F10 reproduz a gravação a partir do início.");
                Debug.Log($"[Referência] Gravação: {path}");
            }
            else
            {
                RestartArena(false);
                recorder.Begin();
                ctx.Events.Notify("Gravando entradas", "F9 encerra a gravação.");
            }
        }
    }
}

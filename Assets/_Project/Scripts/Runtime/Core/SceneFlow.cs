using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Ruinas
{
    public enum MissionStartMode { NewGame, Continue, ReturnToCamp }

    public class LoadRequest
    {
        public string scene;
        public MissionStartMode mode = MissionStartMode.NewGame;
        public bool referenceReplay = true;
    }

    /// <summary>Carregamento assíncrono de cenas com esmaecimento e tela de carregamento.</summary>
    public class SceneFlow : MonoBehaviour
    {
        public const string BootScene = "Boot";
        public const string MenuScene = "Menu";
        public const string ReferenceScene = "ReferenceArena";
        public const string MissionScene = "Mission";

        public LoadRequest Pending { get; private set; } = new LoadRequest { scene = "" };
        public bool IsLoading { get; private set; }
        public float FadeAlpha => fade != null ? fade.color.a : 0f;

        Canvas canvas;
        UnityEngine.UI.Image fade;
        PixelText label;

        public void Init(GameDatabase db)
        {
            canvas = UIFactory.CreateCanvas("[Fader]", 5000, transform, false);
            fade = UIFactory.Image("Preto", canvas.transform, db != null && db.uiSkin != null ? db.uiSkin.white : null, new Color(0.01f, 0.02f, 0.02f, 0f), false);
            fade.rectTransform.Stretch();
            fade.raycastTarget = false;
            label = UIFactory.Text("Carregando", canvas.transform, "", 3f, UIFactory.TextNormal, TextAnchor.LowerRight);
            label.rectTransform.Stretch(40, 40, 60, 50);
            canvas.enabled = false;
        }

        public void Load(LoadRequest request)
        {
            if (IsLoading || request == null) return;
            StartCoroutine(LoadRoutine(request));
        }

        public void Load(string scene) => Load(new LoadRequest { scene = scene });

        IEnumerator LoadRoutine(LoadRequest request)
        {
            IsLoading = true;
            Pending = request;
            Services.State.TrySet(GameState.Loading);
            yield return FadeTo(1f, 0.25f);
            label.Text = "Carregando...";
            yield return null;

            var op = SceneManager.LoadSceneAsync(request.scene, LoadSceneMode.Single);
            if (op == null)
            {
                Debug.LogError($"[Cenas] Cena '{request.scene}' não está nas Build Settings.");
                label.Text = "";
                IsLoading = false;
                yield break;
            }
            while (!op.isDone)
            {
                label.Text = $"Carregando... {Mathf.RoundToInt(op.progress * 100f)}%";
                yield return null;
            }
            // Deixa Start() da nova cena montar o nível antes de revelar.
            yield return null;
            yield return null;
            label.Text = "";
            yield return FadeTo(0f, 0.35f);
            IsLoading = false;
        }

        public IEnumerator FadeTo(float target, float duration)
        {
            if (fade == null) yield break;
            canvas.enabled = true;
            float start = fade.color.a;
            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                SetAlpha(Mathf.Lerp(start, target, Mathf.Clamp01(t / duration)));
                yield return null;
            }
            SetAlpha(target);
            if (target <= 0.001f) canvas.enabled = false;
        }

        void SetAlpha(float a)
        {
            var c = fade.color;
            c.a = a;
            fade.color = c;
        }

        /// <summary>Esmaece, executa a ação (ex.: reposicionar no checkpoint) e revela novamente.</summary>
        public void FadeThrough(Action middle, float outTime = 0.3f, float hold = 0.15f, float inTime = 0.35f)
        {
            StartCoroutine(FadeThroughRoutine(middle, outTime, hold, inTime));
        }

        IEnumerator FadeThroughRoutine(Action middle, float outTime, float hold, float inTime)
        {
            yield return FadeTo(1f, outTime);
            try { middle?.Invoke(); }
            catch (Exception e) { Debug.LogException(e); }
            yield return new WaitForSecondsRealtime(hold);
            yield return FadeTo(0f, inTime);
        }
    }
}

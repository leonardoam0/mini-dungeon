using UnityEngine;
using UnityEngine.AI;

namespace Ruinas
{
    /// <summary>
    /// Portão de barras verticais com campo violeta quando trancado. Fechado: colisor sólido e obstáculo
    /// de navegação (inimigos não atravessam). Aberto: barras descem e o campo apaga.
    /// </summary>
    public class GateController : MonoBehaviour
    {
        public string gateId;
        public Transform bars;
        public Renderer energyField;
        public Light energyLight;
        public Collider blocker;
        public NavMeshObstacle obstacle;
        public bool startsOpen = true;
        public float openDepth = 2.7f;
        public float speed = 3.2f;

        public bool IsOpen { get; private set; }
        float level;          // 0 = fechado, 1 = aberto
        Vector3 barsClosedPos;
        bool initialized;
        MaterialPropertyBlock mpb;
        static readonly int IntensityId = Shader.PropertyToID("_Intensity");

        void Awake() => Init();

        void Init()
        {
            if (initialized) return;
            initialized = true;
            mpb = new MaterialPropertyBlock();
            if (bars != null) barsClosedPos = bars.localPosition;
            IsOpen = startsOpen;
            level = startsOpen ? 1f : 0f;
            Apply();
        }

        public void SetOpen(bool open, bool instant = false)
        {
            Init();
            if (IsOpen == open && !instant) return;
            bool changed = IsOpen != open;
            IsOpen = open;
            if (instant) level = open ? 1f : 0f;
            if (changed && !instant) Services.Audio?.Play(open ? "gate_open" : "gate_close", transform.position);
            Apply();
        }

        void Update()
        {
            float target = IsOpen ? 1f : 0f;
            if (Mathf.Approximately(level, target)) return;
            level = Mathf.MoveTowards(level, target, Time.deltaTime * speed / Mathf.Max(0.1f, openDepth) * 1.2f);
            Apply();
        }

        void Apply()
        {
            if (bars != null) bars.localPosition = barsClosedPos + Vector3.down * openDepth * level;
            bool solid = level < 0.85f;
            if (blocker != null) blocker.enabled = solid;
            if (obstacle != null) obstacle.enabled = solid;
            float field = 1f - level;
            if (energyField != null)
            {
                energyField.enabled = field > 0.02f;
                energyField.GetPropertyBlock(mpb);
                mpb.SetFloat(IntensityId, 1.4f * field);
                energyField.SetPropertyBlock(mpb);
            }
            if (energyLight != null) energyLight.intensity = 2.2f * field;
        }
    }
}

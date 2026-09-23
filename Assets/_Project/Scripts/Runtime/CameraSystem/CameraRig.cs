using UnityEngine;

namespace Ruinas
{
    /// <summary>
    /// Câmera elevada em três quartos com azimute fixo. Segue a raiz do jogador (movida pelo motor),
    /// nunca os ossos da animação; o eixo vertical é mais amortecido para absorver degraus e saltos.
    /// Roda depois do movimento (ordem de execução alta, em LateUpdate).
    /// </summary>
    [DefaultExecutionOrder(200)]
    public class CameraRig : MonoBehaviour
    {
        static readonly int SeeThroughId = Shader.PropertyToID("_RuinasSeeThrough");

        public CameraProfile profile;
        public Camera cam;

        Transform target;
        PlayerMotor motor;
        Vector3 focus;
        Vector3 focusVel;
        float focusY;
        float focusYVel;
        Vector3 lookAhead;
        Vector3 lookAheadVel;
        float shake;
        float shakeTime;
        bool initialized;
        float seeThroughRadius = 0.12f;

        [Header("Depuração (modo de referência)")]
        public bool freeLook;
        public float freeYaw;
        public float freePitch;
        public float freeDistance;

        public Transform Target => target;
        public Vector3 Focus => new Vector3(focus.x, focusY, focus.z);

        public void Init(Transform followTarget, CameraProfile p)
        {
            if (cam == null) cam = GetComponentInChildren<Camera>();
            if (p != null) profile = p;
            target = followTarget;
            motor = followTarget != null ? followTarget.GetComponent<PlayerMotor>() : null;
            ApplyProfile(profile);
            initialized = true;
            Snap();
        }

        public void SetSeeThroughRadius(float r) => seeThroughRadius = r;

        public void ApplyProfile(CameraProfile p)
        {
            if (p == null || cam == null) return;
            profile = p;
            cam.orthographic = p.orthographic;
            cam.orthographicSize = p.orthographicSize;
            cam.fieldOfView = p.fieldOfView;
            cam.nearClipPlane = p.nearClip;
            cam.farClipPlane = p.farClip;
            freeYaw = p.yaw;
            freePitch = p.pitch;
            freeDistance = p.distance;
        }

        public void Snap()
        {
            if (target == null) return;
            Vector3 p = target.position + profile.targetOffset;
            focus = ClampToRoom(p);
            focusY = p.y;
            focusVel = Vector3.zero;
            focusYVel = 0f;
            lookAhead = Vector3.zero;
            lookAheadVel = Vector3.zero;
            Place();
        }

        public void AddShake(float amount)
        {
            if (Services.Settings != null && !Services.Settings.Data.cameraShake) return;
            if (profile == null) return;
            shake = Mathf.Min(profile.maxShake, shake + amount * profile.shakeScale);
        }

        Vector3 ClampToRoom(Vector3 p)
        {
            if (profile == null || !profile.useRoomBounds) return p;
            var room = RoomBounds.Find(target != null ? target.position : p);
            return room != null ? room.Clamp(p) : p;
        }

        void LateUpdate()
        {
            if (!initialized || target == null || profile == null) return;
            float dt = Time.deltaTime;
            if (dt <= 0f) { Place(); return; }

            Vector3 p = target.position + profile.targetOffset;
            Vector3 vel = motor != null ? motor.Velocity : Vector3.zero;
            vel.y = 0f;
            float maxSpeed = motor != null ? Mathf.Max(0.1f, motor.MaxSpeed) : 5f;
            Vector3 laTarget = vel.sqrMagnitude > 0.01f ? vel.normalized * profile.lookAhead * Mathf.Clamp01(vel.magnitude / maxSpeed) : Vector3.zero;
            lookAhead = Vector3.SmoothDamp(lookAhead, laTarget, ref lookAheadVel, profile.lookAheadSmoothTime, Mathf.Infinity, dt);

            Vector3 desired = ClampToRoom(p + lookAhead);
            Vector3 flat = new Vector3(focus.x, 0f, focus.z);
            Vector3 flatDesired = new Vector3(desired.x, 0f, desired.z);
            flat = Vector3.SmoothDamp(flat, flatDesired, ref focusVel, profile.followSmoothTime, Mathf.Infinity, dt);
            focus = new Vector3(flat.x, 0f, flat.z);
            focusY = Mathf.SmoothDamp(focusY, p.y, ref focusYVel, profile.verticalSmoothTime, Mathf.Infinity, dt);

            if (shake > 0f)
            {
                shakeTime += dt * 40f;
                shake = Mathf.MoveTowards(shake, 0f, dt * 1.6f);
            }
            Place();
        }

        void Place()
        {
            if (profile == null) return;
            float yaw = freeLook ? freeYaw : profile.yaw;
            float pitch = freeLook ? freePitch : profile.pitch;
            float dist = freeLook ? freeDistance : profile.distance;
            var rot = Quaternion.Euler(pitch, yaw, 0f);
            Vector3 f = new Vector3(focus.x, focusY, focus.z);
            Vector3 pos = f - rot * Vector3.forward * dist;
            if (Mathf.Abs(profile.screenOffsetY) > 1e-4f && cam != null)
            {
                float visibleH = cam.orthographic ? cam.orthographicSize * 2f : 2f * dist * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
                pos += rot * Vector3.up * profile.screenOffsetY * visibleH;
            }
            if (shake > 0.0001f)
            {
                float sx = (Mathf.PerlinNoise(shakeTime, 0.3f) - 0.5f) * 2f;
                float sy = (Mathf.PerlinNoise(0.7f, shakeTime) - 0.5f) * 2f;
                pos += rot * new Vector3(sx, sy, 0f) * shake;
            }
            transform.SetPositionAndRotation(pos, rot);

            if (target != null)
            {
                Vector3 chest = target.position + Vector3.up * 1.1f;
                Shader.SetGlobalVector(SeeThroughId, new Vector4(chest.x, chest.y, chest.z, seeThroughRadius));
            }
        }
    }
}

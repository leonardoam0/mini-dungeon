using System.Collections.Generic;
using UnityEngine;

namespace Ruinas.EditorTools
{
    /// <summary>
    /// Poses-chave autorais. Convenções (graus, espaço local): braço X negativo = para frente/cima
    /// (−90 aponta à frente); braço direito Z+ = abre para fora; corpo X+ = inclina à frente; Y+ gira
    /// o tronco para a direita; tilt da raiz X+ = tomba à frente. Cada golpe tem preparação, contato e
    /// recuperação alinhados aos tempos do AttackDefinition correspondente.
    /// </summary>
    public static class PoseForge
    {
        static readonly Vector3 ArmLRest = new Vector3(0, 0, -4);
        static readonly Vector3 ArmRRest = new Vector3(0, 0, 4);

        static PoseKey K(float t, Vector3? body = null, Vector3? head = null, Vector3? armL = null, Vector3? armR = null,
            Vector3? legL = null, Vector3? legR = null, Vector3? off = null, Vector3? tilt = null, Ease ease = Ease.InOutQuad)
        {
            return new PoseKey
            {
                time = t,
                body = body ?? Vector3.zero,
                head = head ?? Vector3.zero,
                armL = armL ?? ArmLRest,
                armR = armR ?? ArmRRest,
                legL = legL ?? Vector3.zero,
                legR = legR ?? Vector3.zero,
                rootOffset = off ?? Vector3.zero,
                rootTilt = tilt ?? Vector3.zero,
                ease = ease,
            };
        }

        static PoseClip C(string id, float duration, params PoseKey[] keys) => new PoseClip { id = id, duration = duration, keys = keys, blendIn = 0.05f, blendOut = 0.12f };

        static PoseClip Upper(PoseClip c)
        {
            c.upperBodyOnly = true;
            return c;
        }

        public static PoseClip[] All()
        {
            var list = new List<PoseClip>();
            var V = new System.Func<float, float, float, Vector3>((x, y, z) => new Vector3(x, y, z));

            // ---------------- Espada (golpes de 0,45 / 0,45 / 0,60 s; contato em 0,16 / 0,16 / 0,24 s)
            list.Add(C("sword_1", 0.45f,
                K(0f),
                K(0.13f, body: V(4, 28, 0), armR: V(-115, 0, 48), armL: V(-25, 0, -12), legL: V(-10, 0, 0), legR: V(8, 0, 0), ease: Ease.OutQuad),
                K(0.2f, body: V(10, -22, 0), armR: V(-82, 0, -28), armL: V(-15, 0, -20), legL: V(-18, 0, 0), legR: V(12, 0, 0), ease: Ease.InQuad),
                K(0.3f, body: V(8, -32, 0), armR: V(-62, 0, -44), armL: V(-8, 0, -18), legL: V(-15, 0, 0), legR: V(10, 0, 0), ease: Ease.OutQuad),
                K(0.45f, ease: Ease.InOutQuad)));
            list.Add(C("sword_2", 0.45f,
                K(0f, body: V(6, -25, 0), armR: V(-70, 0, -40)),
                K(0.13f, body: V(4, -34, 0), armR: V(-105, 0, -52), armL: V(-20, 0, -10), legL: V(8, 0, 0), legR: V(-12, 0, 0), ease: Ease.OutQuad),
                K(0.2f, body: V(10, 22, 0), armR: V(-84, 0, 32), armL: V(-12, 0, -24), legL: V(10, 0, 0), legR: V(-18, 0, 0), ease: Ease.InQuad),
                K(0.3f, body: V(8, 30, 0), armR: V(-64, 0, 46), armL: V(-6, 0, -20), ease: Ease.OutQuad),
                K(0.45f)));
            list.Add(C("sword_3", 0.6f,
                K(0f),
                K(0.2f, body: V(-12, 0, 0), head: V(-8, 0, 0), armR: V(-172, 0, 6), armL: V(-150, 0, -8), legL: V(-14, 0, 0), legR: V(10, 0, 0), off: V(0, 0.04f, 0), ease: Ease.OutQuad),
                K(0.27f, body: V(22, 0, 0), head: V(10, 0, 0), armR: V(-58, 0, 2), armL: V(-50, 0, -4), legL: V(-26, 0, 0), legR: V(16, 0, 0), off: V(0, -0.06f, 0.06f), ease: Ease.InQuad),
                K(0.4f, body: V(18, 0, 0), armR: V(-48, 0, 4), armL: V(-40, 0, -6), legL: V(-24, 0, 0), legR: V(14, 0, 0), off: V(0, -0.05f, 0.05f)),
                K(0.6f)));

            // ---------------- Machado (pesado: contato em 0,30 s)
            list.Add(C("axe_1", 0.66f,
                K(0f),
                K(0.27f, body: V(-16, 0, 0), head: V(-10, 0, 0), armR: V(-178, 0, 4), armL: V(-172, 0, -4), legL: V(-12, 0, 0), legR: V(12, 0, 0), off: V(0, 0.06f, -0.04f), ease: Ease.OutQuad),
                K(0.34f, body: V(28, 0, 0), head: V(12, 0, 0), armR: V(-45, 0, 2), armL: V(-45, 0, -2), legL: V(-28, 0, 0), legR: V(18, 0, 0), off: V(0, -0.1f, 0.08f), tilt: V(5, 0, 0), ease: Ease.InQuad),
                K(0.5f, body: V(24, 0, 0), armR: V(-38, 0, 2), armL: V(-38, 0, -2), legL: V(-26, 0, 0), legR: V(16, 0, 0), off: V(0, -0.08f, 0.06f), tilt: V(4, 0, 0)),
                K(0.66f)));
            list.Add(C("axe_2", 0.72f,
                K(0f),
                K(0.26f, body: V(6, 52, 0), armR: V(-92, 0, 64), armL: V(-80, 0, 30), legL: V(-10, 0, 0), legR: V(14, 0, 0), ease: Ease.OutQuad),
                K(0.38f, body: V(10, -58, 0), armR: V(-84, 0, -52), armL: V(-70, 0, -40), legL: V(-18, 0, 0), legR: V(10, 0, 0), ease: Ease.InQuad),
                K(0.52f, body: V(8, -66, 0), armR: V(-70, 0, -58), armL: V(-60, 0, -46)),
                K(0.72f)));

            // ---------------- Lança (estocadas rápidas)
            PoseClip Thrust(string id, float dur, float contact, float reach)
            {
                return C(id, dur,
                    K(0f),
                    K(contact * 0.8f, body: V(-4, 18, 0), armR: V(-80, 0, 18), armL: V(-60, 0, -30), legL: V(8, 0, 0), legR: V(-8, 0, 0), off: V(0, 0, -0.08f), ease: Ease.OutQuad),
                    K(contact + 0.04f, body: V(14, -8, 0), armR: V(-96, 0, -4), armL: V(-86, 0, -8), legL: V(-22, 0, 0), legR: V(14, 0, 0), off: V(0, 0, reach), ease: Ease.InQuad),
                    K(dur * 0.75f, body: V(10, -6, 0), armR: V(-90, 0, 0), armL: V(-80, 0, -6), legL: V(-18, 0, 0), legR: V(12, 0, 0), off: V(0, 0, reach * 0.6f)),
                    K(dur));
            }
            list.Add(Thrust("spear_1", 0.36f, 0.12f, 0.12f));
            list.Add(Thrust("spear_2", 0.36f, 0.12f, 0.12f));
            list.Add(Thrust("spear_3", 0.5f, 0.18f, 0.22f));

            // ---------------- Socos (sem arma)
            list.Add(C("punch_1", 0.34f, K(0f), K(0.1f, body: V(0, 20, 0), armR: V(-60, 0, 20)), K(0.15f, body: V(8, -12, 0), armR: V(-92, 0, -6), ease: Ease.InQuad), K(0.34f)));
            list.Add(C("punch_2", 0.34f, K(0f), K(0.1f, body: V(0, -20, 0), armL: V(-60, 0, -20)), K(0.15f, body: V(8, 12, 0), armL: V(-92, 0, 6), ease: Ease.InQuad), K(0.34f)));

            // ---------------- Arco
            list.Add(Upper(C("bow_draw", 0.3f,
                K(0f),
                K(0.18f, body: V(0, -12, 0), head: V(0, 10, 0), armL: V(-88, 12, 0), armR: V(-84, -18, -8), ease: Ease.OutQuad),
                K(0.3f, body: V(0, -14, 0), head: V(0, 12, 0), armL: V(-90, 12, 0), armR: V(-86, -22, -12)))));
            list.Add(Upper(C("bow_release", 0.22f,
                K(0f, body: V(0, -14, 0), head: V(0, 12, 0), armL: V(-90, 12, 0), armR: V(-86, -22, -12)),
                K(0.05f, body: V(-4, -10, 0), armL: V(-94, 12, 0), armR: V(-70, 0, 24), ease: Ease.OutQuad),
                K(0.22f))));

            // ---------------- Esquiva (rolamento com compensação do centro de giro)
            {
                var keys = new List<PoseKey>();
                const float c = 0.85f;
                int steps = 8;
                for (int i = 0; i <= steps; i++)
                {
                    float u = i / (float)steps;
                    float th = u * 360f;
                    float rad = th * Mathf.Deg2Rad;
                    bool tucked = i > 0 && i < steps;
                    keys.Add(K(0.34f * u,
                        body: tucked ? V(40, 0, 0) : Vector3.zero,
                        head: tucked ? V(30, 0, 0) : Vector3.zero,
                        armL: tucked ? V(-70, 0, -10) : ArmLRest,
                        armR: tucked ? V(-70, 0, 10) : ArmRRest,
                        legL: tucked ? V(-80, 0, 0) : Vector3.zero,
                        legR: tucked ? V(-80, 0, 0) : Vector3.zero,
                        off: new Vector3(0f, c - c * Mathf.Cos(rad), -c * Mathf.Sin(rad)),
                        tilt: new Vector3(th, 0f, 0f),
                        ease: Ease.Linear));
                }
                list.Add(new PoseClip { id = "roll", duration = 0.34f, keys = keys.ToArray(), blendIn = 0.03f, blendOut = 0.08f });
            }

            // ---------------- Salto da pena
            list.Add(C("leap", 0.5f,
                K(0f),
                K(0.07f, body: V(20, 0, 0), armL: V(20, 0, -10), armR: V(20, 0, 10), legL: V(-30, 0, 0), legR: V(-30, 0, 0), off: V(0, -0.12f, 0), ease: Ease.OutQuad),
                K(0.18f, body: V(-6, 0, 0), head: V(-10, 0, 0), armL: V(-165, 0, -25), armR: V(-165, 0, 25), legL: V(12, 0, 0), legR: V(-20, 0, 0), ease: Ease.OutQuad),
                K(0.34f, body: V(12, 0, 0), armL: V(-120, 0, -40), armR: V(-120, 0, 40), legL: V(-50, 0, 0), legR: V(-40, 0, 0)),
                K(0.44f, body: V(24, 0, 0), armL: V(-40, 0, -50), armR: V(-40, 0, 50), legL: V(-35, 0, 0), legR: V(20, 0, 0), off: V(0, -0.14f, 0), ease: Ease.InQuad),
                K(0.5f, ease: Ease.OutQuad)));

            // ---------------- Conjuração (pulso magenta, invocação)
            list.Add(C("cast", 0.4f,
                K(0f),
                K(0.13f, body: V(-10, 0, 0), head: V(-12, 0, 0), armL: V(-150, 0, -24), armR: V(-150, 0, 24), ease: Ease.OutQuad),
                K(0.2f, body: V(14, 0, 0), head: V(6, 0, 0), armL: V(-80, 0, -70), armR: V(-80, 0, 70), legL: V(-10, 0, 0), legR: V(10, 0, 0), ease: Ease.OutBack),
                K(0.3f, body: V(10, 0, 0), armL: V(-70, 0, -64), armR: V(-70, 0, 64)),
                K(0.4f)));

            // ---------------- Poção e reações
            list.Add(Upper(C("drink", 0.45f,
                K(0f),
                K(0.15f, head: V(-22, 0, 0), armR: V(-150, 0, -22), ease: Ease.OutQuad),
                K(0.33f, head: V(-26, 0, 0), armR: V(-155, 0, -24)),
                K(0.45f))));
            list.Add(Upper(C("hit_light", 0.2f, K(0f), K(0.05f, body: V(-10, 0, 0), head: V(-12, 0, 0), armL: V(-20, 0, -18), armR: V(-20, 0, 18), ease: Ease.OutQuad), K(0.2f))));
            list.Add(C("hit_heavy", 0.32f, K(0f), K(0.06f, body: V(-14, 0, 0), head: V(-18, 0, 0), armL: V(-30, 0, -40), armR: V(-30, 0, 40), tilt: V(-12, 0, 0), ease: Ease.OutQuad), K(0.32f)));
            list.Add(C("flinch", 0.36f, K(0f), K(0.06f, body: V(-16, 8, 0), head: V(-20, 0, 0), armL: V(-40, 0, -30), armR: V(-40, 0, 30), tilt: V(-10, 0, 0), ease: Ease.OutQuad), K(0.36f)));
            var death = C("death", 0.6f,
                K(0f),
                K(0.18f, body: V(-10, 0, 0), head: V(-20, 0, 0), armL: V(-60, 0, -40), armR: V(-60, 0, 40), tilt: V(-20, 0, 0), ease: Ease.OutQuad),
                K(0.6f, body: V(0, 0, 0), head: V(-10, 0, 0), armL: V(-150, 0, -30), armR: V(-150, 0, 30), legL: V(-10, 0, 0), legR: V(8, 0, 0), off: V(0, 0.12f, 0), tilt: V(-86, 0, 0), ease: Ease.InQuad));
            death.blendOut = 0.3f;
            list.Add(death);
            list.Add(C("stunned", 0.9f,
                K(0f, head: V(10, -14, 0), body: V(4, 0, 0)),
                K(0.45f, head: V(10, 14, 0), body: V(4, 0, 0)),
                K(0.9f, head: V(10, -14, 0), body: V(4, 0, 0))));
            list[list.Count - 1].loop = true;

            // ---------------- Surgimento
            list.Add(C("spawn", 0.7f, K(0f, off: V(0, -1.9f, 0), armL: V(-150, 0, -20), armR: V(-150, 0, 20)), K(0.55f, off: V(0, 0.05f, 0), armL: V(-120, 0, -20), armR: V(-120, 0, 20), ease: Ease.OutQuad), K(0.7f, ease: Ease.InOutQuad)));
            var burrowed = C("burrowed", 0.1f, K(0f, off: V(0, -2.4f, 0)), K(0.1f, off: V(0, -2.4f, 0)));
            burrowed.blendIn = 0.001f;
            list.Add(burrowed);
            list.Add(C("emerge", 0.8f, K(0f, off: V(0, -2.4f, 0)), K(0.8f, ease: Ease.OutBack)));

            // ---------------- Inimigos
            list.Add(C("zombie_attack", 1.0f,
                K(0f),
                K(0.5f, body: V(-10, 0, 0), head: V(-6, 0, 0), armL: V(-160, 0, -10), armR: V(-160, 0, 10), legL: V(-10, 0, 0), legR: V(10, 0, 0), ease: Ease.OutQuad),
                K(0.6f, body: V(20, 0, 0), head: V(10, 0, 0), armL: V(-62, 0, -6), armR: V(-62, 0, 6), legL: V(-24, 0, 0), legR: V(16, 0, 0), off: V(0, -0.04f, 0.1f), ease: Ease.InQuad),
                K(0.78f, body: V(16, 0, 0), armL: V(-55, 0, -6), armR: V(-55, 0, 6), legL: V(-20, 0, 0), legR: V(14, 0, 0), off: V(0, -0.03f, 0.08f)),
                K(1.0f)));
            list.Add(C("brute_swipe", 1.1f,
                K(0f),
                K(0.5f, body: V(4, 42, 0), armR: V(-130, 0, 74), armL: V(-40, 0, -20), ease: Ease.OutQuad),
                K(0.62f, body: V(12, -40, 0), armR: V(-80, 0, -40), armL: V(-30, 0, -30), ease: Ease.InQuad),
                K(0.85f, body: V(10, -44, 0), armR: V(-66, 0, -46)),
                K(1.1f)));
            list.Add(C("brute_slam", 1.7f,
                K(0f),
                K(1.05f, body: V(-18, 0, 0), head: V(-14, 0, 0), armL: V(-178, 0, -8), armR: V(-178, 0, 8), legL: V(-8, 0, 0), legR: V(8, 0, 0), off: V(0, 0.12f, 0), ease: Ease.OutQuad),
                K(1.18f, body: V(34, 0, 0), head: V(14, 0, 0), armL: V(-36, 0, -4), armR: V(-36, 0, 4), legL: V(-30, 0, 0), legR: V(22, 0, 0), off: V(0, -0.2f, 0.1f), tilt: V(8, 0, 0), ease: Ease.InQuad),
                K(1.45f, body: V(30, 0, 0), armL: V(-34, 0, -4), armR: V(-34, 0, 4), legL: V(-28, 0, 0), legR: V(20, 0, 0), off: V(0, -0.18f, 0.08f), tilt: V(6, 0, 0)),
                K(1.7f)));
            // Vinha: "body" é a base do caule, "head" o bulbo.
            list.Add(C("spit_windup", 1.0f, K(0f), K(0.8f, body: V(-18, 0, 0), head: V(-24, 0, 0), ease: Ease.OutQuad), K(1.0f, body: V(-20, 0, 0), head: V(-28, 0, 0))));
            list.Add(C("spit", 0.4f, K(0f, body: V(-20, 0, 0), head: V(-28, 0, 0)), K(0.08f, body: V(22, 0, 0), head: V(30, 0, 0), ease: Ease.InQuad), K(0.4f, ease: Ease.OutQuad)));
            return list.ToArray();
        }
    }
}

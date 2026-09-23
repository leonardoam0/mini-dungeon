using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace Ruinas
{
    /// <summary>
    /// Piloto automático de validação (-autoplay): joga a missão pelos MESMOS comandos do jogador — segue a
    /// rota, luta, usa artefatos e poção, esquiva de avisos, abre baús e sai pelo portal. Registra cada etapa
    /// no log. Não substitui um teste humano de sensação; comprova que o percurso é completável.
    /// </summary>
    public class AutoPilot : IPlayerCommandSource
    {
        readonly MissionDirector mission;
        readonly LevelContext ctx;
        readonly AutoPilotRoute route;
        int stepIndex;
        float stepTime;
        readonly NavMeshPath path = new NavMeshPath();
        readonly Vector3[] corners = new Vector3[64];
        int cornerCount, cornerIndex;
        float repathTimer;
        Vector3 pathGoal;
        float stuckTimer;
        Vector3 stuckRef;
        int swing;
        float totalTime;
        float statusTimer;
        readonly List<Actor> scratch = new List<Actor>();
        // Alvos inalcançáveis (item caído fora da malha, inimigo sem caminho) são ignorados por um tempo.
        readonly Dictionary<Object, float> ignoredUntil = new Dictionary<Object, float>();
        Object chaseTarget;
        float chaseTime, chaseBestDist;
        const float StepTimeout = 150f;
        public int FailedSteps { get; private set; }

        public bool Active => stepIndex < (route != null && route.steps != null ? route.steps.Length : 0) || HasEnemiesNear();

        public AutoPilot(MissionDirector director, LevelContext context)
        {
            mission = director;
            ctx = context;
            route = Object.FindAnyObjectByType<AutoPilotRoute>();
            Debug.Log($"[AutoPilot] iniciado; etapas={(route != null && route.steps != null ? route.steps.Length : 0)}");
        }

        bool HasEnemiesNear()
        {
            var p = ctx.Player;
            return p != null && ctx.Actors.NearestHostile(p.Actor, p.transform.position, 8f) != null;
        }

        public void Fill(ref PlayerCommands c, PlayerController p)
        {
            c.gamepad = true;
            c.moveIsWorld = true;
            if (Services.State == null || !Services.State.AcceptsGameplayInput || p.IsDefeated) return;
            float dt = Time.deltaTime;
            totalTime += dt;
            stepTime += dt;
            statusTimer += dt;
            if (statusTimer >= 20f)
            {
                statusTimer = 0f;
                var st = route != null && route.steps != null && stepIndex < route.steps.Length ? route.steps[stepIndex].label : "fim";
                Debug.Log($"[AutoPilot] estado: etapa '{st}' há {stepTime:0}s, posição {p.transform.position:F1}, alvo {(chaseTarget != null ? chaseTarget.name : "-")}, inimigos a 11 m: {ctx.Actors.CountAlive(Team.Enemy)} vivos na cena");
            }
            if (route != null && route.steps != null && stepIndex < route.steps.Length && stepTime > StepTimeout)
            {
                FailedSteps++;
                Debug.LogWarning($"[AutoPilot] FALHA: etapa {stepIndex + 1} '{route.steps[stepIndex].label}' não concluída em {StepTimeout:0}s (posição {p.transform.position:F1}); seguindo para a próxima.");
                NextStep();
            }

            var recv = p.Actor.Receiver;
            if (recv.Fraction < 0.45f && p.Actions.PotionCooldown <= 0f) c.potionPressed = true;

            // Esquiva de avisos no chão sob o jogador.
            if (DangerUnderfoot(p, out Vector3 away))
            {
                c.dodgePressed = true;
                c.moveWorld = away;
                return;
            }

            if (Fight(ref c, p)) return;
            FollowRoute(ref c, p);
        }

        bool DangerUnderfoot(PlayerController p, out Vector3 away)
        {
            away = Vector3.zero;
            foreach (var a in ctx.Areas.Active)
            {
                if (a == null || !a.DamageActive || a.Team == Team.Player) continue;
                Vector3 d = p.transform.position - a.transform.position;
                d.y = 0f;
                if (d.magnitude < a.Radius + 0.3f)
                {
                    away = d.sqrMagnitude > 0.01f ? d.normalized : -p.transform.forward;
                    return true;
                }
            }
            return false;
        }

        bool Fight(ref PlayerCommands c, PlayerController p)
        {
            Vector3 pos = p.transform.position;
            var enemy = ctx.Actors.NearestHostile(p.Actor, pos, 11f,
                a => !IsIgnored(a) && !Physics.Linecast(p.Actor.Center, a.Center, Layers.EnvironmentMask));
            if (enemy == null) return false;
            // Só conta como perseguição sem progresso quando o inimigo está fora do alcance do golpe.
            float flatDist = Flat(enemy.transform.position - pos) - enemy.radius;
            if (flatDist > p.CurrentMeleeRange() * 0.85f) { if (Chase(enemy, flatDist)) return false; }
            else { chaseTarget = enemy; chaseTime = 0f; chaseBestDist = flatDist; }

            Vector3 to = enemy.transform.position - pos;
            to.y = 0f;
            float dist = to.magnitude - enemy.radius;
            c.hasAimPoint = true;
            c.aimPoint = enemy.transform.position;

            ctx.Actors.HostilesInRadius(p.Actor, pos, 4.5f, scratch);
            var arts = p.Artifacts;
            if (scratch.Count >= 2 && arts.Slots[1].def != null && arts.Slots[1].cooldown <= 0f) c.artifact2 = true;
            else if (arts.Slots[2].def != null && arts.Slots[2].cooldown <= 0f && !arts.IsActive(2)) c.artifact3 = true;
            else if (scratch.Count >= 3 && arts.Slots[0].def != null && arts.Slots[0].cooldown <= 0f) c.artifact1 = true;

            bool ranged = enemy.EnemyDef != null && (enemy.EnemyDef.archetype == EnemyArchetype.Archer || enemy.EnemyDef.archetype == EnemyArchetype.SporeVine);
            if (ranged && dist > 5f && p.Inventory.Arrows > 0)
            {
                c.rangedPressed = true;
                c.rangedHeld = true;
                return true;
            }

            float reach = p.CurrentMeleeRange() * 0.85f;
            if (dist > reach)
            {
                c.moveWorld = SteerTo(p, enemy.transform.position);
            }
            else
            {
                swing++;
                c.meleeHeld = true;
                c.meleePressed = swing % 2 == 0;
            }
            return true;
        }

        void FollowRoute(ref PlayerCommands c, PlayerController p)
        {
            if (route == null || route.steps == null || stepIndex >= route.steps.Length) return;
            var step = route.steps[stepIndex];
            Vector3 goal = step.point != null ? step.point.position : p.transform.position;

            // Coleta itens próximos antes de seguir.
            foreach (var pick in Object.FindObjectsByType<Pickup>())
            {
                if (pick == null || pick.Collected || pick.kind != PickupKind.Item || IsIgnored(pick)) continue;
                float d = Vector3.Distance(pick.transform.position, p.transform.position);
                if (d < 9f)
                {
                    if (Chase(pick, d)) continue;
                    goal = pick.transform.position;
                    break;
                }
            }

            switch (step.action)
            {
                case RouteAction.GoTo:
                    if (Flat(goal - p.transform.position) < 1.3f && goal == (step.point != null ? step.point.position : goal)) NextStep();
                    break;
                case RouteAction.OpenChest:
                    if (step.chest == null || step.chest.Opened) { NextStep(); return; }
                    if (step.chest.locked && stepTime > 30f) { NextStep(); return; }
                    goal = step.chest.transform.position;
                    if (Flat(goal - p.transform.position) < step.chest.radius * 0.8f)
                    {
                        c.interactPressed = !step.chest.locked;
                        return;
                    }
                    break;
                case RouteAction.ClearEncounter:
                    if (step.encounter == null || step.encounter.State == EncounterState.Cleared) { NextStep(); return; }
                    break;
                case RouteAction.Exit:
                    var exit = mission.exit;
                    if (exit != null && exit.active)
                    {
                        goal = exit.transform.position;
                        if (Flat(goal - p.transform.position) < exit.radius * 0.8f) { c.interactPressed = true; return; }
                    }
                    break;
            }
            c.moveWorld = SteerTo(p, goal);
        }

        bool IsIgnored(Object o) => o != null && ignoredUntil.TryGetValue(o, out float until) && totalTime < until;

        /// <summary>Acompanha a aproximação ao alvo; se não houver progresso em 8 s, passa a ignorá-lo por 20 s.</summary>
        bool Chase(Object target, float dist)
        {
            if (target != chaseTarget)
            {
                chaseTarget = target;
                chaseTime = 0f;
                chaseBestDist = dist;
                return false;
            }
            chaseTime += Time.deltaTime;
            if (dist < chaseBestDist - 0.5f) { chaseBestDist = dist; chaseTime = 0f; }
            if (chaseTime < 8f) return false;
            ignoredUntil[target] = totalTime + 20f;
            Debug.Log($"[AutoPilot] alvo inalcançável ignorado: {target.name} a {dist:0.0} m");
            chaseTarget = null;
            return true;
        }

        void NextStep()
        {
            var s = route.steps[stepIndex];
            Debug.Log($"[AutoPilot] etapa {stepIndex + 1}/{route.steps.Length} '{s.label}' concluída em {stepTime:0.0}s (total {totalTime:0.0}s, vidas {mission.Lives})");
            stepIndex++;
            stepTime = 0f;
            cornerCount = 0;
        }

        static float Flat(Vector3 v)
        {
            v.y = 0f;
            return v.magnitude;
        }

        Vector3 SteerTo(PlayerController p, Vector3 goal)
        {
            repathTimer -= Time.deltaTime;
            if (cornerCount == 0 || repathTimer <= 0f || (goal - pathGoal).sqrMagnitude > 1f)
            {
                repathTimer = 0.5f;
                pathGoal = goal;
                cornerCount = 0;
                if (NavMesh.SamplePosition(goal, out var g, 3f, NavMesh.AllAreas) &&
                    NavMesh.SamplePosition(p.transform.position, out var s, 2f, NavMesh.AllAreas) &&
                    NavMesh.CalculatePath(s.position, g.position, NavMesh.AllAreas, path))
                {
                    cornerCount = path.GetCornersNonAlloc(corners);
                    cornerIndex = cornerCount > 1 ? 1 : 0;
                }
            }

            stuckTimer += Time.deltaTime;
            if (stuckTimer > 2f)
            {
                if ((p.transform.position - stuckRef).magnitude < 0.3f)
                {
                    cornerCount = 0;
                    stuckTimer = 0f;
                    Vector2 r = Random.insideUnitCircle;
                    return new Vector3(r.x, 0f, r.y).normalized;
                }
                stuckTimer = 0f;
                stuckRef = p.transform.position;
            }

            while (cornerIndex < cornerCount)
            {
                Vector3 d = corners[cornerIndex] - p.transform.position;
                d.y = 0f;
                if (d.magnitude > 0.5f) return d.normalized;
                cornerIndex++;
            }
            Vector3 direct = goal - p.transform.position;
            direct.y = 0f;
            return direct.magnitude > 0.3f ? direct.normalized : Vector3.zero;
        }
    }
}

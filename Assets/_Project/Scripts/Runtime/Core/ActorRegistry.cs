using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ruinas
{
    /// <summary>Atores ativos da cena, para consultas de alvo sem varrer a hierarquia.</summary>
    public class ActorRegistry
    {
        readonly List<Actor> actors = new List<Actor>();
        public IReadOnlyList<Actor> All => actors;

        public void Register(Actor a)
        {
            if (a != null && !actors.Contains(a)) actors.Add(a);
        }

        public void Unregister(Actor a) => actors.Remove(a);

        public int CountAlive(Team team)
        {
            int n = 0;
            foreach (var a in actors) if (a != null && a.team == team && !a.IsDead) n++;
            return n;
        }

        public Actor NearestHostile(Actor from, Vector3 center, float maxDistance, Func<Actor, bool> filter = null)
        {
            Actor best = null;
            float bestD = maxDistance;
            foreach (var a in actors)
            {
                if (a == null || a.IsDead || !from.IsHostileTo(a)) continue;
                if (filter != null && !filter(a)) continue;
                Vector3 d = a.transform.position - center;
                if (Mathf.Abs(d.y) > 3f) continue;
                d.y = 0f;
                float dist = d.magnitude - a.radius;
                if (dist < bestD)
                {
                    bestD = dist;
                    best = a;
                }
            }
            return best;
        }

        public void HostilesInRadius(Actor from, Vector3 center, float radius, List<Actor> results, float maxDy = 2.5f)
        {
            results.Clear();
            foreach (var a in actors)
            {
                if (a == null || a.IsDead || !from.IsHostileTo(a)) continue;
                Vector3 d = a.transform.position - center;
                if (Mathf.Abs(d.y) > maxDy) continue;
                d.y = 0f;
                if (d.magnitude - a.radius * 0.5f <= radius) results.Add(a);
            }
        }
    }
}

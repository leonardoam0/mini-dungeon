using System.Collections.Generic;
using UnityEngine;

namespace Ruinas
{
    public enum MapPoiKind { Chest, Gate, Exit, Checkpoint, Merchant, Objective }

    /// <summary>Ponto de interesse do mapa sobreposto (aparece quando a célula já foi explorada).</summary>
    public class MapPoi : MonoBehaviour
    {
        public static readonly List<MapPoi> All = new List<MapPoi>();
        public MapPoiKind kind;
        public bool alwaysVisible;

        void OnEnable() => All.Add(this);
        void OnDisable() => All.Remove(this);

        public bool IsRelevant
        {
            get
            {
                switch (kind)
                {
                    case MapPoiKind.Chest:
                        var c = GetComponent<Chest>();
                        return c == null || !c.Opened;
                    case MapPoiKind.Exit:
                        var e = GetComponent<ExitPortal>();
                        return e == null || e.active;
                    default: return true;
                }
            }
        }
    }
}

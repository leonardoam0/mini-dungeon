using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Ruinas
{
    /// <summary>Encaminha a seleção (foco por controle ou mouse) para uma ação — ex.: mostrar detalhes do item.</summary>
    public class SelectForward : MonoBehaviour, ISelectHandler
    {
        public Action onSelect;
        public void OnSelect(BaseEventData eventData) => onSelect?.Invoke();
    }
}

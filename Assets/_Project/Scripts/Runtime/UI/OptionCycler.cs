using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Ruinas
{
    /// <summary>Opção de múltipla escolha navegável por controle (esquerda/direita) e por clique.</summary>
    public class OptionCycler : MonoBehaviour, IMoveHandler
    {
        public string label;
        public string[] options;
        public int index;
        public PixelText text;
        public Action<int> onChanged;

        public void Setup(string lbl, string[] opts, int current, PixelText target, Action<int> changed)
        {
            label = lbl;
            options = opts;
            index = Mathf.Clamp(current, 0, opts.Length - 1);
            text = target;
            onChanged = changed;
            var btn = GetComponent<Button>();
            if (btn != null) btn.onClick.AddListener(() => Step(1));
            Refresh();
        }

        public void Step(int dir)
        {
            if (options == null || options.Length == 0) return;
            index = (index + dir + options.Length) % options.Length;
            Refresh();
            onChanged?.Invoke(index);
        }

        void Refresh()
        {
            if (text != null) text.Text = $"{label}:  < {options[index]} >";
        }

        public void OnMove(AxisEventData eventData)
        {
            if (eventData.moveDir == MoveDirection.Left) { Step(-1); eventData.Use(); }
            else if (eventData.moveDir == MoveDirection.Right) { Step(1); eventData.Use(); }
        }
    }
}

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Ruinas
{
    /// <summary>
    /// Destaque de seleção compartilhado por mouse e controle: passar o mouse seleciona o elemento,
    /// então existe um único foco visível. Toca sons discretos de navegação.
    /// </summary>
    public class SelectHighlight : MonoBehaviour, ISelectHandler, IDeselectHandler, IPointerEnterHandler, ISubmitHandler, IPointerClickHandler
    {
        public Graphic highlight;
        public PixelText label;
        public Color labelNormal = new Color(0.86f, 0.84f, 0.78f);
        public Color labelSelected = Color.white;
        public bool playSounds = true;

        Selectable selectable;

        void Awake()
        {
            selectable = GetComponent<Selectable>();
            SetVisual(false);
        }

        void OnEnable()
        {
            bool selected = EventSystem.current != null && EventSystem.current.currentSelectedGameObject == gameObject;
            SetVisual(selected);
        }

        public void OnSelect(BaseEventData eventData)
        {
            SetVisual(true);
            if (playSounds) Services.Audio?.PlayUi("ui_move");
        }

        public void OnDeselect(BaseEventData eventData) => SetVisual(false);

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (selectable != null && !selectable.interactable) return;
            if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject != gameObject)
                EventSystem.current.SetSelectedGameObject(gameObject);
        }

        public void OnSubmit(BaseEventData eventData)
        {
            if (playSounds && (selectable == null || selectable.interactable)) Services.Audio?.PlayUi("ui_select");
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (playSounds && (selectable == null || selectable.interactable)) Services.Audio?.PlayUi("ui_select");
        }

        void SetVisual(bool on)
        {
            if (highlight != null) highlight.enabled = on;
            if (label != null) label.color = on ? labelSelected : labelNormal;
        }
    }
}

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace VeinVanguard
{
    public class UiControlFeedback : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler, ISubmitHandler
    {
        public Color surface, accent;
        public bool selected;
        Selectable button;
        OutlinedUiGraphic shape;
        bool hover, pressed;
        float submitUntil;
        void Awake() { button=GetComponent<Selectable>();shape=button.targetGraphic.GetComponent<OutlinedUiGraphic>(); }
        void Update()
        {
            if(!button)Awake();
            bool enabled=button.IsInteractable();
            bool down=pressed||Time.unscaledTime<submitUntil;
            bool focus=enabled && (hover || selected || (EventSystem.current && EventSystem.current.currentSelectedGameObject==gameObject));
            button.targetGraphic.color=enabled ? Color.Lerp(surface,Color.white,focus?.07f:0) : new Color(.15f,.20f,.27f,.98f);
            var edge=focus?Color.Lerp(accent,Color.white,.45f):enabled?accent:new Color(.36f,.45f,.55f);
            var offset=new Vector2(0,enabled&&down?-3:focus?1:0);
            var shadow=new Vector2(0,enabled&&down?-1:-4);
            if(shape.borderColor!=edge||shape.visualOffset!=offset||shape.shadowOffset!=shadow)
            {
                shape.borderColor=edge;shape.visualOffset=offset;shape.shadowOffset=shadow;
                button.targetGraphic.SetVerticesDirty();
            }
        }
        public void OnPointerEnter(PointerEventData data)=>hover=true;
        public void OnPointerExit(PointerEventData data){hover=false;pressed=false;}
        public void OnPointerDown(PointerEventData data)=>pressed=true;
        public void OnPointerUp(PointerEventData data)=>pressed=false;
        public void OnSubmit(BaseEventData data){if(button is Button&&button.IsInteractable())submitUntil=Time.unscaledTime+.14f;}
        void OnDisable(){hover=pressed=false;submitUntil=0;}
    }
}

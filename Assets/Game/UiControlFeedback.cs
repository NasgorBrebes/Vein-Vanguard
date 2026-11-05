using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace VeinVanguard
{
    public class UiControlFeedback : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler, ISubmitHandler
    {
        public Color surface, accent;
        public bool selected;
        public bool animateVisual;
        public Vector2 entranceOffset;
        public float entranceScale=1,focusScale=1;
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
            var offset=new Vector2(0,enabled&&down?-3:focus?1:0)+entranceOffset;
            float scale=entranceScale*(focus?focusScale:1);
            var shadow=new Vector2(0,enabled&&down?-1:-4);
            if(shape.borderColor!=edge||shape.visualOffset!=offset||shape.shadowOffset!=shadow||shape.visualScale!=scale)
            {
                shape.borderColor=edge;shape.visualOffset=offset;shape.shadowOffset=shadow;shape.visualScale=scale;
                button.targetGraphic.SetVerticesDirty();
            }
            if(animateVisual)
                for(int i=0;i<transform.childCount;i++)
                {
                    var child=transform.GetChild(i) as RectTransform;
                    if(child){child.anchoredPosition=entranceOffset;child.localScale=Vector3.one*scale;}
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

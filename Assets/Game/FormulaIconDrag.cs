using UnityEngine;
using UnityEngine.EventSystems;

namespace VeinVanguard
{
    public sealed class FormulaIconDrag : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public BattleManager owner;
        public FormulaIcon icon;
        bool dragging;
        UnityEngine.UI.ScrollRect scrolling;

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (!dragging && owner && eventData != null)
            {
                scrolling=owner.BeginAssemblyScroll(eventData,transform);
                if(scrolling)scrolling.OnBeginDrag(eventData);
                else dragging = owner.BeginFormulaDrag(icon, eventData);
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            if(scrolling&&eventData!=null)scrolling.OnDrag(eventData);
            else if (dragging && owner && eventData != null) owner.MoveFormulaDrag(eventData);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if(scrolling)
            {
                if(eventData!=null)scrolling.OnEndDrag(eventData);
                scrolling.StopMovement();scrolling=null;
                if(owner)owner.EndAssemblyScroll();
            }
            if (!dragging) return;
            dragging = false;
            if (owner) owner.EndFormulaDrag(eventData);
        }

        void OnDisable() => OnEndDrag(null);
    }
}

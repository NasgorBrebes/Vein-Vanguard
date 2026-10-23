using UnityEngine;
using UnityEngine.EventSystems;

namespace VeinVanguard
{
    public sealed class FormulaIconDrag : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public BattleManager owner;
        public FormulaIcon icon;
        bool dragging;

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (!dragging && owner && eventData != null)
                dragging = owner.BeginFormulaDrag(icon, eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (dragging && owner && eventData != null) owner.MoveFormulaDrag(eventData);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!dragging) return;
            dragging = false;
            if (owner) owner.EndFormulaDrag(eventData);
        }

        void OnDisable() => OnEndDrag(null);
    }
}

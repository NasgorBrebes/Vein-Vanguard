using UnityEngine;
using UnityEngine.EventSystems;

namespace VeinVanguard
{
    public sealed class FormulaSocketDrop : MonoBehaviour, IDropHandler
    {
        public BattleManager owner;
        public int socket;

        public void OnDrop(PointerEventData eventData)
        {
            if (!owner || owner.AssemblyScrolling || eventData == null || !eventData.pointerDrag) return;
            var source = eventData.pointerDrag.GetComponent<FormulaIconDrag>();
            if (source && source.owner == owner) owner.InstallFormulaIcon(source.icon, socket);
        }
    }
}

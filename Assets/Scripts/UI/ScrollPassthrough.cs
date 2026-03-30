using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace MergeGame.UI
{
    /// <summary>
    /// Attach to interactive elements inside a ScrollRect to allow drag-to-scroll
    /// while preserving tap/click behavior.
    /// Passes drag events up to the parent ScrollRect.
    /// </summary>
    public class ScrollPassthrough : MonoBehaviour,
        IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private ScrollRect parentScrollRect;
        private bool resolved;

        private ScrollRect GetScrollRect()
        {
            if (!resolved)
            {
                parentScrollRect = GetComponentInParent<ScrollRect>();
                resolved = true;
            }
            return parentScrollRect;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            var sr = GetScrollRect();
            if (sr != null) sr.OnBeginDrag(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            var sr = GetScrollRect();
            if (sr != null) sr.OnDrag(eventData);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            var sr = GetScrollRect();
            if (sr != null) sr.OnEndDrag(eventData);
        }
    }
}

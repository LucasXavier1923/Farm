using UnityEngine;
using UnityEngine.EventSystems;

namespace FarmPrototype.Farming
{
    public sealed class FarmInventorySlotView : MonoBehaviour,
        IBeginDragHandler, IDragHandler, IEndDragHandler,
        IPointerEnterHandler, IPointerExitHandler, IPointerMoveHandler
    {
        private FarmHudController hud;
        private string itemId;
        private FarmItemQuality quality;

        public void Initialize(FarmHudController owner, string id, FarmItemQuality itemQuality = FarmItemQuality.Normal)
        {
            hud = owner;
            itemId = id;
            quality = FarmItemQualityRules.Clamp(itemQuality);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (string.IsNullOrEmpty(itemId)) return;
            hud?.HideItemTooltip();
            hud?.BeginItemDrag(itemId, quality, eventData.position);
        }

        public void OnDrag(PointerEventData eventData) => hud?.UpdateItemDrag(eventData.position);
        public void OnEndDrag(PointerEventData eventData) => hud?.EndItemDrag();

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!string.IsNullOrWhiteSpace(itemId))
                hud?.ShowItemTooltip(itemId, quality, eventData.position, FarmLocalization.Get("storage.backpack", "BACKPACK"));
        }

        public void OnPointerMove(PointerEventData eventData)
        {
            if (!string.IsNullOrWhiteSpace(itemId))
                hud?.MoveItemTooltip(eventData.position);
        }

        public void OnPointerExit(PointerEventData eventData) => hud?.HideItemTooltip();
    }

    public sealed class FarmHotbarSlotView : MonoBehaviour,
        IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler,
        IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
    {
        private FarmHudController hud;
        private int slotIndex;

        public void Initialize(FarmHudController owner, int index)
        {
            hud = owner;
            slotIndex = index;
        }

        public void OnPointerEnter(PointerEventData eventData) =>
            hud?.SetHotbarDropTarget(slotIndex, true);

        public void OnPointerExit(PointerEventData eventData) =>
            hud?.SetHotbarDropTarget(slotIndex, false);

        public void OnDrop(PointerEventData eventData)
        {
            hud?.SetHotbarDropTarget(slotIndex, true);
            hud?.CompleteItemDrag(slotIndex);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left) hud?.BeginHotbarDrag(slotIndex, eventData.position);
        }

        public void OnDrag(PointerEventData eventData) => hud?.UpdateItemDrag(eventData.position);
        public void OnEndDrag(PointerEventData eventData) => hud?.EndItemDrag();

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Right) hud?.ClearHotbarSlot(slotIndex);
            else if (eventData.button == PointerEventData.InputButton.Left) hud?.SelectHotbarSlot(slotIndex);
        }
    }
}

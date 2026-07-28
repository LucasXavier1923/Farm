using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace FarmPrototype.Farming
{
    public sealed class FarmStorageSlotView : MonoBehaviour,
        IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler,
        IPointerEnterHandler, IPointerExitHandler, IPointerMoveHandler
    {
        private FarmHudController hud;
        private bool fromBackpack;
        private string itemId;
        private FarmItemQuality quality;

        public void Initialize(FarmHudController owner, bool sourceIsBackpack, string id, FarmItemQuality itemQuality = FarmItemQuality.Normal)
        {
            hud = owner;
            fromBackpack = sourceIsBackpack;
            itemId = id;
            quality = FarmItemQualityRules.Clamp(itemQuality);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (hud == null || string.IsNullOrWhiteSpace(itemId)) return;
            if (eventData.button == PointerEventData.InputButton.Right)
            {
                Transfer(1);
                return;
            }

            if (eventData.button != PointerEventData.InputButton.Left) return;
            if (Keyboard.current != null && Keyboard.current.shiftKey.isPressed)
                hud.TransferHalf(itemId, quality, fromBackpack);
            else Transfer(int.MaxValue);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left || string.IsNullOrWhiteSpace(itemId)) return;
            hud?.HideItemTooltip();
            hud?.BeginStorageItemDrag(itemId, quality, !fromBackpack, eventData.position);
        }

        public void OnDrag(PointerEventData eventData) => hud?.UpdateItemDrag(eventData.position);
        public void OnEndDrag(PointerEventData eventData) => hud?.EndItemDrag();

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!string.IsNullOrWhiteSpace(itemId))
                hud?.ShowItemTooltip(itemId, eventData.position, fromBackpack
                    ? FarmLocalization.Get("storage.backpack", "BACKPACK")
                    : FarmLocalization.Get("storage.chest", "STORAGE"));
        }

        public void OnPointerMove(PointerEventData eventData)
        {
            if (!string.IsNullOrWhiteSpace(itemId))
                hud?.MoveItemTooltip(eventData.position);
        }

        public void OnPointerExit(PointerEventData eventData) => hud?.HideItemTooltip();

        private void Transfer(int amount)
        {
            if (fromBackpack) hud.TransferToStorage(itemId, quality, amount);
            else hud.TransferFromStorage(itemId, quality, amount);
        }
    }
}

using Vintagestory.API.Common;

namespace VSHUD
{
    class SlotIDTransfer
    {
        public ItemSlot slot;
        public int fromId;
        public int toId;

        public SlotIDTransfer(ItemSlot slot, int fromId)
        {
            this.slot = slot;
            this.fromId = fromId;
            this.toId = fromId;
        }

        public SlotIDTransfer(ItemSlot slot, int fromId, int toId)
        {
            this.slot = slot;
            this.fromId = fromId;
            this.toId = toId;
        }
    }
}

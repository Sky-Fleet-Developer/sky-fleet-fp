namespace Core.Character.Stuff
{
    public interface ISlotsGridListener
    {
        void SlotFilled(SlotCell slot);
        void SlotReplaced(SlotCell slot);
        void SlotEmptied(SlotCell slot);
    }
}
namespace Core.Trading
{
    public interface ITradeParticipant
    {
        public int GetCost(ItemSign itemSign);
    }
}
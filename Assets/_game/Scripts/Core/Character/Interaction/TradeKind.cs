using System;

namespace Core.Character.Interaction
{
    [Flags]
    public enum TradeKind
    {
        Sell = 1, Buyout = 2
    }

    public static class TradeKindExtensions
    {
        public static string GetAddButtonLocalizationKey(this TradeKind kind)
        {
            return $"trade-kind_{kind.ToString().ToLower()}_add-button";
        }
    }
}
using System.Collections.Generic;
using Zenject;

namespace Core.Trading
{
    public interface IInventoryOwner
    {
        string InventoryKey { get; }
    }
}
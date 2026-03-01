using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace Core.Items
{
    public interface IItemObjectFactory
    {
        void Deconstruct(IItemObject itemObject);
        Task<List<IItemObject>> Create(ItemInstance item);
        Task<IItemObject> CreateSingle(ItemInstance item);
        void SetupInstance(IItemObjectHandle itemObjectHandle, ItemInstance item);
    }
}
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Zenject;

namespace Core.Items
{
    public interface IItemObjectFactory
    {
        void Deconstruct(IItemObject itemObject);
        Task<List<IItemObject>> Create(ItemInstance item, DiContainer overrideDiContainer = null);
        Task<IItemObject> CreateSingle(ItemInstance item, DiContainer overrideDiContainer = null);
        void SetupInstance(IItemObjectHandle itemObjectHandle, ItemInstance item, DiContainer overrideDiContainer = null);
    }
}
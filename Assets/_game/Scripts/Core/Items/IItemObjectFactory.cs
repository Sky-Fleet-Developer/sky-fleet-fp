using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace Core.Items
{
    public interface IItemObjectFactory
    {
        void Deconstruct(IItemObjectHandle itemObject);
        Task<List<GameObject>> Create(ItemInstance item);
    }
}
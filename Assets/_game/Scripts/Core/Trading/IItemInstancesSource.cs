using System.Collections;
using System.Collections.Generic;
using Core.Items;

namespace Core.Trading
{
    public interface IItemInstancesSource : IItemInstancesSourceReadonly, IPullPutItem
    {
    }
}
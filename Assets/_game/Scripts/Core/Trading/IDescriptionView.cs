using UnityEngine;

namespace Core.Trading
{
    public interface IDescriptionView
    {
        string NameToView { get;}
        string IconKey { get;}
    }
}
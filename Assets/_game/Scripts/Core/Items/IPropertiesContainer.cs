using System.Collections.Generic;
using Core.Misc;

namespace Core.Items
{
    public interface IPropertiesContainer
    {
        IReadOnlyList<Property> Properties { get; }
        public bool TryGetProperty(string propertyName, out Property property);
    }
}
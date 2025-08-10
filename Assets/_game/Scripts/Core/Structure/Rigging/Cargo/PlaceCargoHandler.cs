using System;

namespace Core.Structure.Rigging.Cargo
{
    public class PlaceCargoHandler
    {
        public Action PlaceAction;
        public static readonly PlaceCargoHandler Empty = new PlaceCargoHandler{PlaceAction = null};
    }
}
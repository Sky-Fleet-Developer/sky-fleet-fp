using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Sirenix.OdinInspector;
using Sirenix.Utilities;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ContentSerializer
{
    public class Deserializer : ISerializationContext
    {
        public Action<Object> DetectedObjectReport => throw new NotImplementedException();
        public Func<int, Object> GetObject { get; set; }
        public SerializerBehaviour Behaviour { get; }

        public Deserializer(SerializerBehaviour behaviour)
        {
            Behaviour = behaviour;
            Behaviour.Context = this;
        }
    }
}
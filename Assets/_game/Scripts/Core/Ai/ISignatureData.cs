using System.Collections.Generic;

namespace Core.Ai
{
    public interface ISignatureData : ITargetData
    {
        public string SignatureId { get; }
    }
}
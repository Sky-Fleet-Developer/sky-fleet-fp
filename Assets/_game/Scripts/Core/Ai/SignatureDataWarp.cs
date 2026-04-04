namespace Core.Ai
{
    public struct SignatureDataWarp
    {
        public float SqrDistance;
        public ISignatureData Data;

        public SignatureDataWarp(ISignatureData data, float sqrDistance)
        {
            Data = data; SqrDistance = sqrDistance;
        }
    }
}
using System;
using Paterns.AbstractFactory;
using UnityEngine;

namespace Core.ContentSerializer.CustumSerializers
{
    public class FactoryArrayConventer : AbstractFactory<Array, byte[]>
    {
        public FactoryArrayConventer()
        {
            RegisterNewType(new ConvertVector3Array());
            RegisterNewType(new ConvertIntArray());
            RegisterNewType(new ConvertVector2Array());
        }

        protected override byte[] GetDefault()
        {
            return null;
        }
    }


    public abstract class GeneratorArrayFactory<T> : Generator<Array, byte[]>
    {
        public override bool CheckDefine(Array define)
        {
            return define.GetType() == typeof(T);
        }
    }

    public class ConvertVector3Array : GeneratorArrayFactory<Vector3[]>
    {
        public override byte[] Generate(Array define)
        {
            Vector3[] arr = (Vector3[])define;
            byte[] vectorsBuffer = new byte[arr.Length * sizeof(float) * 3];
            for (int i = 0; i < arr.Length; i++)
            {
                byte[] x = BitConverter.GetBytes(arr[i].x);
                byte[] y = BitConverter.GetBytes(arr[i].y);
                byte[] z = BitConverter.GetBytes(arr[i].z);
                Array.Copy(x, 0, vectorsBuffer, i * sizeof(float) * 3, sizeof(float));
                Array.Copy(y, 0, vectorsBuffer, i * sizeof(float) * 3 + sizeof(float), sizeof(float));
                Array.Copy(z, 0, vectorsBuffer, i * sizeof(float) * 3 + sizeof(float) * 2, sizeof(float));
            }
            return vectorsBuffer;
        }
    }


    public class ConvertIntArray : GeneratorArrayFactory<int[]>
    {
        public override byte[] Generate(Array define)
        {
            int[] arr = (int[])define;
            byte[] intBuffer = new byte[arr.Length * sizeof(int)];
            for (int i = 0; i < arr.Length; i++)
            {
                Array.Copy(BitConverter.GetBytes(arr[i]), 0, intBuffer, i * sizeof(int), sizeof(int));
            }
            return intBuffer;
        }
    }

    public class ConvertVector2Array : GeneratorArrayFactory<Vector2[]>
    {
        public override byte[] Generate(Array define)
        {
            Vector2[] arr = (Vector2[])define;
            byte[] vectorsBuffer = new byte[arr.Length * sizeof(float) * 2];
            for (int i = 0; i < arr.Length; i++)
            {
                byte[] x = BitConverter.GetBytes(arr[i].x);
                byte[] y = BitConverter.GetBytes(arr[i].y);
                Array.Copy(x, 0, vectorsBuffer, i * sizeof(float) * 2, sizeof(float));
                Array.Copy(y, 0, vectorsBuffer, i * sizeof(float) * 2 + sizeof(float), sizeof(float));
            }
            return vectorsBuffer;
        }
    }
}

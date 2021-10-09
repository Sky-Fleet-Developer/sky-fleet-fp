using System;
using Paterns.AbstractFactory;
using UnityEngine;

namespace Core.ContentSerializer.CustomSerializers
{
    public struct TypeDeconvert
    {
        public Type type;
        public byte[] buffer;

        public TypeDeconvert(Type type, byte[] buffer)
        {
            this.type = type;
            this.buffer = buffer;
        }
    }

    public class FactoryArrayDeconvert : AbstractFactory<TypeDeconvert, Array>
    {
        public FactoryArrayDeconvert()
        {
            RegisterNewType(new Vector3ArrayDeconvert());
            RegisterNewType(new Vector2ArrayDeconvert());
            RegisterNewType(new IntArrayDeconvert());
        }

        protected override Array GetDefault()
        {
            return null;
        }
    }

    public abstract class GeneratorDeconvertArrayFactory<T> : Generator<TypeDeconvert, Array>
    {
        public override bool CheckDefine(TypeDeconvert define)
        {
            return define.type == typeof(T);
        }
    }


    public class Vector3ArrayDeconvert : GeneratorDeconvertArrayFactory<Vector3[]>
    {
        public override Array Generate(TypeDeconvert define)
        {
            byte[] arr = define.buffer;
            Vector3[] vectors = new Vector3[arr.Length / sizeof(float) / 3];
            for (int i = 0; i < vectors.Length; i++)
            {
                vectors[i] = new Vector3(BitConverter.ToSingle(arr, i * sizeof(float) * 3), 
                    BitConverter.ToSingle(arr, i * sizeof(float) * 3 + sizeof(float)), 
                    BitConverter.ToSingle(arr, i * sizeof(float) * 3 + sizeof(float) * 2));
            }
            return vectors;
        }
    }

    public class Vector2ArrayDeconvert : GeneratorDeconvertArrayFactory<Vector2[]>
    {
        public override Array Generate(TypeDeconvert define)
        {
            byte[] arr = define.buffer;
            Vector2[] vectors = new Vector2[arr.Length / sizeof(float) / 2];
            for (int i = 0; i < vectors.Length; i++)
            {
                vectors[i] = new Vector2(BitConverter.ToSingle(arr, i * sizeof(float) * 2),
                    BitConverter.ToSingle(arr, i * sizeof(float) * 2 + sizeof(float)));
            }
            return vectors;
        }
    }

    public class IntArrayDeconvert : GeneratorDeconvertArrayFactory<int[]>
    {
        public override Array Generate(TypeDeconvert define)
        {
            byte[] arr = define.buffer;
            int[] ints = new int[arr.Length / sizeof(int)];
            for (int i = 0; i < ints.Length; i++)
            {
                ints[i] = BitConverter.ToInt32(arr, i * sizeof(int));
            }
            return ints;
        }
    }
}

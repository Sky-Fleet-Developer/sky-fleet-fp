using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Core.Utilities;

namespace Core.TerrainGenerator.Utility
{
    public static class RawReader
    {
        private static readonly Semaphore Semaphore = new Semaphore(3, 3);
        private static AsyncThreadDelegate<float[,]> _readWorker = new AsyncThreadDelegate<float[,]>(Semaphore);
        public static float[,] ReadArray(string path)
        {
            using (FileStream file = File.Open(path, FileMode.Open))
            {
                int length = (int)(file.Length / sizeof(ushort));
                int sqrLength = (int)Math.Sqrt(length);
                int i = 0;
                byte[] buf = new byte[sizeof(ushort)];
                float[,] height = new float[sqrLength,sqrLength];
                while (file.Read(buf, 0, sizeof(ushort)) > 0)
                {
                    float value = (float) BitConverter.ToUInt16(buf, 0) / ushort.MaxValue;
                    height[sqrLength - 1 - i / sqrLength, i % sqrLength] = value;
                    i++;
                }

                return height;
            }
        }
        
        public static Task<float[,]> ReadAsync(string path)
        {
            return _readWorker.RunAsync(() => ReadArray(path));
        }

        public static void WriteRaw16(float[,] data, string filePath)
        {
            using (var writer = new BinaryWriter(File.Open(filePath, FileMode.Create)))
            {
                int rows = data.GetLength(0);
                int columns = data.GetLength(1);

                for (int i = 0; i < rows; i++)
                {
                    for (int j = 0; j < columns; j++)
                    {
                        byte[] bytes = BitConverter.GetBytes((UInt16)(data[columns - 1 - i, j] * ushort.MaxValue));
                        writer.Write(bytes);
                    }
                }
            }
        }
    }
}
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text;

namespace Core.ContentSerializer
{
    public static class ExtensionStream
    {
        private static byte[] intBuffer = new byte[sizeof(int)];
        private static byte[] shortBuffer = new byte[sizeof(short)];
        private static byte[] floatBuffer = new byte[sizeof(float)];
        private static byte[] charBuffer = new byte[sizeof(char)];
        private static byte[] byteBuffer = new byte[sizeof(byte)];

        public static void WriteInt(this Stream stream, int value)
        {
            intBuffer = BitConverter.GetBytes(value);
            stream.Write(intBuffer, 0, sizeof(int));
        }

        public static int ReadInt(this Stream stream)
        {
            stream.Read(intBuffer, 0, sizeof(int));
            return BitConverter.ToInt32(intBuffer, 0);
        }

        public static void WriteShort(this Stream stream, short value)
        {
            shortBuffer = BitConverter.GetBytes(value);
            stream.Write(shortBuffer, 0, sizeof(short));
        }

        public static short ReadShort(this Stream stream)
        {
            stream.Read(shortBuffer, 0, sizeof(short));
            return BitConverter.ToInt16(shortBuffer, 0);
        }

        public static void WriteUShort(this Stream stream, ushort value)
        {
            shortBuffer = BitConverter.GetBytes(value);
            stream.Write(shortBuffer, 0, sizeof(ushort));
        }

        public static ushort ReadUShort(this Stream stream)
        {
            stream.Read(shortBuffer, 0, sizeof(ushort));
            return BitConverter.ToUInt16(shortBuffer, 0);
        }

        public static void WriteFloat(this Stream stream, float value)
        {
            floatBuffer = BitConverter.GetBytes(value);
            stream.Write(floatBuffer, 0, sizeof(int));
        }

        public static float ReadFloat(this Stream stream)
        {
            stream.Read(floatBuffer, 0, sizeof(float));
            return BitConverter.ToSingle(floatBuffer, 0);
        }

        public static void WriteChar(this Stream stream, char value)
        {
            charBuffer = BitConverter.GetBytes(value);
            stream.Write(charBuffer, 0, sizeof(char));
        }

        public static char ReadChar(this Stream stream)
        {
            stream.Read(charBuffer, 0, sizeof(char));
            return BitConverter.ToChar(charBuffer, 0);
        }

        public static void WriteString(this Stream stream, string value)
        {
            byte[] buffer = Encoding.UTF8.GetBytes(value);
            WriteInt(stream, buffer.Length);
            stream.Write(buffer, 0, buffer.Length);
        }

        public static string ReadString(this Stream stream)
        {
            int count = ReadInt(stream);
            byte[] buffer = new byte[count];
            stream.Read(buffer, 0, count);
            return Encoding.UTF8.GetString(buffer);
        }

        public static void WriteByte(this Stream stream, byte value)
        {
            byteBuffer[0] = value;
            stream.Write(byteBuffer, 0, 1);
        }

        public static byte ReadByte(this Stream stream)
        {
            stream.Read(byteBuffer, 0, 1);
            return byteBuffer[0];
        }

        public static void WriteBool(this Stream stream, bool value)
        {
            if (value)
                byteBuffer[0] = 255;
            else
                byteBuffer[0] = 0;
            stream.Write(byteBuffer, 0, 1);
        }

        public static bool ReadBool(this Stream stream)
        {
            stream.Read(byteBuffer, 0, 1);
            return byteBuffer[0] > 0;
        }
    }
}
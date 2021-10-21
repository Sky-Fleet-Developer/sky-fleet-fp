using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text;

namespace Core.ContentSerializer
{
    public class ExtensionStream
    {
        private byte[] intBuffer = new byte[sizeof(int)];
        private byte[] floatBuffer = new byte[sizeof(float)];
        private byte[] charBuffer = new byte[sizeof(char)];
        private byte[] byteBuffer = new byte[sizeof(byte)];

        public void WriteInt(int value, Stream stream)
        {
            intBuffer = BitConverter.GetBytes(value);
            stream.Write(intBuffer, 0, sizeof(int));
        }

        public int ReadInt(Stream stream)
        {
            stream.Read(intBuffer, 0, sizeof(int));
            return BitConverter.ToInt32(intBuffer, 0);
        }

        public void WriteFloat(float value, Stream stream)
        {
            floatBuffer = BitConverter.GetBytes(value);
            stream.Write(floatBuffer, 0, sizeof(int));
        }

        public float ReadFloat(Stream stream)
        {
            stream.Read(floatBuffer, 0, sizeof(float));
            return BitConverter.ToSingle(floatBuffer, 0);
        }

        public void WriteChar(char value, Stream stream)
        {
            charBuffer = BitConverter.GetBytes(value);
            stream.Write(charBuffer, 0, sizeof(char));
        }

        public char ReadChar(Stream stream)
        {
            stream.Read(charBuffer, 0, sizeof(char));
            return BitConverter.ToChar(charBuffer, 0);
        }

        public void WriteString(string value, Stream stream)
        {
            byte[] buffer = Encoding.UTF8.GetBytes(value);
            WriteInt(buffer.Length, stream);
            stream.Write(buffer, 0, buffer.Length);
        }

        public string ReadString(Stream stream)
        {
            int count = ReadInt(stream);
            byte[] buffer = new byte[count];
            stream.Read(buffer, 0, count);
            return Encoding.UTF8.GetString(buffer);
        }

        public void WriteByte(byte value, Stream stream)
        {
            byteBuffer[0] = value;
            stream.Write(byteBuffer, 0, 1);
        }

        public byte ReadByte(Stream stream)
        {
            stream.Read(byteBuffer, 0, 1);
            return byteBuffer[0];
        }
    }
}
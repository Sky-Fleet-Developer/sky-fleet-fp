using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Unity.Collections;
using UnityEngine;

namespace Core.ContentSerializer.CustomSerializers
{
    public class MeshSerializer : ICustomSerializer
    {
        public string Serialize(object source, ISerializationContext context, int idx)
        {
            Mesh mesh = (Mesh)(source);
            switch (idx)
            {
                case 1:
                    return mesh.name + "_" + mesh.GetInstanceID();

                default:
                    MemoryStream memoryStream = new MemoryStream();
                    FactoryArrayConventer arrayConventer = new FactoryArrayConventer();


                    //write to memory stream
                    byte[] name = Encoding.ASCII.GetBytes(mesh.name);
                    memoryStream.Write(BitConverter.GetBytes(name.Length), 0, sizeof(int));
                    memoryStream.Write(name, 0, name.Length);

                    byte[] format = { (byte)mesh.indexFormat };
                    memoryStream.Write(format, 0, 1);

                    //vertexes write
                    Vector3[] vertexesNB = mesh.vertices;
                    byte[] vertexesBuf = arrayConventer.Generate(vertexesNB);
                    memoryStream.Write(BitConverter.GetBytes(vertexesBuf.Length), 0, sizeof(int));
                    memoryStream.Write(vertexesBuf, 0, vertexesBuf.Length);

                    //normals write
                    Vector3[] normalsNB = mesh.normals;
                    byte[] normalsBuf = arrayConventer.Generate(normalsNB);
                    memoryStream.Write(BitConverter.GetBytes(normalsBuf.Length), 0, sizeof(int));
                    memoryStream.Write(normalsBuf, 0, normalsBuf.Length);

                    //triangles write
                    memoryStream.Write(BitConverter.GetBytes(mesh.subMeshCount), 0, sizeof(int));
                    for(int i = 0; i < mesh.subMeshCount; i++)
                    {
                        byte[] trianglesBuf = arrayConventer.Generate(mesh.GetTriangles(i));
                        memoryStream.Write(BitConverter.GetBytes(trianglesBuf.Length), 0, sizeof(int));
                        memoryStream.Write(trianglesBuf, 0, trianglesBuf.Length);
                    }
                    

                    //subMesh write
                    memoryStream.Write(BitConverter.GetBytes(mesh.subMeshCount), 0, sizeof(int));
                    for (int i = 0; i < mesh.subMeshCount; i++)
                    {
                        int indexStart = mesh.GetSubMesh(i).indexStart;
                        int indexCount = mesh.GetSubMesh(i).indexCount;
                        int topology = (int)mesh.GetSubMesh(i).topology;
                        memoryStream.Write(BitConverter.GetBytes(indexStart), 0, sizeof(int));
                        memoryStream.Write(BitConverter.GetBytes(indexCount), 0, sizeof(int));
                        memoryStream.Write(BitConverter.GetBytes(topology), 0, sizeof(int));
                    }

                    //uvs writes
                    byte[] uvBuf = arrayConventer.Generate(mesh.uv);
                    memoryStream.Write(BitConverter.GetBytes(uvBuf.Length), 0, sizeof(int));
                    memoryStream.Write(uvBuf, 0, uvBuf.Length);

                    uvBuf = arrayConventer.Generate(mesh.uv2);
                    memoryStream.Write(BitConverter.GetBytes(uvBuf.Length), 0, sizeof(int));
                    memoryStream.Write(uvBuf, 0, uvBuf.Length);

                    uvBuf = arrayConventer.Generate(mesh.uv3);
                    memoryStream.Write(BitConverter.GetBytes(uvBuf.Length), 0, sizeof(int));
                    memoryStream.Write(uvBuf, 0, uvBuf.Length);

                    //weight animation
                    NativeArray<BoneWeight1> bonesW = mesh.GetAllBoneWeights();                   
                    memoryStream.Write(BitConverter.GetBytes(bonesW.Length), 0, sizeof(int));
                    for (int i = 0; i < bonesW.Length; i++)
                    {
                        memoryStream.Write(BitConverter.GetBytes(bonesW[i].boneIndex), 0, sizeof(int));
                        memoryStream.Write(BitConverter.GetBytes(bonesW[i].weight), 0, sizeof(float));
                    }

                    NativeArray<byte> bones = mesh.GetBonesPerVertex();
                    memoryStream.Write(BitConverter.GetBytes(bones.Length), 0, sizeof(int));
                    memoryStream.Write(bones.ToArray(), 0, bones.Length);


                    string path = $"{context.ModFolderPath}{PathStorage.BASE_PATH_MODELS}";
                    Directory.CreateDirectory(path);
                    FileStream file = File.Open($"{path}/{mesh.name}_{mesh.GetInstanceID()}.mesh", FileMode.OpenOrCreate);
                    memoryStream.Seek(0, SeekOrigin.Begin);
                    memoryStream.CopyTo(file);
                    file.Close();
                    return mesh.GetInstanceID().ToString();
            }
        }

        public int GetStringsCount() => 2;

        public static async Task Deserialize(FileStream file, Mesh mesh)
        {
            FactoryArrayDeconvert deconvert = new FactoryArrayDeconvert();

            //
            byte[] bufferInt = new byte[sizeof(int)];//Get count bytes in name
            file.Read(bufferInt, 0, sizeof(int));
            int count = BitConverter.ToInt32(bufferInt, 0);

            byte[] nameBytes = new byte[count];//Get name
            file.Read(nameBytes, 0, count);
            string nameMesh = Encoding.ASCII.GetString(nameBytes);

            byte[] formatBuffer = new byte[1];
            file.Read(formatBuffer, 0, sizeof(byte)); //Get index format

            file.Read(bufferInt, 0, sizeof(int)); //Get vertexes
            count = BitConverter.ToInt32(bufferInt, 0);
            byte[] vertexBuf = new byte[count];
            file.Read(vertexBuf, 0, count);
            List<Vector3> listVertexes = new List<Vector3>();
            listVertexes.AddRange((Vector3[])deconvert.Generate(new TypeDeconvert(typeof(Vector3[]), vertexBuf)));

            await Task.Delay(100);

            file.Read(bufferInt, 0, sizeof(int)); //Get normals
            count = BitConverter.ToInt32(bufferInt, 0);
            byte[] normalsBuf = new byte[count];
            file.Read(normalsBuf, 0, count);
            List<Vector3> listNormals = new List<Vector3>();
            listNormals.AddRange((Vector3[])deconvert.Generate(new TypeDeconvert(typeof(Vector3[]), normalsBuf)));

            file.Read(bufferInt, 0, sizeof(int)); //Get triangles
            count = BitConverter.ToInt32(bufferInt, 0);
            List<int[]> triangles = new List<int[]>();
            for (int i = 0; i < count; i++)
            {
                file.Read(bufferInt, 0, sizeof(int));
                byte[] tr = new byte[BitConverter.ToInt32(bufferInt, 0)];
                file.Read(tr, 0, tr.Length);
                triangles.Add((int[])deconvert.Generate(new TypeDeconvert(typeof(int[]), tr)));
            }

            await Task.Delay(100);

            //Get subMesh
            file.Read(bufferInt, 0, sizeof(int));
            count = BitConverter.ToInt32(bufferInt, 0);
            List<UnityEngine.Rendering.SubMeshDescriptor> subMeshs = new List<UnityEngine.Rendering.SubMeshDescriptor>();
            for (int i = 0; i < count; i++)
            {
                int indexStart;
                int indexCount;
                int topology;
                file.Read(bufferInt, 0, sizeof(int));
                indexStart = BitConverter.ToInt32(bufferInt, 0);
                file.Read(bufferInt, 0, sizeof(int));
                indexCount = BitConverter.ToInt32(bufferInt, 0);
                file.Read(bufferInt, 0, sizeof(int));
                topology = BitConverter.ToInt32(bufferInt, 0);
                subMeshs.Add(new UnityEngine.Rendering.SubMeshDescriptor(indexStart, indexCount, (MeshTopology)topology));
            }

            await Task.Delay(100);

            //Get uvs
            file.Read(bufferInt, 0, sizeof(int));
            count = BitConverter.ToInt32(bufferInt, 0);
            byte[] uv0Buf = new byte[count];
            file.Read(uv0Buf, 0, count);
            List<Vector2> listUV0 = new List<Vector2>();
            listUV0.AddRange((Vector2[])deconvert.Generate(new TypeDeconvert(typeof(Vector2[]), uv0Buf)));

            file.Read(bufferInt, 0, sizeof(int));
            count = BitConverter.ToInt32(bufferInt, 0);
            byte[] uv2Buf = new byte[count];
            file.Read(uv2Buf, 0, count);
            List<Vector2> listUV2 = new List<Vector2>();
            listUV2.AddRange((Vector2[])deconvert.Generate(new TypeDeconvert(typeof(Vector2[]), uv2Buf)));

            file.Read(bufferInt, 0, sizeof(int));
            count = BitConverter.ToInt32(bufferInt, 0);
            byte[] uv3Buf = new byte[count];
            file.Read(uv3Buf, 0, count);
            List<Vector2> listUV3 = new List<Vector2>();
            listUV3.AddRange((Vector2[])deconvert.Generate(new TypeDeconvert(typeof(Vector2[]), uv3Buf)));

            await Task.Delay(100);

            //Get weight animation
            byte[] bufferFloat = new byte[sizeof(float)];
            file.Read(bufferInt, 0, sizeof(int));
            count = BitConverter.ToInt32(bufferInt, 0);
            List<BoneWeight1> weights = new List<BoneWeight1>();
            for (int i = 0; i < count; i++)
            {
                file.Read(bufferInt, 0, sizeof(int));
                int boneIndex = BitConverter.ToInt32(bufferInt, 0);
                file.Read(bufferFloat, 0, sizeof(float));
                float boneWeight = BitConverter.ToInt32(bufferFloat, 0);
                BoneWeight1 w = new BoneWeight1();
                w.boneIndex = boneIndex;
                w.weight = boneWeight;
                weights.Add(w);
            }

            file.Read(bufferInt, 0, sizeof(int));
            count = BitConverter.ToInt32(bufferInt, 0);
            byte[] bones = new byte[count];

            ///
            mesh.name = nameMesh;
            mesh.indexFormat = (UnityEngine.Rendering.IndexFormat)formatBuffer[0];

            mesh.SetVertices(listVertexes);
            mesh.SetNormals(listNormals);
            mesh.SetUVs(0, listUV0);
            mesh.SetUVs(1, listUV2);
            mesh.SetUVs(2, listUV3);
            mesh.subMeshCount = subMeshs.Count;

            for (int i = 0; i < triangles.Count; i++)
            {
                mesh.SetTriangles(triangles[i], i);
            }
            mesh.SetSubMeshes(subMeshs);

            Unity.Collections.NativeArray<byte> nativeBones = new Unity.Collections.NativeArray<byte>(bones, Unity.Collections.Allocator.Persistent);
            Unity.Collections.NativeArray<BoneWeight1> nativeWeights = new Unity.Collections.NativeArray<BoneWeight1>(weights.ToArray(), Unity.Collections.Allocator.Persistent);
            mesh.SetBoneWeights(nativeBones, nativeWeights);
            nativeBones.Dispose();
            nativeWeights.Dispose();

            mesh.RecalculateBounds();
            mesh.RecalculateTangents();
        }

        public async Task Deserialize(string prefix, object source, Dictionary<string, string> cache, ISerializationContext context)
        {
            string path = $"{context.ModFolderPath}{PathStorage.MOD_RELETIVE_PATH_MODELS}";

            Debug.Log($"Search mesh in path: {path}/{cache[prefix + "_1"]}.mesh");

            FileStream file = File.Open($"{path}/{cache[prefix + "_1"]}.mesh", FileMode.Open);
            if (file == null)
            {
                throw new NullReferenceException();
            }
            await Deserialize(file, (Mesh)source);
            file.Close();
        }
    }
}

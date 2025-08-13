using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Core.Configurations;
using Core.ContentSerializer;
using Core.ContentSerializer.Bundles;
using Core.ContentSerializer.Providers;
using Core.Data;
using Core.Explorer.Content;
using Core.Game;
using Core.Structure;
using Core.Structure.Rigging;
using Newtonsoft.Json;
using Runtime;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Core.SessionManager.SaveService
{
    [ShowInInspector]
    public class SaveLoad
    {
        public static readonly int IntSize = sizeof(int);



        public void Save(string path, string name)
        {
            Debug.Log("Begin to save the session...");

            
            
            State state = new State(StructureUpdateModule.Structures);
            
            

            state.worldOffset = WorldOffset.Offset;
            Transform playerTransform = Session.Instance.Player.transform;
            state.playerPos = playerTransform.localPosition - WorldOffset.Offset;
            state.playerRot = playerTransform.localEulerAngles;

            SaveToFile(state, path, name);
            Debug.Log("Session was saved successfully!");
        }

        public async Task Load(string filePath)
        {
            State state = LoadStateAtPath(filePath);

            //TODO: подождать пока загрузится сцена меню, если мы ещё не в ней

            Transform playerTransform = Session.Instance.Player.transform;
            playerTransform.localPosition = state.playerPos;
            playerTransform.localEulerAngles = state.playerRot;
            WorldOffset.Offset = state.worldOffset;

            List<System.Reflection.Assembly> assemblies = AppDomain.CurrentDomain.GetAssemblies().ToList();
            LinkedList<Mod> mods = Session.Instance.Settings.mods;
            if (mods != null)
            {
                foreach (Mod mod in mods)
                {
                    TablePrefabs.Instance.ExtractBlocksFromMod(mod);
                    assemblies.Add(mod.assembly);
                }
            }

            Deserializer deserializer = StructureProvider.GetDeserializer(assemblies.ToArray());

            List<Task> waiting = new List<Task>();
            foreach (StructureBundle structureBundle in state.structuresCache)
            {
                Task<IStructure> task = structureBundle.ConstructStructure(deserializer);
                waiting.Add(task);
            }

            await Task.WhenAll(waiting);
            Debug.Log($"Save at path {filePath} successfully loaded!");
        }

        private State LoadStateAtPath(string filePath)
        {
            State state = null;
            using (FileStream stream = new FileStream(filePath, FileMode.Open))
            {
                try
                {
                    byte[] intBuffer = new byte[IntSize];
                    stream.Read(intBuffer, 0, IntSize);
                    int headerLength = BitConverter.ToInt32(intBuffer, 0);
                    stream.Seek(headerLength, SeekOrigin.Begin);

                    state = ReadStateJson(stream);
                }
                catch (Exception e)
                {
                    Debug.LogError(e);
                }
            }

            return state;
        }

        public StateHeader ReadHeader(string fileName)
        {
            FileStream stream = new FileStream(fileName, FileMode.Open);
            StateHeader header = null;
            try
            {
                ReadHeader(stream, out string stateName, out string version, out List<string> modsNames);
                
                header = new StateHeader{ name =  stateName, serializationVersion = version, mods =  modsNames};
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }

            stream.Close();
            return header;
        }

        private void SaveToFile(State state, string path, string name)
        {
            SessionSettings sessionSettings = Session.Instance.Settings;

            FileStream stream = new FileStream(path, FileMode.OpenOrCreate);

            try
            {
                WriteHeader(name, stream, sessionSettings.mods);
                WriteStateJson(state, stream);
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }

            stream.Close();
        }

        private static State ReadStateJson(Stream stream)
        {
            byte[] intBuffer = new byte[IntSize];
            stream.Read(intBuffer, 0, IntSize);
            int jsonSize = BitConverter.ToInt32(intBuffer, 0);
            byte[] stringBuffer = new byte[jsonSize];
            stream.Read(stringBuffer, 0, jsonSize);
            string json = Encoding.ASCII.GetString(stringBuffer);
            State state = null;
            try
            {
                state = JsonConvert.DeserializeObject<State>(json);

                // TODO: если Application.isPlaying, загрузить список модов из ModLoader
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
            return state;
        }

        private static void WriteStateJson(State state, Stream stream)
        {
            string json = JsonConvert.SerializeObject(state);
            byte[] jsonBytes = Encoding.ASCII.GetBytes(json);
            stream.Write(BitConverter.GetBytes(jsonBytes.Length), 0, IntSize);
            stream.Write(jsonBytes, 0, jsonBytes.Length);
        }

        private static int GetHeaderLength(Stream stream)
        {
            byte[] intBuffer = new byte[IntSize];
            stream.Read(intBuffer, 0, IntSize);
            return BitConverter.ToInt32(intBuffer, 0);
        }

        private static void ReadHeader(Stream stream, out string stateName, out string serializationVersion,
            out List<string> modsNames)
        {
            stream.Seek(IntSize, SeekOrigin.Begin); //header size
            byte[] intBuffer = new byte[IntSize];
            stream.Read(intBuffer, 0, IntSize); //name size
            int nameLength = BitConverter.ToInt32(intBuffer, 0);

            byte[] stringBuffer = new byte[nameLength];

            stream.Read(stringBuffer, 0, nameLength); //name
            stateName = Encoding.ASCII.GetString(stringBuffer);

            stream.Read(intBuffer, 0, IntSize); //version size
            int svLength = BitConverter.ToInt32(intBuffer, 0);
            stringBuffer = new byte[svLength];
            stream.Read(stringBuffer, 0, svLength); //version
            serializationVersion = Encoding.ASCII.GetString(stringBuffer);

            stream.Read(intBuffer, 0, IntSize); //mods count
            int modsCount = BitConverter.ToInt32(intBuffer, 0);

            modsNames = new List<string>(modsCount);
            for (int i = 0; i < modsCount; i++)
            {
                stream.Read(intBuffer, 0, IntSize); //mod name size
                int length = BitConverter.ToInt32(intBuffer, 0);
                stringBuffer = new byte[length];
                stream.Read(stringBuffer, 0, length); //mod name
                modsNames.Add(Encoding.ASCII.GetString(stringBuffer));
            }
        }

        private static void WriteHeader(string saveName, Stream stream, LinkedList<Mod> mods)
        {
            int intSize = sizeof(int);
            byte[] nameB = Encoding.ASCII.GetBytes(saveName);
            byte[] sv = Encoding.ASCII.GetBytes(GameData.Data.serializationVersion);
            int headerLength = nameB.Length + intSize * 4 + sv.Length;
            foreach (Mod mod in mods)
            {
                nameB = Encoding.ASCII.GetBytes(mod.name);
                headerLength += nameB.Length + intSize;
            }

            stream.Write(BitConverter.GetBytes(headerLength), 0, intSize); //header size
            stream.Write(BitConverter.GetBytes(nameB.Length), 0, intSize); //name size
            stream.Write(nameB, 0, nameB.Length); //name
            stream.Write(BitConverter.GetBytes(sv.Length), 0, intSize); //version size
            stream.Write(sv, 0, sv.Length); //version
            stream.Write(BitConverter.GetBytes(mods.Count), 0, intSize); //mods count
            foreach (Mod mod in mods)
            {
                nameB = Encoding.ASCII.GetBytes(mod.name);
                stream.Write(BitConverter.GetBytes(nameB.Length), 0, intSize); //mod name size
                stream.Write(nameB, 0, nameB.Length); //mod name
            }
        }
    }

    public class StateHeader
    {
        public string name;
        public string serializationVersion = "0.0.1";
        public List<string> mods;
    }
}

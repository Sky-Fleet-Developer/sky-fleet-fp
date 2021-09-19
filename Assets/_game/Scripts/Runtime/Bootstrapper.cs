using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Management;
using Structure;
using UnityEngine;
using System.Reflection;

namespace Game
{
    public class Bootstrapper : MonoBehaviour
    {
        void Awake()
        {
            StructureManager.CheckInstance();
            GameData.CheckInstance();
            GameData.Instance.OnEnable();


            List<string> loadedLog = new List<string>();

            try
            {

                string path = Directory.GetCurrentDirectory() + "/Mods";
                string[] fileNames = Directory.GetFiles(path, ".", SearchOption.TopDirectoryOnly);
                loadedLog.Add($"Path {path}. Files found {fileNames.Length}");


                foreach (string assemblyFileName in fileNames)
                {
                    loadedLog.Add($"Check {assemblyFileName}");

                    if (assemblyFileName.Contains(".dll"))
                    {
                        Assembly asm = Assembly.Load(AssemblyName.GetAssemblyName(assemblyFileName));

                        if (asm != null)
                        {
                            loadedLog.Add($"Found assamble {asm.GetName().Name}:");

                            Type[] types = asm.GetTypes();

                            foreach (Type type in types)
                            {
                                loadedLog.Add(type.FullName);
                            }
                        }
                        else
                        {
                            loadedLog.Add($"Assamble is null - {asm.GetName().Name}:");
                        }
                    }
                }


                /*foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    log.Add(assembly.GetName().Name);
     
                }*

                /*using (FileStream stream = new FileStream(Application.dataPath + "/LOG.txt", FileMode.OpenOrCreate))
                {
                    stream.Close();
                }
                */
                //log.AddRange(loadedLog);

            }
            catch (Exception e)
            {
                loadedLog.Add(e.ToString());
            }

            File.WriteAllLines(Application.dataPath + "/LOG.txt", loadedLog);
        }
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Core.Structure;
using Core.Structure.Rigging;
using Newtonsoft.Json;
using UnityEngine;
using UnityEditor;

namespace Structure.Editor
{
    public class WiresEditor : EditorWindow
    {
        public static WiresEditor current;
        
        [MenuItem("Factory/Wires Editor")]
        public static void OpenWindow()
        {
            current = GetWindow<WiresEditor>();
        }

        [SerializeField] private IStructure selectedStructure;
        [SerializeField] private StructureConfiguration configuration = null;
        [SerializeField] private List<(IBlock block, List<Port> ports, FieldInfo[] infos, List<Port> specialPorts)> portsList;
        private List<string> createWireList;
        private string json;
        private bool configDirty = true;
        
        public void OnEnable()
        {
            Selection.selectionChanged += GetFomSelection;
            GetFomSelection();
        }

        public void OnDisable()
        {
            Selection.selectionChanged -= GetFomSelection;
        }

        private void GetFomSelection()
        {
            if (Selection.activeGameObject && Selection.activeGameObject.TryGetComponent(out IStructure structure))
            {
                selectedStructure = structure;
            }
        }

        private void OnGUI()
        {
            if(selectedStructure == null) return;
            
            switch (Event.current.type)
            {
                default://case EventType.Repaint:
                    if (configuration == null)
                    {
                        CreateButton();
                    }
                    else
                    {
                        CreationMenu();
                    }

                    break;
            }
        }

        private void CreateButton()
        {
            GUILayout.Space(20);
            EditorGUILayout.ObjectField(selectedStructure.transform, typeof(Transform), true);

            if (GUILayout.Button("Create configuration", GUILayout.Width(200)))
            {
                CreateConfiguration();
            }
        }

        private void CreateConfiguration()
        {
            if(selectedStructure.Blocks == null) selectedStructure.RefreshParents();
            portsList = new List<(IBlock block, List<Port> ports, FieldInfo[] infos, List<Port> specialPorts)>(selectedStructure.Blocks.Count);

            configuration = new StructureConfiguration
            {
                blocks = new List<BlockConfiguration>(selectedStructure.Blocks.Count),
                wires = new List<string>()
            };

            for(int i = 0; i < selectedStructure.Blocks.Count; i++)
            {
                var block = selectedStructure.Blocks[i];
                
                var ports = new List<Port>();
                Factory.GetPorts(block, ref ports);
                
                var specialPorts = new List<Port>();
                Factory.GetSpecialPorts(block, ref specialPorts);

                var infos = Factory.GetPortsInfo(block);
                
                portsList.Add((block, ports, infos, specialPorts));

                configuration.blocks.Add(Factory.GetConfiguration(block));
            }
        }

        private void CreationMenu()
        {
            foreach (var port in portsList)
            {
                DisplayBlockPorts(port.block, port.ports, port.infos, port.specialPorts);
            }

            CreateWireMenu();

            ShowJson();
        }

        private void ShowJson()
        {
            if (configDirty)
            {
                json = JsonConvert.SerializeObject(configuration);
                configDirty = false;
            }
            GUILayout.TextArea(json, GUILayout.Height(300));
        }

        private void CreateWireMenu()
        {
            if (createWireList == null) createWireList = new List<string>();

            for (int i = 0; i < createWireList.Count; i++)
            {
                createWireList[i] = GUILayout.TextField(createWireList[i]);
            }
            string newWire = GUILayout.TextField(string.Empty);
            if (newWire.Length > 0)
            {
                createWireList.Add(newWire);
            }

            if (GUILayout.Button("Create wire"))
            {
                configuration.wires.Add(Factory.GetWireString(createWireList));
                createWireList = new List<string>();
                configDirty = true;
            }
        }

        private void DisplayBlockPorts(IBlock block, List<Port> ports, FieldInfo[] infos, List<Port> specialPorts)
        {
            GUILayout.Label(block.transform.name);
            for (int i = 0; i < ports.Count; i++)
            {
                DisplayPort(ports[i], infos[i]);
            }
            if (specialPorts.Count > 0)
            {
                GUILayout.Label("Special ports:");
                for (int i = 0; i < specialPorts.Count; i++)
                {
                    DisplayPort(specialPorts[i], null);
                }
            }
            GUILayout.Space(10);

        }

        private void DisplayPort(Port port, FieldInfo info)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(10);
            if (info != null)
            {
                GUILayout.Label(info.Name, GUILayout.Width(110));
                GUILayout.Space(10);
            }
            else
            {
                GUILayout.Space(60);
            }
            GUILayout.TextField(port.Guid);

            GUILayout.EndHorizontal();

        }
    }
}
